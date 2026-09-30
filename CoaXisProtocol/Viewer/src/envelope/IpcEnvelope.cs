using System;
using System.Text.Json;

namespace CoaXis.Protocol.Viewer;

/// <summary>
/// Godot(Viewer) と外部クライアント間でやり取りする IPC メッセージの共通エンベロープ
/// IpcEnvelopeBuilder を使用して生成することを推奨する
/// </summary>
/// <remarks>
/// プロパティ名は<see cref="IpcJsonOptions.Default"/>のcamelCase命名ポリシーに委ねる
/// </remarks>
public sealed class IpcEnvelope
{
    public string EventId { get; set; }
    public string EventType { get; set; }
    public string Version { get; set; }
    public string Timestamp { get; set; }
    public string Source { get; set; }
    public string CorrelationId { get; set; }
    public JsonElement Payload { get; set; }
}
