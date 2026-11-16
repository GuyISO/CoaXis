using System;
using System.Text.Json.Serialization;

namespace CoaXis.Protocol.Viewer;

/// <summary>
/// Manipulate操作の対象ModelPropertyを通知するpayload。
/// </summary>
public sealed class ModelPropertyManipulatedPayload
{
    /// <summary>操作対象のモデル属性識別子。</summary>
    [JsonRequired]
    public Guid PropertyId { get; init; }
}
