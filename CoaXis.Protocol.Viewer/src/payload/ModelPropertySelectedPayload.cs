using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CoaXis.Protocol.Viewer;

/// <summary>
/// 選択中ModelPropertyの識別子一覧を通知するpayload。
/// </summary>
public sealed class ModelPropertySelectedPayload
{
    /// <summary>選択中のモデル属性識別子。</summary>
    [JsonRequired]
    public List<Guid> PropertyIds { get; init; } = new();
}
