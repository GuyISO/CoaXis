using Godot;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// NamedPipe 経由で IPC メッセージを受信し、ドメイン層への振り分けとResult応答を行う Autoload ノード
/// </summary>
public partial class IpcService : Node
{
    #region Fields

    /// <summary>
    /// 受信スレッドからメインスレッドへ処理を引き渡すためのキュー
    /// Godot の Node/Signal API はメインスレッドでのみ安全に呼べるため、実際のディスパッチは _Process 側で行う
    /// </summary>
    private readonly ConcurrentQueue<PendingRequest> _pendingRequests = new();

    private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    private const int MainThreadTimeoutMilliseconds = 2000;
    private const string ViewerSourceName = "Viewer";
    private const string ResultEventType = "Result";

    private NamedPipeServerStream _pipeServer;
    private CancellationTokenSource _cts;
    private Task _listenTask;

    #endregion

    #region Lifecycle

    public override void _Ready()
    {
        IpcSettings settings = Application.Setting.Service.Current.Ipc;
        if (settings.StartPipeServerOnReady)
        {
            Start(settings.PipeName);
        }
    }

    public override void _Process(double delta)
    {
        // 受信スレッドが溜めたリクエストを、メインスレッド上で順番にドメイン層へ振り分ける
        while (_pendingRequests.TryDequeue(out PendingRequest request))
        {
            IpcResultPayload result = IpcCommandDispatcher.Dispatch(request.Envelope);
            Application.Ipc.Event.NotifyMessageHandled(request.Envelope.EventType, result.Ok, result.ErrorCode);
            request.CompletionSource.TrySetResult(result);
        }
    }

    public override void _ExitTree()
    {
        Stop();

        base._ExitTree();
    }

    #endregion

    #region Public API

    /// <summary>
    /// NamedPipe サーバーを起動する
    /// </summary>
    /// <param name="pipeName">待ち受けるパイプ名</param>
    internal void Start(string pipeName)
    {
        if (_listenTask != null && !_listenTask.IsCompleted)
        {
            return;
        }

        _cts = new CancellationTokenSource();
        CancellationToken token = _cts.Token;
        _listenTask = Task.Run(() => AcceptLoopAsync(pipeName, token), token);

        Application.Log.Info($"Ipc: server started on pipe '{pipeName}'.");
    }

    /// <summary>
    /// NamedPipe サーバーを停止する
    /// </summary>
    internal void Stop()
    {
        if (_cts == null)
        {
            return;
        }

        _cts.Cancel();

        try
        {
            _pipeServer?.Dispose();
        }
        catch (Exception)
        {
            // 停止処理は継続するため、切断済みパイプの Dispose 例外は無視する
        }

        _cts.Dispose();
        _cts = null;

        Application.Log.Info("Ipc: server stopped.");
    }

    #endregion

    #region Internal Helpers

