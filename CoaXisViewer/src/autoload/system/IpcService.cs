using Godot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// IPCメッセージを受け取り、Viewer内部のイベントへ橋渡しするサービス
/// </summary>
public partial class IpcService : Node
{
    #region Fields

    public static IpcService Instance { get; private set; }
    private const int MainThreadResponseTimeoutMs = 5000;

    [ExportGroup("IPC")]
    [Export] private string _pipeName = "CoaXisViewerPipe";
    [Export] private bool _startPipeServerOnReady = true;

    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ConcurrentQueue<PendingRequest> _pendingRequests = new ConcurrentQueue<PendingRequest>();
    private readonly object _pipeLock = new object();
    private CancellationTokenSource _pipeCts;
    private Task _pipeServerTask;
    private NamedPipeServerStream _currentPipe;

    [Signal] public delegate void OutboundMessageReadyEventHandler(string message);

    private sealed class PendingRequest
    {
        public string RequestJson { get; init; }
        public TaskCompletionSource<IpcResultPayload> Completion { get; init; }
    }

    private sealed class IpcResultPayload
    {
        public bool Ok { get; init; }
        public string Request { get; init; }
        public string ErrorCode { get; init; }
        public string Message { get; init; }
    }

    #endregion

    #region Lifecycle

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _Ready()
    {
        // SettingsService が先に起動して読み込んだ値を IPC 起動設定へ反映する。
        ApplyExternalSettings();

        if (_startPipeServerOnReady)
        {
            StartPipeServer();
        }
    }

    public override void _Process(double delta)
    {
        while (_pendingRequests.TryDequeue(out PendingRequest pending))
        {
            IpcResultPayload result = HandleRequestOnMainThread(pending.RequestJson);
            pending.Completion.TrySetResult(result);
        }
    }

    public override void _ExitTree()
    {
        StopPipeServer();
        Instance = null;
    }

    #endregion

    #region Private Utilities

    /// <summary>
    /// 外部設定（viewer-settings.json）を IPC 実行時パラメータへ反映する。
    /// </summary>
    /// <remarks>
    /// 設定ファイルが存在しない/不正な場合でも SettingsService 側で既定値に
    /// フォールバックされるため、このメソッドは常に安全な値を受け取る想定。
    /// </remarks>
    private void ApplyExternalSettings()
    {
        ViewerSettings settings = SettingsService.Current ?? ViewerSettings.CreateDefault();
        settings.Normalize();

        // Export 値より外部設定を優先し、ビルド後でも運用変更できるようにする。
        _pipeName = settings.Ipc.PipeName;
        _startPipeServerOnReady = settings.Ipc.StartPipeServerOnReady;

        LogHub.Info($"IPC: settings applied. pipe='{_pipeName}', autoStart={_startPipeServerOnReady}");
    }

    #endregion

    #region Public API

    /// <summary>
    /// 受信したJSON文字列をIPCエンベロープとして解釈し、対応する処理を実行する
    /// </summary>
    /// <param name="json">受信メッセージJSON</param>
    /// <returns>処理成功時はtrue、失敗時はfalse</returns>
    public bool TryHandleIncomingMessage(string json)
    {
        return TryHandleIncomingMessage(json, out _);
    }

