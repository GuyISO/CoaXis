// TODO: リファクタリング確認後に削除
using CoaXis.Protocol.Viewer;
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
    private delegate IpcResultPayload Handler(object payload);

    private static readonly Dictionary<string, Handler> Handlers = new()
    {
        [IpcEventType.ToViewer.LoadModel] = HandleLoadModel
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
            if (!IpcPayloadTypeMap.TryGetPayloadType(envelope.EventType, out Type payloadType))
            {
                return IpcResultPayload.Failure(envelope.EventType, IpcErrorCode.UnsupportedEventType, $"No payload contract is defined for eventType: {envelope.EventType}");
            }

            object typedPayload = envelope.Payload.Deserialize(payloadType, IpcJsonOptions.Default);
            return handler(typedPayload);
        }
        catch (JsonException ex)
        {
            return IpcResultPayload.Failure(envelope.EventType, IpcErrorCode.InvalidPayload, ex.Message);
        }
        catch (Exception ex)
        {
            Application.Log.Error($"Ipc: unhandled exception while dispatching '{envelope.EventType}'. {ex.Message}");
            return IpcResultPayload.Failure(envelope.EventType, IpcErrorCode.InternalError, ex.Message);
        }
    }

    /// <summary>
    /// LoadModel: payloadに含まれるモデル実体と属性をRegistryへ登録する
    /// </summary>
    /// <param name="payload">ModelSetPayload</param>
    private static IpcResultPayload HandleLoadModel(object payload)
    {
        ModelSetPayload modelSetPayload = payload as ModelSetPayload;
        if (modelSetPayload == null ||
            modelSetPayload.Entities == null ||
            modelSetPayload.Properties == null)
        {
            return IpcResultPayload.Failure(IpcEventType.ToViewer.LoadModel, IpcErrorCode.InvalidPayload, "payload.entities and payload.properties are required.");
        }

        if (modelSetPayload.Entities.Count == 0)
        {
            return IpcResultPayload.Failure(IpcEventType.ToViewer.LoadModel, IpcErrorCode.TargetNotFound, "payload.entities must contain at least one model.");
        }

        Application.Model.Load.Entity.ReplaceEntities(modelSetPayload.Entities);
        Application.Model.Load.Entity.LoadProperties(modelSetPayload.Properties);

        return IpcResultPayload.Success(
            IpcEventType.ToViewer.LoadModel,
            $"Loaded {modelSetPayload.Entities.Count} models and {modelSetPayload.Properties.Count} properties.");
    }
}
