using System;
using System.Globalization;
using System.Text.Json;

namespace CoaXis.Protocol.Viewer;

/// <summary>
/// IPCメッセージの共通エンベロープを生成する
/// </summary>
public static class IpcEnvelopeBuilder
{
    /// <summary>
    /// 現行プロトコルバージョンを使ってIPCエンベロープを生成する
    /// </summary>
    /// <typeparam name="TPayload">エンベロープに格納するpayloadの型</typeparam>
    /// <param name="eventType">イベント種別</param>
    /// <param name="source">送信元</param>
    /// <param name="payload">エンベロープに格納するデータ</param>
    /// <param name="correlationId">関連する要求のイベントID。要求への応答でない場合は null</param>
    /// <returns>共通項目を設定したIPCエンベロープ</returns>
    public static IpcEnvelope Create<TPayload>(string eventType, string source, TPayload payload, string correlationId = null)
    {
        return new IpcEnvelope
        {
            EventId = Guid.NewGuid().ToString(),
            EventType = eventType,
            Version = IpcProtocolVersion.Current,
            Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
            Source = source,
            CorrelationId = correlationId,
            Payload = JsonSerializer.SerializeToElement(payload)
        };
    }
}