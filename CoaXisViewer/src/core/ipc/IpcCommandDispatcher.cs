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
        [IpcEventType.ToViewer.AddOrReplaceModelEntities] = HandleAddOrReplaceModelEntities,
        [IpcEventType.ToViewer.AddOrReplaceModelProperties] = HandleAddOrReplaceModelProperties,
        [IpcEventType.ToViewer.DeleteModelEntities] = HandleDeleteModelEntities,
        [IpcEventType.ToViewer.DeleteModelProperties] = HandleDeleteModelProperties
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

            object typedPayload = envelope.Payload.Deserialize(payloadType, IpcJsonOptions.Default);
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
    /// ModelEntity DTO群を追加または同じIDのEntityへ置換する。
    /// </summary>
    /// <param name="payload">Entity DTO群payload</param>
    private static IpcResultPayload HandleAddOrReplaceModelEntities(object payload)
    {
        if (payload is not ModelEntitiesPayload entitiesPayload || entitiesPayload.Entities == null)
        {
            return IpcResultPayload.Failure(
                IpcEventType.ToViewer.AddOrReplaceModelEntities,
                IpcErrorCode.InvalidPayload,
                "payload.entities is required.");
        }

        int count = Application.Model.ModelSet.AddOrReplaceEntities(entitiesPayload.Entities);
        return IpcResultPayload.Success(
            IpcEventType.ToViewer.AddOrReplaceModelEntities,
            $"Added or replaced {count} models.");
    }

    /// <summary>
    /// ModelProperty DTO群を追加または同じIDのPropertyへ置換する。
    /// </summary>
    /// <param name="payload">Property DTO群payload</param>
    private static IpcResultPayload HandleAddOrReplaceModelProperties(object payload)
    {
        if (payload is not ModelPropertiesPayload propertiesPayload || propertiesPayload.Properties == null)
        {
            return IpcResultPayload.Failure(
                IpcEventType.ToViewer.AddOrReplaceModelProperties,
                IpcErrorCode.InvalidPayload,
                "payload.properties is required.");
        }

        int count = Application.Model.ModelSet.AddOrReplaceProperties(propertiesPayload.Properties);
        return IpcResultPayload.Success(
            IpcEventType.ToViewer.AddOrReplaceModelProperties,
            $"Added or replaced {count} properties.");
    }

    /// <summary>
    /// 指定Entityと子孫Entity、所属Propertyを削除する。
    /// </summary>
    /// <param name="payload">削除対象ID群payload</param>
    private static IpcResultPayload HandleDeleteModelEntities(object payload)
    {
        if (payload is not ModelIdsPayload idsPayload || idsPayload.Ids == null)
        {
            return IpcResultPayload.Failure(
                IpcEventType.ToViewer.DeleteModelEntities,
                IpcErrorCode.InvalidPayload,
                "payload.ids is required.");
        }

        int count = Application.Model.ModelSet.DeleteEntities(idsPayload.Ids);
        return IpcResultPayload.Success(
            IpcEventType.ToViewer.DeleteModelEntities,
            $"Deleted {count} models and their properties.");
    }

    /// <summary>
    /// 指定Propertyと子孫Propertyを削除する。
    /// </summary>
    /// <param name="payload">削除対象ID群payload</param>
    private static IpcResultPayload HandleDeleteModelProperties(object payload)
    {
        if (payload is not ModelIdsPayload idsPayload || idsPayload.Ids == null)
        {
            return IpcResultPayload.Failure(
                IpcEventType.ToViewer.DeleteModelProperties,
                IpcErrorCode.InvalidPayload,
                "payload.ids is required.");
        }

        int count = Application.Model.ModelSet.DeleteProperties(idsPayload.Ids);
        return IpcResultPayload.Success(
            IpcEventType.ToViewer.DeleteModelProperties,
            $"Deleted {count} properties.");
    }

    #endregion
}
