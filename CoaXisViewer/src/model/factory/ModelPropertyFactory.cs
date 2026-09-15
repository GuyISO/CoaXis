using CoaXis.Protocol.Viewer;
using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// ModelPropertyDto から ModelProperty を生成し、ModelRegistry へ登録・階層解決を行うファクトリ
/// </summary>
public partial class ModelPropertyFactory : Node
{
    #region Public API

    /// <summary>
    /// ModelPropertyDto の集合から ModelProperty を一括生成し、Registry に登録して階層を解決する。
    /// </summary>
    /// <param name="propertyDtos">生成元となる Property DTO の集合</param>
    /// <returns>生成された ModelProperty の一覧</returns>
    public IReadOnlyList<ModelProperty> CreateProperties(IReadOnlyList<ModelPropertyDto> propertyDtos)
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

            properties.Add(CreateModelProperty(dto));
        }

        // 全件を登録してから階層を解決することで、入力順に依存せず親子関係を確定する。
        foreach (ModelProperty property in properties)
        {
            Application.Model.Registry.RegisterProperty(property);
        }
        Application.Model.Registry.ResolvePropertyHierarchy();

        return properties;
    }

    #endregion

    #region Internal Helpers

    private static ModelProperty CreateModelProperty(ModelPropertyDto dto)
    {
        Guid resolvedParentId = dto.ParentId ?? Guid.Empty;
        return new ModelProperty(
            dto.Id,
            resolvedParentId,
            dto.PropertyType,
            dto.ValueType,
            dto.Value);
    }

    #endregion
}
