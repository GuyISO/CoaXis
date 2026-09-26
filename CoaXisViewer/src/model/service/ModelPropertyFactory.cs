using CoaXis.Protocol.Viewer;
using System;
using System.Collections.Generic;

/// <summary>
/// ModelPropertyDto から ModelProperty を生成するファクトリ
/// </summary>
public static class ModelPropertyFactory
{
    #region Public API

    /// <summary>
    /// ModelPropertyDto の集合から ModelProperty を一括生成する。
    /// </summary>
    /// <param name="propertyDtos">生成元となる Property DTO の集合</param>
    /// <returns>生成された ModelProperty の一覧</returns>
    public static IReadOnlyList<ModelProperty> Create(IReadOnlyList<ModelPropertyDto> propertyDtos)
    {
        if (propertyDtos == null)
        {
            throw new ArgumentNullException(nameof(propertyDtos));
        }

        var properties = new List<ModelProperty>(propertyDtos.Count);
        var propertyIds = new HashSet<Guid>();
        foreach (ModelPropertyDto dto in propertyDtos)
        {
            if (dto == null)
            {
                throw new ArgumentException("ModelPropertyDto must not be null.", nameof(propertyDtos));
            }

            if (dto.Id == Guid.Empty)
            {
                throw new ArgumentException("ModelPropertyDto.Id must not be empty.", nameof(propertyDtos));
            }

            if (!propertyIds.Add(dto.Id))
            {
                throw new ArgumentException($"Duplicate ModelPropertyDto.Id '{dto.Id}'.", nameof(propertyDtos));
            }

            properties.Add(ModelPropertyMapper.Map(dto));
        }

        return properties;
    }

    #endregion

}
