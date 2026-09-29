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
        [IpcEventType.ToViewer.LoadModel] = typeof(ModelSetPayload),
        
        // FromViewerイベントのpayload型
        // 未定義
        
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
