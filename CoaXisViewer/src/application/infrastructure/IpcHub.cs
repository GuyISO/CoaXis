using CoaXis.Protocol.Viewer;
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
/// IPC メッセージの受信、振り分け、結果応答を管理するハブ。
/// </summary>
public partial class IpcHub : BaseHub
{
    #region Fields

    /// <summary>受信スレッドからメインスレッドへ処理を引き渡すためのキュー Godot の Node/Signal API はメインスレッドでのみ安全に呼べるため、実際のディスパッチは _Process 側で行う。</summary>
    private readonly ConcurrentQueue<PendingRequest> _pendingRequests = new();

    /// <summary>受信タスクで検出した状態・受信通知をメインスレッドへ渡すキュー。</summary>
    private readonly ConcurrentQueue<PendingNotification> _pendingNotifications = new();

    /// <summary>メインスレッド処理の待機タイムアウト（ミリ秒）。</summary>
    private const int MainThreadTimeoutMilliseconds = 2000;
    /// <summary>IPCメッセージの送信元名。</summary>
    private const string ViewerSourceName = "Viewer";

    /// <summary>IPC用の名前付きパイプサーバー。</summary>
    private NamedPipeServerStream _pipeServer;
    /// <summary>受信待機の取消しトークン。</summary>
    private CancellationTokenSource _cts;
    /// <summary>受信待機タスク。</summary>
    private Task _listenTask;

    #endregion

    #region Properties

    /// <summary>
    /// NamedPipeサーバーが稼働中かどうかを取得する。
    /// </summary>
    public bool IsPipeServerRunning =>
        _cts != null &&
        !_cts.IsCancellationRequested &&
        _listenTask is { IsCompleted: false };

    /// <summary>
    /// NamedPipeクライアントが現在接続中かどうかを取得する。
    /// </summary>
    public bool IsClientConnected => _pipeServer?.IsConnected ?? false;

    #endregion

    #region Signals

    /// <summary>
    /// NamedPipeサーバー稼働状態の通知。
    /// </summary>
    /// <param name="isRunning">サーバーが稼働中の場合は true</param>
    [Signal] public delegate void PipeServerStatusEventHandler(bool isRunning);

    /// <summary>
    /// NamedPipeサーバーへのクライアントの接続状態の通知。
    /// </summary>
    /// <param name="isConnected">クライアントが接続中の場合は true</param>
    [Signal] public delegate void ClientStatusEventHandler(bool isConnected);


    /// <summary>
    /// IPCメッセージ受信の通知。
    /// </summary>
    /// <param name="eventType">受信したメッセージの eventType</param>
    [Signal] public delegate void MessageReceivedEventHandler(string eventType);

    /// <summary>
    /// IPC メッセージの処理結果を通知する。
    /// </summary>
    /// <param name="eventType">処理したメッセージの eventType</param>
    /// <param name="ok">処理が成功した場合は true</param>
    /// <param name="errorCode">失敗時の標準化エラーコード</param>
    [Signal] public delegate void MessageHandledEventHandler(string eventType, bool ok, string errorCode);

    #endregion

    #region Lifecycle

    public override void _Ready()
    {
        IpcSettings settings = Application.Setting.Current.Ipc;
        if (settings.StartPipeServerOnReady)
        {
            StartPipeServer(settings.PipeName);
        }
    }

    public override void _Process(double delta)
    {
        while (_pendingNotifications.TryDequeue(out PendingNotification notification))
        {
            if (notification.Type == PendingNotificationType.ClientStatus)
            {
                EmitSignal(SignalName.ClientStatus, notification.IsConnected);
            }
            else
            {
                EmitSignal(SignalName.MessageReceived, notification.EventType);
            }
        }

        // 受信スレッドが溜めたリクエストを、メインスレッド上で順番にドメイン層へ振り分ける
        while (_pendingRequests.TryDequeue(out PendingRequest request))
        {
            IpcResultPayload result = IpcCommandDispatcher.Dispatch(request.Envelope);
            EmitSignal(SignalName.MessageHandled, request.Envelope.EventType, result.Ok, result.ErrorCode);
            request.CompletionSource.TrySetResult(result);
        }
    }

    public override void _ExitTree()
    {
        StopPipeServer();

        base._ExitTree();
    }

    #endregion

    #region Events

    #endregion

    #region Methods

