using CoaXis.Protocol.Viewer;
using System;
using System.Collections.Generic;

/// <summary>
/// ModelEntityDto から ModelEntity を生成するファクトリ
/// </summary>
public static class ModelEntityFactory
{
    #region Public API

    /// <summary>
    /// ModelEntityDto の集合から ModelEntity を一括生成する。
    /// </summary>
    /// <param name="entityDtos">生成元となる DTO の集合</param>
    /// <returns>生成された ModelEntity の一覧</returns>
    public static IReadOnlyList<ModelEntity> Create(IReadOnlyList<ModelEntityDto> entityDtos)
    {
        if (entityDtos == null)
        {
            throw new ArgumentNullException(nameof(entityDtos));
        }

        var entities = new List<ModelEntity>(entityDtos.Count);
        var entityIds = new HashSet<Guid>();
        foreach (ModelEntityDto dto in entityDtos)
        {
            if (dto == null)
            {
                throw new ArgumentException("ModelEntityDto must not be null.", nameof(entityDtos));
            }

            if (dto.Id == Guid.Empty)
            {
                throw new ArgumentException("ModelEntityDto.Id must not be empty.", nameof(entityDtos));
            }

            if (!entityIds.Add(dto.Id))
            {
                throw new ArgumentException($"Duplicate ModelEntityDto.Id '{dto.Id}'.", nameof(entityDtos));
            }

            entities.Add(ModelEntityMapper.Map(dto));
        }

        return entities;
    }

    #endregion
}