    private bool TryHandleIncomingMessage(string json, out IpcResultPayload result)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            result = CreateErrorResult(string.Empty, ViewerIpcErrorCode.EmptyMessage, "empty message received");
            LogHub.Warn("IPC: empty message received.");
            return false;
        }

        ViewerIpcEnvelope envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<ViewerIpcEnvelope>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            result = CreateErrorResult(string.Empty, ViewerIpcErrorCode.InvalidJson, ex.Message);
            LogHub.Error($"IPC: invalid json. {ex.Message}");
            return false;
        }

        if (envelope == null || string.IsNullOrWhiteSpace(envelope.Command))
        {
            result = CreateErrorResult(string.Empty, ViewerIpcErrorCode.MissingCommand, "command is missing");
            LogHub.Warn("IPC: command is missing.");
            return false;
        }

        try
        {
            if (ExecuteCommand(envelope.Command, envelope.Payload, out string errorCode, out string message))
            {
                result = CreateSuccessResult(envelope.Command, message);
                return true;
            }

            result = CreateErrorResult(envelope.Command, errorCode, message);
            return false;
        }
        catch (InvalidOperationException ex)
        {
            result = CreateErrorResult(envelope.Command, ViewerIpcErrorCode.InternalError, ex.Message);
            LogHub.Error($"IPC: invalid operation for command '{envelope.Command}'. {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            result = CreateErrorResult(envelope.Command, ViewerIpcErrorCode.InternalError, ex.Message);
            LogHub.Error($"IPC: unhandled exception for command '{envelope.Command}'. {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Editorへ送信用のイベントメッセージを作成して通知する
    /// </summary>
    /// <param name="eventName">イベント名</param>
    /// <param name="payload">イベントペイロード</param>
    public void EmitOutboundEvent(string eventName, object payload = null)
    {
        if (string.IsNullOrWhiteSpace(eventName))
        {
            return;
        }

        string json = JsonSerializer.Serialize(new { command = eventName, payload });
        EmitSignal(SignalName.OutboundMessageReady, json);
    }

    #endregion

    #region Internal Helpers

    private void StartPipeServer()
    {
        if (_pipeServerTask != null)
        {
            return;
        }

        _pipeCts = new CancellationTokenSource();
        _pipeServerTask = Task.Run(() => RunPipeServerLoopAsync(_pipeCts.Token));
        LogHub.Info($"IPC: NamedPipe server started. pipe='{_pipeName}'");
    }

    private void StopPipeServer()
    {
        if (_pipeCts == null)
        {
            return;
        }

        _pipeCts.Cancel();

        lock (_pipeLock)
        {
            _currentPipe?.Dispose();
        }

        try
        {
            _pipeServerTask?.Wait(500);
        }
        catch (AggregateException)
        {
        }

        _pipeServerTask = null;
        _pipeCts.Dispose();
        _pipeCts = null;
        _currentPipe = null;
    }

    private async Task RunPipeServerLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            NamedPipeServerStream server = null;
            try
            {
                server = new NamedPipeServerStream(
                    _pipeName,
                    PipeDirection.InOut,
                    1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                lock (_pipeLock)
                {
                    _currentPipe = server;
                }

                await server.WaitForConnectionAsync(cancellationToken);

                using StreamReader reader = new StreamReader(server);
                using StreamWriter writer = new StreamWriter(server) { AutoFlush = true };

                while (server.IsConnected && !cancellationToken.IsCancellationRequested)
                {
                    string requestJson = await reader.ReadLineAsync();
                    if (requestJson == null)
                    {
                        break;
                    }

                    TaskCompletionSource<IpcResultPayload> completion = new TaskCompletionSource<IpcResultPayload>(TaskCreationOptions.RunContinuationsAsynchronously);
                    _pendingRequests.Enqueue(new PendingRequest
                    {
                        RequestJson = requestJson,
                        Completion = completion
                    });

                    IpcResultPayload result;
                    try
                    {
                        result = await completion.Task.WaitAsync(TimeSpan.FromMilliseconds(MainThreadResponseTimeoutMs), cancellationToken);
                    }
                    catch (TimeoutException)
                    {
                        result = CreateErrorResult(string.Empty, ViewerIpcErrorCode.MainThreadTimeout, "main thread response timeout");
                    }

                    string responseJson = CreateResultEnvelopeJson(result);
                    await writer.WriteLineAsync(responseJson);
                    EmitSignal(SignalName.OutboundMessageReady, responseJson);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (IOException ex)
            {
                LogHub.Warn($"IPC pipe IO error: {ex.Message}");
            }
            finally
            {
                lock (_pipeLock)
                {
                    if (ReferenceEquals(_currentPipe, server))
                    {
                        _currentPipe = null;
                    }
                }

                server?.Dispose();
            }
        }
    }

    private IpcResultPayload HandleRequestOnMainThread(string json)
    {
        if (TryHandleIncomingMessage(json, out IpcResultPayload result))
        {
            return result;
        }

        return result;
    }

    private static string CreateResultEnvelopeJson(IpcResultPayload result)
    {
        return JsonSerializer.Serialize(new
        {
            command = ViewerIpcEvent.Result,
            payload = new
            {
                ok = result.Ok,
                request = result.Request,
                errorCode = result.ErrorCode,
                message = result.Message
            }
        });
    }

    private static IpcResultPayload CreateSuccessResult(string request, string message = "accepted")
    {
        return new IpcResultPayload
        {
            Ok = true,
            Request = request ?? string.Empty,
            ErrorCode = ViewerIpcErrorCode.None,
            Message = string.IsNullOrWhiteSpace(message) ? "accepted" : message
        };
    }

    private static IpcResultPayload CreateErrorResult(string request, string errorCode, string message)
    {
        return new IpcResultPayload
        {
            Ok = false,
            Request = request ?? string.Empty,
            ErrorCode = string.IsNullOrWhiteSpace(errorCode) ? ViewerIpcErrorCode.InternalError : errorCode,
            Message = string.IsNullOrWhiteSpace(message) ? "error" : message
        };
    }

    private bool ExecuteCommand(string command, JsonElement? payload, out string errorCode, out string message)
    {
        errorCode = ViewerIpcErrorCode.None;
        message = "accepted";

        switch (command)
        {
            case ViewerIpcCommand.LoadModel:
                return TryLoadModel(payload, out errorCode, out message);
            case ViewerIpcCommand.Select:
            case ViewerIpcCommand.Highlight:
                return TrySelectModels(payload, out errorCode, out message);
            case ViewerIpcCommand.ClearHighlight:
                ModelEventHub.RequestClearSelection();
                message = "selection cleared";
                return true;
            case ViewerIpcCommand.Hide:
                return TrySetVisibility(payload, false, out errorCode, out message);
            case ViewerIpcCommand.Show:
                return TrySetVisibility(payload, true, out errorCode, out message);
            case ViewerIpcCommand.Focus:
                return TryFocus(payload, out errorCode, out message);
            case ViewerIpcCommand.ApplyViewPreset:
                return TryApplyViewPreset(payload, out errorCode, out message);
            case ViewerIpcCommand.ApplyCameraPreset:
                return TryApplyCameraPreset(payload, out errorCode, out message);
            default:
                errorCode = ViewerIpcErrorCode.UnsupportedCommand;
                message = $"unsupported command '{command}'";
                LogHub.Warn($"IPC: unsupported command '{command}'.");
                return false;
        }
    }

    private static bool TryLoadModel(JsonElement? payload, out string errorCode, out string message)
    {
        errorCode = ViewerIpcErrorCode.None;
        message = "load model requested";

        if (!TryGetString(payload, "path", out string path))
        {
            errorCode = ViewerIpcErrorCode.InvalidPayload;
            message = "'path' is missing";
            LogHub.Warn("IPC LoadModel: 'path' is missing.");
            return false;
        }

        ModelEventHub.RequestLoadModel(path);
        return true;
    }

    private static bool TrySelectModels(JsonElement? payload, out string errorCode, out string message)
    {
        errorCode = ViewerIpcErrorCode.None;
        message = "select requested";

        List<AnyModel> models = FindModels(payload);
        if (models.Count == 0)
        {
            errorCode = ViewerIpcErrorCode.TargetNotFound;
            message = "target model is not found";
            LogHub.Warn("IPC Select: target model is not found.");
            return false;
        }

        if (models.Count == 1)
        {
            ModelEventHub.RequestSelectModel(models[0]);
        }
        else
        {
            ModelEventHub.RequestSelectModels(models.ToArray());
        }

        return true;
    }

    private static bool TrySetVisibility(JsonElement? payload, bool visible, out string errorCode, out string message)
    {
        errorCode = ViewerIpcErrorCode.None;
        message = visible ? "show requested" : "hide requested";

        List<AnyModel> models = FindModels(payload);
        if (models.Count == 0)
        {
            errorCode = ViewerIpcErrorCode.TargetNotFound;
            message = "target model is not found";
            LogHub.Warn("IPC Visibility: target model is not found.");
            return false;
        }

        foreach (AnyModel model in models)
        {
            model.Visible = visible;
        }

        return true;
    }

    private static bool TryFocus(JsonElement? payload, out string errorCode, out string message)
    {
        errorCode = ViewerIpcErrorCode.None;
        message = "focus requested";

        List<AnyModel> models = FindModels(payload);
        if (models.Count > 0)
        {
            ViewportEventHub.RequestFit(models, true);
            return true;
        }

        AnyModel[] selected = Selection.GetNodesArray();
        if (selected.Length == 0)
        {
            errorCode = ViewerIpcErrorCode.TargetNotFound;
            message = "no focus target found";
            LogHub.Warn("IPC Focus: no target found.");
            return false;
        }

        ViewportEventHub.RequestFit(selected, true);
        return true;
    }

    private static bool TryApplyViewPreset(JsonElement? payload, out string errorCode, out string message)
    {
        errorCode = ViewerIpcErrorCode.None;
        message = "view preset requested";

        if (!TryGetString(payload, "projection", out string projection))
        {
            errorCode = ViewerIpcErrorCode.InvalidPayload;
            message = "'projection' is missing";
            LogHub.Warn("IPC ApplyViewPreset: 'projection' is missing.");
            return false;
        }

        switch (projection.Trim().ToLowerInvariant())
        {
            case "perspective":
                ViewportEventHub.RequestSetProjectionType(Camera3D.ProjectionType.Perspective);
                message = "projection set to perspective";
                return true;
            case "orthogonal":
                ViewportEventHub.RequestSetProjectionType(Camera3D.ProjectionType.Orthogonal);
                message = "projection set to orthogonal";
                return true;
            case "toggle":
                ViewportEventHub.RequestToggleProjectionType();
                message = "projection toggled";
                return true;
            default:
                errorCode = ViewerIpcErrorCode.InvalidPayload;
                message = $"unsupported projection '{projection}'";
                LogHub.Warn($"IPC ApplyViewPreset: unsupported projection '{projection}'.");
                return false;
        }
    }

    private static bool TryApplyCameraPreset(JsonElement? payload, out string errorCode, out string message)
    {
        errorCode = ViewerIpcErrorCode.None;
        message = "camera preset requested";

        if (payload == null || payload.Value.ValueKind != JsonValueKind.Object)
        {
            errorCode = ViewerIpcErrorCode.InvalidPayload;
            message = "payload is missing";
            LogHub.Warn("IPC ApplyCameraPreset: payload is missing.");
            return false;
        }

        bool applied = false;
        if (TryGetFloat(payload, "distance", out float distance))
        {
            ViewportEventHub.RequestSetDistance(distance, true);
            applied = true;
        }

        if (TryGetFloat(payload, "size", out float size))
        {
            ViewportEventHub.RequestSetSizeTo(size, true);
            applied = true;
        }

        if (TryGetFloat(payload, "fov", out float fov))
        {
            ViewportEventHub.RequestSetFov(fov, true);
            applied = true;
        }

        if (!applied)
        {
            errorCode = ViewerIpcErrorCode.InvalidPayload;
            message = "no supported numeric fields found";
            LogHub.Warn("IPC ApplyCameraPreset: no supported numeric fields found.");
        }

        return applied;
    }

    private static List<AnyModel> FindModels(JsonElement? payload)
    {
        HashSet<string> names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (TryGetString(payload, "name", out string singleName))
        {
            names.Add(singleName);
        }

        if (payload != null
            && payload.Value.ValueKind == JsonValueKind.Object
            && payload.Value.TryGetProperty("names", out JsonElement namesElement)
            && namesElement.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement item in namesElement.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    string name = item.GetString();
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        names.Add(name);
                    }
                }
            }
        }

        if (names.Count == 0)
        {
            return new List<AnyModel>();
        }

        var result = new List<AnyModel>();
        Node root = Engine.GetMainLoop() is SceneTree tree ? tree.Root : null;
        if (root == null)
        {
            return result;
        }

        CollectModelsByName(root, names, result);
        return result;
    }

    private static void CollectModelsByName(Node node, HashSet<string> names, List<AnyModel> result)
    {
        if (node is AnyModel model && names.Contains(model.Name))
        {
            result.Add(model);
        }

        foreach (Node child in node.GetChildren())
        {
            CollectModelsByName(child, names, result);
        }
    }

    private static bool TryGetString(JsonElement? payload, string key, out string value)
    {
        value = string.Empty;
        if (payload == null || payload.Value.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (!payload.Value.TryGetProperty(key, out JsonElement valueElement) || valueElement.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        value = valueElement.GetString();
        return !string.IsNullOrWhiteSpace(value);
    }

    private static bool TryGetFloat(JsonElement? payload, string key, out float value)
    {
        value = 0.0f;
        if (payload == null || payload.Value.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (!payload.Value.TryGetProperty(key, out JsonElement valueElement))
        {
            return false;
        }

        if (valueElement.ValueKind == JsonValueKind.Number)
        {
            return valueElement.TryGetSingle(out value);
        }

        if (valueElement.ValueKind == JsonValueKind.String)
        {
            return float.TryParse(valueElement.GetString(), out value);
        }

        return false;
    }

    #endregion
}