    /// <summary>
    /// NamedPipe サーバーを起動する。
    /// </summary>
    /// <param name="pipeName">待ち受けるパイプ名</param>
    public void StartPipeServer(string pipeName)
    {
        if (_listenTask != null && !_listenTask.IsCompleted)
        {
            return;
        }

        _cts = new CancellationTokenSource();
        CancellationToken token = _cts.Token;
        _listenTask = Task.Run(() => AcceptLoopAsync(pipeName, token), token);

        Application.Log.Info($"Ipc: server started on pipe '{pipeName}'.");
        
        EmitSignal(SignalName.PipeServerStatus, true);
    }

    /// <summary>
    /// NamedPipe サーバーを停止する。
    /// </summary>
    public void StopPipeServer()
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

        EmitSignal(SignalName.PipeServerStatus, false);
    }

    #endregion

    #region Helpers

    /// <summary>
    /// クライアント接続を待ち受け、切断されるたびに次の接続待ちへ戻る。
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
                _pendingNotifications.Enqueue(PendingNotification.ClientStatusChanged(true));
                try
                {
                    await HandleConnectionAsync(pipe, token).ConfigureAwait(false);
                }
                finally
                {
                    _pendingNotifications.Enqueue(PendingNotification.ClientStatusChanged(false));
                }
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
    /// 1接続分の受信ループ。1行1メッセージのJSONを読み、Result応答を1行返す。
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
            string json = JsonSerializer.Serialize(response, IpcJsonOptions.Default);
            await writer.WriteLineAsync(json).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 受信した1行を検証・振り分けし、Result応答エンベロープを組み立てる。
    /// </summary>
    private async Task<IpcEnvelope> ProcessLineAsync(string line, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            _pendingNotifications.Enqueue(PendingNotification.MessageReceived(string.Empty));
            return BuildResponse(null, IpcResultPayload.Failure(string.Empty, IpcErrorCode.EmptyMessage, "Received an empty message."));
        }

        IpcEnvelope envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<IpcEnvelope>(line, IpcJsonOptions.Default);
        }
        catch (JsonException ex)
        {
            _pendingNotifications.Enqueue(PendingNotification.MessageReceived(string.Empty));
            return BuildResponse(null, IpcResultPayload.Failure(string.Empty, IpcErrorCode.InvalidJson, ex.Message));
        }

        if (envelope == null)
        {
            _pendingNotifications.Enqueue(PendingNotification.MessageReceived(string.Empty));
            return BuildResponse(null, IpcResultPayload.Failure(string.Empty, IpcErrorCode.InvalidJson, "Message payload could not be parsed."));
        }

        _pendingNotifications.Enqueue(PendingNotification.MessageReceived(envelope.EventType ?? string.Empty));

        if (string.IsNullOrWhiteSpace(envelope.EventType))
        {
            return BuildResponse(envelope, IpcResultPayload.Failure(string.Empty, IpcErrorCode.MissingEventType, "eventType is required."));
        }

        if (!IpcProtocolVersion.IsCompatible(envelope.Version))
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
    /// Result エンベロープを組み立てる。
    /// </summary>
    /// <param name="request">元の受信エンベロープ。パース不能時は null</param>
    /// <param name="payload">処理結果</param>
    private static IpcEnvelope BuildResponse(IpcEnvelope request, IpcResultPayload payload)
    {
        return IpcEnvelopeBuilder.Create(
            IpcEventType.Common.Result,
            ViewerSourceName,
            payload,
            request?.EventId);
    }

    /// <summary>
    /// 受信スレッドからメインスレッドへ引き渡す1件分のリクエスト。
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

    /// <summary>メインスレッドで発火するIPC通知の種類。</summary>
    private enum PendingNotificationType
    {
        ClientStatus,
        MessageReceived
    }

    /// <summary>受信タスクからメインスレッドへ渡すIPC通知。</summary>
    private readonly struct PendingNotification
    {
        /// <summary>通知の種類。</summary>
        public PendingNotificationType Type { get; }
        /// <summary>クライアント接続通知の値。</summary>
        public bool IsConnected { get; }
        /// <summary>受信メッセージのイベント種別。</summary>
        public string EventType { get; }

        private PendingNotification(PendingNotificationType type, bool isConnected, string eventType)
        {
            Type = type;
            IsConnected = isConnected;
            EventType = eventType;
        }

        /// <summary>クライアント接続状態通知を生成する。</summary>
        /// <param name="isConnected">接続中の場合は true</param>
        public static PendingNotification ClientStatusChanged(bool isConnected) =>
            new(PendingNotificationType.ClientStatus, isConnected, string.Empty);

        /// <summary>受信メッセージ通知を生成する。</summary>
        /// <param name="eventType">イベント種別。解析できない場合は空文字列</param>
        public static PendingNotification MessageReceived(string eventType) =>
            new(PendingNotificationType.MessageReceived, false, eventType);
    }

    #endregion
}
