// TODO: リファクタリング確認後に削除
using CoaXis.Protocol.Viewer;
using System;

/// <summary>
/// モデル属性DTOをModelPropertyへ変換する
/// </summary>
public static class ModelPropertyMapper
{
    /// <summary>
    /// DTOをModelPropertyへ変換する
    /// </summary>
    /// <param name="dto">変換元のモデル属性DTO</param>
    /// <returns>変換済みのModelProperty</returns>
    /// <exception cref="ArgumentNullException">dtoがnullの場合</exception>
    public static ModelProperty Map(ModelPropertyDto dto)
    {
        if (dto == null)
        {
            throw new ArgumentNullException(nameof(dto));
        }

        Guid resolvedParentId = dto.ParentId ?? Guid.Empty;
        return new ModelProperty(
            dto.Id,
            resolvedParentId,
            dto.PropertyType,
            dto.ValueType,
            dto.Value);
    }
}
