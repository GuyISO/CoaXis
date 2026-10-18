using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CoaXis.Protocol.Viewer;

/// <summary>
/// モデル属性の追加または置換要求payload。
/// </summary>
public sealed class ModelPropertiesPayload
{
    /// <summary>追加または置換するモデル属性DTO。</summary>
    [JsonRequired]
    public List<ModelPropertyDto> Properties { get; init; } = new();
}
