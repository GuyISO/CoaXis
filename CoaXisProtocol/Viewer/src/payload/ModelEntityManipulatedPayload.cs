using System;
using System.Text.Json.Serialization;

namespace CoaXis.Protocol.Viewer;

/// <summary>
/// Manipulate操作の対象ModelEntityを通知するpayload。
/// </summary>
public sealed class ModelEntityManipulatedPayload
{
    /// <summary>操作対象のモデル実体識別子。</summary>
    [JsonRequired]
    public Guid EntityId { get; init; }
}
