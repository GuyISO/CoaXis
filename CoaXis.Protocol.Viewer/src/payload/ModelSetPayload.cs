using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CoaXis.Protocol.Viewer;

/// <summary>
/// モデル読み込み要求のpayload
/// </summary>
public sealed class ModelSetPayload
{
    /// <summary>
    /// 読み込むモデル実体DTOの一覧
    /// </summary>
    [JsonRequired]
    public List<ModelEntityDto> Entities { get; init; } = new List<ModelEntityDto>();

    /// <summary>
    /// 読み込むモデル属性DTOの一覧
    /// </summary>
    [JsonRequired]
    public List<ModelPropertyDto> Properties { get; init; } = new List<ModelPropertyDto>();

    /// <summary>
    /// モデル実体と属性のDTO一覧からpayloadを生成する
    /// </summary>
    /// <param name="entities">読み込むモデル実体DTO。1件以上必要</param>
    /// <param name="properties">読み込むモデル属性DTO。空の一覧を許可</param>
    /// <returns>入力一覧をコピーしたモデル集合payload</returns>
    /// <exception cref="ArgumentNullException">いずれかの一覧がnullの場合</exception>
    /// <exception cref="ArgumentException">entitiesが空の場合</exception>
    public static ModelSetPayload Create(
        IReadOnlyList<ModelEntityDto> entities,
        IReadOnlyList<ModelPropertyDto> properties)
    {
        if (entities == null)
        {
            throw new ArgumentNullException(nameof(entities));
        }

        if (properties == null)
        {
            throw new ArgumentNullException(nameof(properties));
        }

        if (entities.Count == 0)
        {
            throw new ArgumentException("At least one model entity is required.", nameof(entities));
        }

        return new ModelSetPayload
        {
            Entities = new List<ModelEntityDto>(entities),
            Properties = new List<ModelPropertyDto>(properties)
        };
    }
}
