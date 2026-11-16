using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CoaXis.Protocol.Viewer;

/// <summary>
/// モデル実体と属性の値を同時に部分更新する要求payload。
/// </summary>
public sealed class ModelSetModificationPayload
{
    /// <summary>変更するモデル実体DTOの一覧。空の一覧を許可する。</summary>
    [JsonRequired]
    public List<ModelEntityModificationDto> Entities { get; init; } = new();

    /// <summary>変更するモデル属性DTOの一覧。空の一覧を許可する。</summary>
    [JsonRequired]
    public List<ModelPropertyModificationDto> Properties { get; init; } = new();
}
