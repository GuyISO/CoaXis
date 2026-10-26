using CoaXis.Protocol.Viewer;
using System;
using System.Collections.Generic;

/// <summary>
/// 外部DTOから検証済みのモデルを生成するファクトリ。
/// </summary>
public static class ModelFactory
{
    #region Methods

    /// <summary>
    /// EntityとPropertyのDTOを検証し、モデル集合を生成する。
    /// </summary>
    /// <param name="entityDtos">生成元となるモデル実体DTO</param>
    /// <param name="propertyDtos">生成元となるモデル属性DTO</param>
    /// <returns>検証済みモデル集合</returns>
    /// <exception cref="ArgumentNullException">いずれかのDTO集合がnullの場合</exception>
    /// <exception cref="ArgumentException">DTOの値、識別子、または階層が不正な場合</exception>
    public static ModelSet CreateModelSet(
        IReadOnlyList<ModelEntityDto> entityDtos,
        IReadOnlyList<ModelPropertyDto> propertyDtos)
    {
        IReadOnlyList<ModelEntity> entities = CreateEntities(entityDtos, nameof(entityDtos));
        IReadOnlyList<ModelProperty> properties = CreateProperties(propertyDtos, nameof(propertyDtos));
        return new ModelSet(entities, properties);
    }

    /// <summary>
    /// Entity部分更新DTOを検証し、更新内容へ変換する。
    /// </summary>
    /// <param name="entityDtos">部分更新DTO</param>
    /// <returns>検証済みの更新内容</returns>
    /// <exception cref="ArgumentNullException">DTO集合がnullの場合</exception>
    /// <exception cref="ArgumentException">DTOの値または識別子が不正な場合</exception>
    public static IReadOnlyList<ModelEntityModification> CreateEntityModifications(
        IReadOnlyList<ModelEntityModificationDto> entityDtos)
    {
        return CreateModels(
            entityDtos,
            ModelEntityMapper.MapModification,
            modification => modification.Id,
            "ModelEntityModificationDto",
            nameof(entityDtos));
    }

    /// <summary>
    /// Property部分更新DTOを検証し、更新内容へ変換する。
    /// </summary>
    /// <param name="propertyDtos">部分更新DTO</param>
    /// <returns>検証済みの更新内容</returns>
    /// <exception cref="ArgumentNullException">DTO集合がnullの場合</exception>
    /// <exception cref="ArgumentException">DTOの値または識別子が不正な場合</exception>
    public static IReadOnlyList<ModelPropertyModification> CreatePropertyModifications(
        IReadOnlyList<ModelPropertyModificationDto> propertyDtos)
    {
        return CreateModels(
            propertyDtos,
            ModelPropertyMapper.MapModification,
            modification => modification.Id,
            "ModelPropertyModificationDto",
            nameof(propertyDtos));
    }

    /// <summary>
    /// Entity DTO群を既存Entityへ追加した結果の階層を検証し、追加モデルを生成する。
    /// </summary>
    /// <param name="entityDtos">追加するEntity DTO</param>
    /// <param name="existingEntities">現在のEntity集合</param>
    /// <returns>検証済みの追加用Entity</returns>
    /// <exception cref="ArgumentNullException">いずれかの集合がnullの場合</exception>
    /// <exception cref="ArgumentException">DTO、既存IDとの重複、または追加後のEntity階層が不正な場合</exception>
    internal static IReadOnlyList<ModelEntity> CreateEntityAdditions(
        IReadOnlyList<ModelEntityDto> entityDtos,
        IReadOnlyCollection<ModelEntity> existingEntities)
    {
        if (existingEntities == null)
        {
            throw new ArgumentNullException(nameof(existingEntities));
        }

        IReadOnlyList<ModelEntity> additions = CreateEntities(entityDtos, nameof(entityDtos));
        var mergedById = new Dictionary<Guid, ModelEntity>();
        foreach (ModelEntity entity in existingEntities)
        {
            mergedById.Add(entity.Id, entity);
        }

        foreach (ModelEntity entity in additions)
        {
            if (mergedById.ContainsKey(entity.Id))
            {
                throw new ArgumentException(
                    $"ModelEntity ID '{entity.Id}' is already registered.",
                    nameof(entityDtos));
            }

            mergedById.Add(entity.Id, entity);
        }

        ValidateHierarchy(
            new List<ModelEntity>(mergedById.Values),
            entity => entity.Id,
            entity => entity.ParentId,
            "ModelEntity");
        return additions;
    }

