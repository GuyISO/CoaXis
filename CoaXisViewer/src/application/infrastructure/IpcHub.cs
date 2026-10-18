using CoaXis.Protocol.Viewer;
using Godot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

/// <summary>
/// IPC メッセージの受信、振り分け、結果応答、およびViewer発通知を管理するハブ。
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
    /// <summary>現在接続中のクライアントへ書き込む送信セッション。</summary>
    private ConnectionSession _activeConnection;
    /// <summary>選択状態通知イベントを購読済みかどうか。</summary>
    private bool _selectionEventsSubscribed;

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
        SubscribeSelectionEvents();

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
            if (!request.TryStart())
            {
                continue;
            }

            IpcResultPayload result = IpcCommandDispatcher.Dispatch(request.Envelope);
            EmitSignal(SignalName.MessageHandled, request.Envelope.EventType, result.Ok, result.ErrorCode);
            request.Complete(result);
        }
    }

    public override void _ExitTree()
    {
        UnsubscribeSelectionEvents();
        StopPipeServer();

        base._ExitTree();
    }

    #endregion

    #region Events

    /// <summary>
    /// 選択状態変更イベントの購読を開始する。
    /// ModelHubはIpcHubより後に構築されるため、Process開始後に一度だけ購読する。
    /// </summary>
    private void SubscribeSelectionEvents()
    {
        if (_selectionEventsSubscribed)
        {
            return;
        }

        Application.Model.Entity.Selection.SelectionChanged += OnSelectionChanged;
        _selectionEventsSubscribed = true;
    }

    /// <summary>
    /// 選択状態変更イベントの購読を解除する。
    /// </summary>
    private void UnsubscribeSelectionEvents()
    {
        if (!_selectionEventsSubscribed)
        {
            return;
        }

        Application.Model.Entity.Selection.SelectionChanged -= OnSelectionChanged;
        _selectionEventsSubscribed = false;
    }

    /// <summary>
    /// 選択状態が変わった場合、接続中のIPCクライアントへ最新の選択ID一覧を通知する。
    /// </summary>
    private void OnSelectionChanged()
    {
        ConnectionSession connection = Volatile.Read(ref _activeConnection);
        if (connection == null)
        {
            return;
        }

        var payload = new ModelEntitySelectedPayload
        {
            EntityIds = Application.Model.Entity.Selection.SelectedIds
                .OrderBy(entityId => entityId)
                .ToList()
        };
        IpcEnvelope notification = IpcEnvelopeBuilder.Create(
            IpcEventType.FromViewer.ModelEntitySelected,
            ViewerSourceName,
            payload);
        if (!connection.TryEnqueue(notification))
        {
            Application.Log.Warn("Ipc: unable to queue ModelEntitySelected notification.");
        }
    }

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

        CancelPendingRequests();

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

        var connection = new ConnectionSession(writer);
        Volatile.Write(ref _activeConnection, connection);
        Task writeTask = connection.WriteLoopAsync(token);
        try
        {
            while (pipe.IsConnected && !token.IsCancellationRequested)
            {
                string line = await reader.ReadLineAsync().ConfigureAwait(false);
                if (line == null)
                {
                    break;
                }

                IpcEnvelope response = await ProcessLineAsync(line, token).ConfigureAwait(false);
                await connection.SendAndWaitAsync(response, token).ConfigureAwait(false);
            }
        }
        finally
        {
            Interlocked.CompareExchange(ref _activeConnection, null, connection);
            connection.Complete();
            await writeTask.ConfigureAwait(false);
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
        if (token.IsCancellationRequested)
        {
            request.TryCancelBeforeExecution();
        }

        Task<IpcResultPayload> completion = request.CompletionSource.Task;
        Task timeoutTask = Task.Delay(MainThreadTimeoutMilliseconds, token);
        Task completed = await Task.WhenAny(completion, timeoutTask).ConfigureAwait(false);

        if (completed == completion || completion.IsCompleted)
        {
            return BuildResponse(envelope, await completion.ConfigureAwait(false));
        }

        if (token.IsCancellationRequested)
        {
            request.TryCancelBeforeExecution();
            throw new OperationCanceledException(token);
        }

        IpcResultPayload result;
        if (request.TryCancelBeforeExecution())
        {
            result = IpcResultPayload.Failure(
                envelope.EventType,
                IpcErrorCode.MainThreadTimeout,
                "Main-thread processing did not start before the timeout; the request was not executed.");
        }
        else
        {
            // 実行開始後はメインスレッド処理を中断できないため、結果不明として返す。
            result = IpcResultPayload.Failure(
                envelope.EventType,
                IpcErrorCode.MainThreadTimeout,
                "Main-thread processing started but did not finish before the timeout; the outcome is unknown.");
        }

        return BuildResponse(envelope, result);
    }

    /// <summary>
    /// 停止時にキュー内の未実行要求を無効化する。
    /// </summary>
    private void CancelPendingRequests()
    {
        while (_pendingRequests.TryDequeue(out PendingRequest request))
        {
            if (request.TryCancelBeforeExecution())
            {
                request.CompletionSource.TrySetResult(IpcResultPayload.Failure(
                    request.Envelope.EventType,
                    IpcErrorCode.MainThreadTimeout,
                    "The IPC server stopped before the request was executed."));
            }
        }
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
    /// 接続中クライアントへの応答と通知を直列化して書き込むセッション。
    /// </summary>
    private sealed class ConnectionSession
    {
        /// <summary>接続先の書き込みストリーム。</summary>
        private readonly StreamWriter _writer;
        /// <summary>応答と通知を順番に送るキュー。</summary>
        private readonly Channel<OutboundMessage> _outgoing = Channel.CreateUnbounded<OutboundMessage>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });

        /// <summary>
        /// 接続セッションの書き込み先を設定する。
        /// </summary>
        /// <param name="writer">接続に対応するストリームライター</param>
        public ConnectionSession(StreamWriter writer)
        {
            _writer = writer;
        }

        /// <summary>
        /// 応答または非同期通知を送信キューへ追加する。
        /// </summary>
        /// <param name="envelope">送信するIPCエンベロープ</param>
        /// <returns>キューへ追加できた場合はtrue</returns>
        public bool TryEnqueue(IpcEnvelope envelope)
        {
            return _outgoing.Writer.TryWrite(new OutboundMessage(envelope, null));
        }

        /// <summary>
        /// IPC応答を送信キューへ追加し、書き込み完了を待機する。
        /// </summary>
        /// <param name="envelope">送信する応答エンベロープ</param>
        /// <param name="token">接続の取消しトークン</param>
        public async Task SendAndWaitAsync(IpcEnvelope envelope, CancellationToken token)
        {
            var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            await _outgoing.Writer.WriteAsync(new OutboundMessage(envelope, completion), token).ConfigureAwait(false);
            await completion.Task.WaitAsync(token).ConfigureAwait(false);
        }

        /// <summary>
        /// 送信キューを順次ストリームへ書き込む。
        /// </summary>
        /// <param name="token">接続の取消しトークン</param>
        public async Task WriteLoopAsync(CancellationToken token)
        {
            try
            {
                await foreach (OutboundMessage message in _outgoing.Reader.ReadAllAsync(token).ConfigureAwait(false))
                {
                    try
                    {
                        string json = JsonSerializer.Serialize(message.Envelope, IpcJsonOptions.Default);
                        await _writer.WriteLineAsync(json).ConfigureAwait(false);
                        message.Completion?.TrySetResult(true);
                    }
                    catch (Exception exception) when (exception is IOException or ObjectDisposedException)
                    {
                        message.Completion?.TrySetException(exception);
                        FailPending(exception);
                        Application.Log.Warn($"Ipc: client disconnected during message write. {exception.Message}");
                        return;
                    }
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                FailPending(new IOException("IPC connection was cancelled."));
            }
            finally
            {
                _outgoing.Writer.TryComplete();
                FailPending(new IOException("IPC connection was closed."));
            }
        }

        /// <summary>
        /// 新しい書き込みを停止し、キュー済みメッセージの送信を終える。
        /// </summary>
        public void Complete()
        {
            _outgoing.Writer.TryComplete();
        }

        /// <summary>
        /// 書き込みに失敗した場合、キュー内の応答待機を失敗させる。
        /// </summary>
        private void FailPending(Exception exception)
        {
            while (_outgoing.Reader.TryRead(out OutboundMessage message))
            {
                message.Completion?.TrySetException(exception);
            }
        }
    }

    /// <summary>
    /// 送信エンベロープと、必要に応じて応答書き込みの完了通知を保持する。
    /// </summary>
    private sealed class OutboundMessage
    {
        /// <summary>送信するIPCエンベロープ。</summary>
        public IpcEnvelope Envelope { get; }
        /// <summary>応答書き込み完了通知。非同期通知ではnull。</summary>
        public TaskCompletionSource<bool> Completion { get; }

        /// <summary>
        /// 送信エンベロープと完了通知を設定する。
        /// </summary>
        /// <param name="envelope">送信するIPCエンベロープ</param>
        /// <param name="completion">書き込み完了通知</param>
        public OutboundMessage(IpcEnvelope envelope, TaskCompletionSource<bool> completion)
        {
            Envelope = envelope;
            Completion = completion;
        }
    }

    /// <summary>
    /// 受信スレッドからメインスレッドへ引き渡す1件分のリクエスト。
    /// </summary>
    private sealed class PendingRequest
    {
        private const int Queued = 0;
        private const int Executing = 1;
        private const int Cancelled = 2;
        private const int Completed = 3;

        private int _state = Queued;

        public IpcEnvelope Envelope { get; }
        public TaskCompletionSource<IpcResultPayload> CompletionSource { get; }

        /// <summary>
        /// 要求がキュー待ちの場合に限り、メインスレッドでの実行権を取得する。
        /// </summary>
        /// <returns>実行を開始できる場合は true</returns>
        public bool TryStart()
        {
            return Interlocked.CompareExchange(ref _state, Executing, Queued) == Queued;
        }

        /// <summary>
        /// 要求がまだ開始されていない場合に限り、実行前に取り消す。
        /// </summary>
        /// <returns>実行前の取り消しに成功した場合は true</returns>
        public bool TryCancelBeforeExecution()
        {
            return Interlocked.CompareExchange(ref _state, Cancelled, Queued) == Queued;
        }

        /// <summary>
        /// 実行済みの要求結果を待機中の受信処理へ通知する。
        /// </summary>
        /// <param name="result">要求の処理結果</param>
        public void Complete(IpcResultPayload result)
        {
            CompletionSource.TrySetResult(result);
            Interlocked.Exchange(ref _state, Completed);
        }

        /// <summary>
        /// 受信エンベロープからメインスレッド処理要求を作成する。
        /// </summary>
        /// <param name="envelope">処理対象のIPCエンベロープ</param>
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
