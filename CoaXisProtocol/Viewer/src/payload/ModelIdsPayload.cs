using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CoaXis.Protocol.Viewer;

/// <summary>
/// 識別子で対象を指定する削除要求payload。
/// </summary>
public sealed class ModelIdsPayload
{
    /// <summary>削除するモデル実体または属性の識別子。</summary>
    [JsonRequired]
    public List<Guid> Ids { get; init; } = new();
}
