using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CoaXis.Protocol.Viewer;

/// <summary>
/// モデル実体と属性を識別子で同時に指定する要求payload。
/// </summary>
public sealed class ModelSetIdsPayload
{
    /// <summary>対象のモデル実体の識別子。空の一覧を許可する。</summary>
    [JsonRequired]
    public List<Guid> EntityIds { get; init; } = new();

    /// <summary>対象のモデル属性の識別子。空の一覧を許可する。</summary>
    [JsonRequired]
    public List<Guid> PropertyIds { get; init; } = new();
}