    /// <summary>
    /// クライアント接続を待ち受け、切断されるたびに次の接続待ちへ戻る
    /// </summary>
    private async Task AcceptLoopAsync(string pipeName, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                using var pipe = new NamedPipeServerStream(
                    pipeName,
                    PipeDirection.InOut,
                    1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                _pipeServer = pipe;

                await pipe.WaitForConnectionAsync(token).ConfigureAwait(false);
                await HandleConnectionAsync(pipe, token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (IOException)
            {
                // クライアント切断時は次の接続待ちへ戻る
            }
        }
    }

    /// <summary>
    /// 1接続分の受信ループ。1行1メッセージのJSONを読み、Result応答を1行返す
    /// </summary>
    private async Task HandleConnectionAsync(NamedPipeServerStream pipe, CancellationToken token)
    {
        using var reader = new StreamReader(pipe, Encoding.UTF8, false, 1024, leaveOpen: true);
        using var writer = new StreamWriter(pipe, new UTF8Encoding(false), 1024, leaveOpen: true)
        {
            AutoFlush = true,
            NewLine = "\n"
        };

        while (pipe.IsConnected && !token.IsCancellationRequested)
        {
            string line = await reader.ReadLineAsync().ConfigureAwait(false);
            if (line == null)
            {
                break;
            }

            IpcEnvelope response = await ProcessLineAsync(line, token).ConfigureAwait(false);
            string json = JsonSerializer.Serialize(response, SerializerOptions);
            await writer.WriteLineAsync(json).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 受信した1行を検証・振り分けし、Result応答エンベロープを組み立てる
    /// </summary>
    private async Task<IpcEnvelope> ProcessLineAsync(string line, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return BuildResponse(null, IpcResultPayload.Failure(string.Empty, IpcErrorCode.EmptyMessage, "Received an empty message."));
        }

        IpcEnvelope envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<IpcEnvelope>(line, SerializerOptions);
        }
        catch (JsonException ex)
        {
            return BuildResponse(null, IpcResultPayload.Failure(string.Empty, IpcErrorCode.InvalidJson, ex.Message));
        }

        if (envelope == null)
        {
            return BuildResponse(null, IpcResultPayload.Failure(string.Empty, IpcErrorCode.InvalidJson, "Message payload could not be parsed."));
        }

        if (string.IsNullOrWhiteSpace(envelope.EventType))
        {
            return BuildResponse(envelope, IpcResultPayload.Failure(string.Empty, IpcErrorCode.MissingEventType, "eventType is required."));
        }

        if (!IsCompatibleVersion(envelope.Version))
        {
            return BuildResponse(envelope, IpcResultPayload.Failure(envelope.EventType, IpcErrorCode.VersionMismatch, $"Unsupported protocol version: '{envelope.Version}'."));
        }

        var request = new PendingRequest(envelope);
        _pendingRequests.Enqueue(request);

        Task<IpcResultPayload> completion = request.CompletionSource.Task;
        Task timeoutTask = Task.Delay(MainThreadTimeoutMilliseconds, token);
        Task completed = await Task.WhenAny(completion, timeoutTask).ConfigureAwait(false);

        IpcResultPayload result = completed == completion
            ? await completion.ConfigureAwait(false)
            : IpcResultPayload.Failure(envelope.EventType, IpcErrorCode.MainThreadTimeout, "Main thread did not respond in time.");

        return BuildResponse(envelope, result);
    }

    /// <summary>
    /// version のメジャー番号がプロトコルバージョンと一致するか判定する
    /// </summary>
    private static bool IsCompatibleVersion(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            return false;
        }

        string requiredMajor = Constant.Ipc.ProtocolVersion.Split('.')[0];
        string requestedMajor = version.Split('.')[0];
        return string.Equals(requiredMajor, requestedMajor, StringComparison.Ordinal);
    }

    /// <summary>
    /// Result エンベロープを組み立てる
    /// </summary>
    /// <param name="request">元の受信エンベロープ。パース不能時は null</param>
    /// <param name="payload">処理結果</param>
    private static IpcEnvelope BuildResponse(IpcEnvelope request, IpcResultPayload payload)
    {
        return new IpcEnvelope
        {
            EventId = Guid.NewGuid().ToString(),
            EventType = ResultEventType,
            Version = Constant.Ipc.ProtocolVersion,
            Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            Source = ViewerSourceName,
            CorrelationId = request?.EventId,
            Payload = JsonSerializer.SerializeToElement(payload, SerializerOptions)
        };
    }

    #endregion

    #region Nested Types

    /// <summary>
    /// 受信スレッドからメインスレッドへ引き渡す1件分のリクエスト
    /// </summary>
    private sealed class PendingRequest
    {
        public IpcEnvelope Envelope { get; }
        public TaskCompletionSource<IpcResultPayload> CompletionSource { get; }

        public PendingRequest(IpcEnvelope envelope)
        {
            Envelope = envelope;
            CompletionSource = new TaskCompletionSource<IpcResultPayload>(TaskCreationOptions.RunContinuationsAsynchronously);
        }
    }

    #endregion
}
