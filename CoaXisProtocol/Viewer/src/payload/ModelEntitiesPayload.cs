using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CoaXis.Protocol.Viewer;

/// <summary>
/// モデル実体の追加または置換要求payload。
/// </summary>
public sealed class ModelEntitiesPayload
{
    /// <summary>追加または置換するモデル実体DTO。</summary>
    [JsonRequired]
    public List<ModelEntityDto> Entities { get; init; } = new();
}
