using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CoaXis.Protocol.Viewer;

/// <summary>
/// 選択中ModelEntityの識別子一覧を通知するpayload。
/// </summary>
public sealed class ModelEntitySelectedPayload
{
    /// <summary>選択中のモデル実体識別子。</summary>
    [JsonRequired]
    public List<Guid> EntityIds { get; init; } = new();
}
