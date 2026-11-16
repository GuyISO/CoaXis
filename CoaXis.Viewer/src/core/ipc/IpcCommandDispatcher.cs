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

    #region Fields

    private static readonly Dictionary<string, Handler> Handlers = new()
    {
        [IpcEventType.ToViewer.ReplaceModelSet] = HandleReplaceModelSet,
        [IpcEventType.ToViewer.ClearModelSet] = HandleClearModelSet,
        [IpcEventType.ToViewer.AddModelSet] = HandleAddModelSet,
        [IpcEventType.ToViewer.RemoveModelSet] = HandleRemoveModelSet,
        [IpcEventType.ToViewer.ModifyModelSet] = HandleModifyModelSet,
        [IpcEventType.ToViewer.ReadModelSet] = HandleReadModelSet
    };

    #endregion

    #region Methods

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

            // 引数のない要求ではpayload省略を許可する
            object typedPayload = envelope.Payload.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null && payloadType == typeof(IpcEmptyPayload)
                ? new IpcEmptyPayload()
                : envelope.Payload.Deserialize(payloadType, IpcJsonOptions.Default);
            return handler(typedPayload);
        }
        catch (JsonException ex)
        {
            return IpcResultPayload.Failure(envelope.EventType, IpcErrorCode.InvalidPayload, ex.Message);
        }
        catch (ArgumentException ex)
        {
            return IpcResultPayload.Failure(envelope.EventType, IpcErrorCode.InvalidPayload, ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            return IpcResultPayload.Failure(envelope.EventType, IpcErrorCode.TargetNotFound, ex.Message);
        }
        catch (Exception ex)
        {
            Application.Log.Error($"Ipc: unhandled exception while dispatching '{envelope.EventType}'. {ex.Message}");
            return IpcResultPayload.Failure(envelope.EventType, IpcErrorCode.InternalError, ex.Message);
        }
    }
    #endregion

    #region Helpers

    /// <summary>
    /// ReplaceModelSet: payloadに含まれるモデル実体と属性で現在の集合を置換する
    /// </summary>
    /// <param name="payload">ModelSetPayload</param>
    private static IpcResultPayload HandleReplaceModelSet(object payload)
    {
        ModelSetPayload modelSetPayload = payload as ModelSetPayload;
        if (modelSetPayload == null ||
            modelSetPayload.Entities == null ||
            modelSetPayload.Properties == null)
        {
            return IpcResultPayload.Failure(IpcEventType.ToViewer.ReplaceModelSet, IpcErrorCode.InvalidPayload, "payload.entities and payload.properties are required.");
        }

        if (modelSetPayload.Entities.Count == 0)
        {
            return IpcResultPayload.Failure(IpcEventType.ToViewer.ReplaceModelSet, IpcErrorCode.TargetNotFound, "payload.entities must contain at least one model.");
        }

        ModelSet modelSet = ModelFactory.CreateModelSet(modelSetPayload.Entities, modelSetPayload.Properties);
        Application.Model.ModelSet.Replace(modelSet);

        return IpcResultPayload.Success(
            IpcEventType.ToViewer.ReplaceModelSet,
            $"Loaded {modelSet.Entities.Count} models and {modelSet.Properties.Count} properties.");
    }

    /// <summary>
    /// ClearModelSet: 現在のモデル実体と属性をすべて削除する
    /// </summary>
    /// <param name="payload">IpcEmptyPayload</param>
    private static IpcResultPayload HandleClearModelSet(object payload)
    {
        int count = Application.Model.ModelSet.Clear();
        return IpcResultPayload.Success(
            IpcEventType.ToViewer.ClearModelSet,
            $"Cleared {count} models and their properties.");
    }

    /// <summary>
    /// AddModelSet: モデル実体と属性を同時に追加し、既存IDと衝突した場合は拒否する
    /// </summary>
    /// <param name="payload">ModelSetPayload</param>
    private static IpcResultPayload HandleAddModelSet(object payload)
    {
        if (payload is not ModelSetPayload modelSetPayload ||
            modelSetPayload.Entities == null ||
            modelSetPayload.Properties == null)
        {
            return IpcResultPayload.Failure(
                IpcEventType.ToViewer.AddModelSet,
                IpcErrorCode.InvalidPayload,
                "payload.entities and payload.properties are required.");
        }

        (int entityCount, int propertyCount) = Application.Model.ModelSet.Add(
            modelSetPayload.Entities,
            modelSetPayload.Properties);
        return IpcResultPayload.Success(
            IpcEventType.ToViewer.AddModelSet,
            $"Added or replaced {entityCount} models and {propertyCount} properties.");
    }

    /// <summary>
    /// RemoveModelSet: 指定したモデル実体と属性を同時に削除する
    /// </summary>
    /// <param name="payload">ModelSetIdsPayload</param>
    private static IpcResultPayload HandleRemoveModelSet(object payload)
    {
        if (payload is not ModelSetIdsPayload idsPayload ||
            idsPayload.EntityIds == null ||
            idsPayload.PropertyIds == null)
        {
            return IpcResultPayload.Failure(
                IpcEventType.ToViewer.RemoveModelSet,
                IpcErrorCode.InvalidPayload,
                "payload.entityIds and payload.propertyIds are required.");
        }

        (int entityCount, int propertyCount) = Application.Model.ModelSet.Remove(
            idsPayload.EntityIds,
            idsPayload.PropertyIds);
        return IpcResultPayload.Success(
            IpcEventType.ToViewer.RemoveModelSet,
            $"Removed {entityCount} models (including descendants) and {propertyCount} properties.");
    }

    /// <summary>
    /// ModifyModelSet: モデル実体と属性の値を同時に部分更新する
    /// </summary>
    /// <param name="payload">ModelSetModificationPayload</param>
    private static IpcResultPayload HandleModifyModelSet(object payload)
    {
        if (payload is not ModelSetModificationPayload modificationPayload ||
            modificationPayload.Entities == null ||
            modificationPayload.Properties == null)
        {
            return IpcResultPayload.Failure(
                IpcEventType.ToViewer.ModifyModelSet,
                IpcErrorCode.InvalidPayload,
                "payload.entities and payload.properties are required.");
        }

        (int entityCount, int propertyCount) = Application.Model.ModelSet.Modify(
            modificationPayload.Entities,
            modificationPayload.Properties);
        return IpcResultPayload.Success(
            IpcEventType.ToViewer.ModifyModelSet,
            $"Modified {entityCount} models and {propertyCount} properties.");
    }

    /// <summary>
    /// ReadModelSet: 指定したモデル実体と属性のDTOをResultに含めて返す
    /// </summary>
    /// <param name="payload">ModelSetIdsPayload</param>
    private static IpcResultPayload HandleReadModelSet(object payload)
    {
        if (payload is not ModelSetIdsPayload idsPayload ||
            idsPayload.EntityIds == null ||
            idsPayload.PropertyIds == null)
        {
            return IpcResultPayload.Failure(
                IpcEventType.ToViewer.ReadModelSet,
                IpcErrorCode.InvalidPayload,
                "payload.entityIds and payload.propertyIds are required.");
        }

        ModelSetPayload modelSet = Application.Model.ModelSet.Read(idsPayload.EntityIds, idsPayload.PropertyIds);
        return ReadModelSetResultPayload.Success(
            IpcEventType.ToViewer.ReadModelSet,
            $"Read {modelSet.Entities.Count} models and {modelSet.Properties.Count} properties.",
            modelSet);
    }

    #endregion
}