    /// <summary>
    /// Property DTO群を既存Propertyへ追加した結果の階層を検証し、追加モデルを生成する。
    /// </summary>
    /// <param name="propertyDtos">追加するProperty DTO</param>
    /// <param name="existingProperties">現在のProperty集合</param>
    /// <returns>検証済みの追加用Property</returns>
    /// <exception cref="ArgumentNullException">いずれかの集合がnullの場合</exception>
    /// <exception cref="ArgumentException">DTO、既存IDとの重複、または追加後のProperty階層が不正な場合</exception>
    internal static IReadOnlyList<ModelProperty> CreatePropertyAdditions(
        IReadOnlyList<ModelPropertyDto> propertyDtos,
        IReadOnlyCollection<ModelProperty> existingProperties)
    {
        if (existingProperties == null)
        {
            throw new ArgumentNullException(nameof(existingProperties));
        }

        IReadOnlyList<ModelProperty> additions = CreateProperties(propertyDtos, nameof(propertyDtos));
        var mergedById = new Dictionary<Guid, ModelProperty>();
        foreach (ModelProperty property in existingProperties)
        {
            mergedById.Add(property.Id, property);
        }

        foreach (ModelProperty property in additions)
        {
            if (mergedById.ContainsKey(property.Id))
            {
                throw new ArgumentException(
                    $"ModelProperty ID '{property.Id}' is already registered.",
                    nameof(propertyDtos));
            }

            mergedById.Add(property.Id, property);
        }

        ValidateHierarchy(
            new List<ModelProperty>(mergedById.Values),
            property => property.Id,
            property => property.ParentId,
            "ModelProperty");
        return additions;
    }

    /// <summary>
    /// Entity DTO群を内部モデルへ変換し、識別子と階層を検証する。
    /// </summary>
    private static IReadOnlyList<ModelEntity> CreateEntities(
        IReadOnlyList<ModelEntityDto> entityDtos,
        string parameterName)
    {
        IReadOnlyList<ModelEntity> entities = CreateModels(
            entityDtos,
            ModelEntityMapper.Map,
            entity => entity.Id,
            "ModelEntityDto",
            parameterName);
        ValidateHierarchy(entities, entity => entity.Id, entity => entity.ParentId, "ModelEntity");
        return entities;
    }

    /// <summary>
    /// Property DTO群を内部モデルへ変換し、識別子と階層を検証する。
    /// </summary>
    private static IReadOnlyList<ModelProperty> CreateProperties(
        IReadOnlyList<ModelPropertyDto> propertyDtos,
        string parameterName)
    {
        IReadOnlyList<ModelProperty> properties = CreateModels(
            propertyDtos,
            ModelPropertyMapper.Map,
            property => property.Id,
            "ModelPropertyDto",
            parameterName);
        ValidateHierarchy(properties, property => property.Id, property => property.ParentId, "ModelProperty");
        return properties;
    }

    /// <summary>
    /// DTOのnull・空ID・重複IDを検証し、対応する内部モデルへ変換する。
    /// </summary>
    private static IReadOnlyList<TModel> CreateModels<TDto, TModel>(
        IReadOnlyList<TDto> dtos,
        Func<TDto, TModel> map,
        Func<TModel, Guid> getId,
        string modelName,
        string parameterName)
        where TDto : class
        where TModel : class
    {
        if (dtos == null)
        {
            throw new ArgumentNullException(parameterName);
        }

        var models = new List<TModel>(dtos.Count);
        var ids = new HashSet<Guid>();
        foreach (TDto dto in dtos)
        {
            if (dto == null)
            {
                throw new ArgumentException($"{modelName} must not be null.", parameterName);
            }

            TModel model = map(dto);
            Guid id = getId(model);
            if (id == Guid.Empty)
            {
                throw new ArgumentException($"{modelName}.Id must not be empty.", parameterName);
            }

            if (!ids.Add(id))
            {
                throw new ArgumentException($"Duplicate {modelName}.Id '{id}'.", parameterName);
            }

            models.Add(model);
        }

        return models;
    }

    /// <summary>
    /// 親IDをたどり、モデル階層に循環がないことを検証する。
    /// </summary>
    private static void ValidateHierarchy<TModel>(
        IReadOnlyList<TModel> models,
        Func<TModel, Guid> getId,
        Func<TModel, Guid> getParentId,
        string modelName)
        where TModel : class
    {
        var modelsById = new Dictionary<Guid, TModel>(models.Count);
        foreach (TModel model in models)
        {
            modelsById.Add(getId(model), model);
        }

        var visiting = new HashSet<Guid>();
        var visited = new HashSet<Guid>();
        foreach (TModel model in models)
        {
            VisitHierarchy(model, modelsById, getId, getParentId, visiting, visited, modelName);
        }
    }

    /// <summary>
    /// モデルの親階層を再帰的にたどり、循環を検出する。
    /// </summary>
    private static void VisitHierarchy<TModel>(
        TModel model,
        IReadOnlyDictionary<Guid, TModel> modelsById,
        Func<TModel, Guid> getId,
        Func<TModel, Guid> getParentId,
        ISet<Guid> visiting,
        ISet<Guid> visited,
        string modelName)
        where TModel : class
    {
        Guid id = getId(model);
        if (visited.Contains(id))
        {
            return;
        }

        if (!visiting.Add(id))
        {
            throw new ArgumentException($"Circular {modelName} hierarchy detected at '{id}'.");
        }

        Guid parentId = getParentId(model);
        if (modelsById.TryGetValue(parentId, out TModel parent))
        {
            VisitHierarchy(parent, modelsById, getId, getParentId, visiting, visited, modelName);
        }

        visiting.Remove(id);
        visited.Add(id);
    }

    #endregion
}
