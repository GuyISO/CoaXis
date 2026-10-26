using System;
using System.Collections.Generic;

namespace CoaXis.Protocol.Viewer;

/// <summary>
/// IPCイベントとpayload CLR型の対応を管理する
/// </summary>
public static class IpcPayloadTypeMap
{
    private static readonly IReadOnlyDictionary<string, Type> PayloadTypes = new Dictionary<string, Type>(StringComparer.Ordinal)
    {
        // ToViewerイベントのpayload型
        [IpcEventType.ToViewer.ReplaceModelSet] = typeof(ModelSetPayload),
        [IpcEventType.ToViewer.ClearModelSet] = typeof(IpcEmptyPayload),
        [IpcEventType.ToViewer.AddModelSet] = typeof(ModelSetPayload),
        [IpcEventType.ToViewer.RemoveModelSet] = typeof(ModelSetIdsPayload),
        [IpcEventType.ToViewer.ModifyModelSet] = typeof(ModelSetModificationPayload),
        [IpcEventType.ToViewer.ReadModelSet] = typeof(ModelSetIdsPayload),
        // FromViewerイベントのpayload型
        [IpcEventType.FromViewer.ModelEntitySelected] = typeof(ModelEntitySelectedPayload),
        // Commonイベントのpayload型
        [IpcEventType.Common.Result] = typeof(IpcResultPayload)
    };

    /// <summary>
    /// イベント種別に対応するpayload型を取得する
    /// </summary>
    /// <param name="eventType">イベント種別</param>
    /// <param name="payloadType">対応するpayload型。対応がない場合は null</param>
    /// <returns>対応するpayload型が登録済みの場合は true</returns>
    public static bool TryGetPayloadType(string eventType, out Type payloadType)
    {
        return PayloadTypes.TryGetValue(eventType ?? string.Empty, out payloadType);
    }
}
