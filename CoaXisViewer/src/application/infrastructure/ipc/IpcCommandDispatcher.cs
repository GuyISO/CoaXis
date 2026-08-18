using System;
using System.Collections.Generic;
using System.Text.Json;

/// <summary>
/// eventType ごとの処理を振り分けるディスパッチャ
/// </summary>
/// <remarks>
/// Handler はドメイン層の Facade/Event を直接呼び出すため、必ずメインスレッドから Dispatch すること。
/// </remarks>
public static class IpcCommandDispatcher
{
    private delegate IpcResultPayload Handler(JsonElement payload);

    private static readonly Dictionary<string, Handler> Handlers = new()
    {
        ["LoadModel"] = HandleLoadModel
    };

    /// <summary>
    /// eventType に対応するハンドラを実行し、結果を返す
    /// </summary>
    /// <param name="envelope">受信したメッセージエンベロープ</param>
    public static IpcResultPayload Dispatch(IpcEnvelope envelope)
    {
        if (!Handlers.TryGetValue(envelope.EventType, out Handler handler))
        {
            return IpcResultPayload.Failure(envelope.EventType, IpcErrorCode.UnsupportedEventType, $"Unsupported eventType: {envelope.EventType}");
        }

        try
        {
            return handler(envelope.Payload);
        }
        catch (Exception ex)
        {
            Application.Log.Error($"Ipc: unhandled exception while dispatching '{envelope.EventType}'. {ex.Message}");
            return IpcResultPayload.Failure(envelope.EventType, IpcErrorCode.InternalError, ex.Message);
        }
    }

    /// <summary>
    /// LoadModel: JSON ファイルからモデル群を読み込み、Registry へ登録する
    /// </summary>
    /// <param name="payload">{ "path": string } を想定</param>
    private static IpcResultPayload HandleLoadModel(JsonElement payload)
    {
        if (payload.ValueKind != JsonValueKind.Object ||
            !payload.TryGetProperty("path", out JsonElement pathElement) ||
            pathElement.ValueKind != JsonValueKind.String)
        {
            return IpcResultPayload.Failure("LoadModel", IpcErrorCode.InvalidPayload, "payload.path (string) is required.");
        }

        string path = pathElement.GetString();
        if (string.IsNullOrWhiteSpace(path))
        {
            return IpcResultPayload.Failure("LoadModel", IpcErrorCode.InvalidPayload, "payload.path must not be empty.");
        }

        List<ModelDto> models = ModelJsonLoader.Load(path);
        if (models.Count == 0)
        {
            return IpcResultPayload.Failure("LoadModel", IpcErrorCode.TargetNotFound, $"No models loaded from '{path}'.");
        }

        foreach (ModelDto dto in models)
        {
            Application.Model.Factory.CreateFromDto(dto, dto.ParentId);
        }

        return IpcResultPayload.Success("LoadModel", $"Loaded {models.Count} model(s) from '{path}'.");
    }
}
