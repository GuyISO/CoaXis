using CoaXis.Protocol.Viewer;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// EntityとPropertyにまたがるモデル集合の変更を調整するハブ。
/// </summary>
public partial class ModelSetHub : BaseHub
{
    #region Fields

    #endregion

    #region Properties

    #endregion

    #region Signals

    #endregion

    #region Lifecycle

    #endregion

    #region Events

    #endregion

    #region Methods

    /// <summary>
    /// 検証済みモデル集合でEntityとPropertyを一括置換する。
    /// </summary>
    /// <param name="modelSet">検証済みモデル集合</param>
    /// <returns>置換したEntity数</returns>
    /// <exception cref="ArgumentNullException">modelSetがnullの場合</exception>
    /// <exception cref="ArgumentException">Entityが空、またはルートEntityを含む場合</exception>
    public int Replace(ModelSet modelSet)
    {
        if (modelSet == null)
        {
            throw new ArgumentNullException(nameof(modelSet));
        }

        if (modelSet.Entities.Count == 0)
        {
            throw new ArgumentException("At least one model entity is required.", nameof(modelSet));
        }

        EnsureRootEntityIsNotIncluded(modelSet.Entities);

        Application.Model.Property.Registry.Clear();
        Application.Model.Entity.Load.Clear();
        Application.Model.Entity.Load.AddOrReplacePrepared(modelSet.Entities);
        Application.Model.Property.Load.AddOrReplacePrepared(modelSet.Properties);
        return modelSet.Entities.Count;
    }

    /// <summary>
    /// EntityとPropertyをすべて削除する。ルートEntityは残る。
    /// </summary>
    /// <returns>削除したEntity数</returns>
    public int Clear()
    {
        int removedEntityCount = Application.Model.Entity.Registry.Items.Keys
            .Count(entityId => entityId != RootModelEntity.RootEntityId);

        Application.Model.Property.Registry.Clear();
        Application.Model.Entity.Load.Clear();
        return removedEntityCount;
    }

    /// <summary>
    /// EntityとProperty DTO群を同時に追加する。既存IDと衝突する場合は何も変更しない。
    /// </summary>
    /// <param name="entityDtos">追加するEntity DTO</param>
    /// <param name="propertyDtos">追加するProperty DTO</param>
    /// <returns>追加したEntity数とProperty数</returns>
    /// <exception cref="ArgumentNullException">いずれかのDTO集合がnullの場合</exception>
    /// <exception cref="ArgumentException">DTOが不正、既存IDと衝突、ルートEntityを含む、または両方が空の場合</exception>
    public (int EntityCount, int PropertyCount) Add(
        IReadOnlyList<ModelEntityDto> entityDtos,
        IReadOnlyList<ModelPropertyDto> propertyDtos)
    {
        IReadOnlyList<ModelEntity> entities = ModelFactory.CreateEntityAdditions(
            entityDtos,
            Application.Model.Entity.Registry.Items.Values.ToList());
        IReadOnlyList<ModelProperty> properties = ModelFactory.CreatePropertyAdditions(
            propertyDtos,
            Application.Model.Property.Registry.Items.Values.ToList());
        if (entities.Count == 0 && properties.Count == 0)
        {
            throw new ArgumentException("At least one entity or property is required.", nameof(entityDtos));
        }

        EnsureRootEntityIsNotIncluded(entities);

        if (entities.Count > 0)
        {
            Application.Model.Entity.Load.AddOrReplacePrepared(entities);
            Application.Model.Property.Registry.ResolveHierarchy();
            Application.Model.Property.Load.SyncParticles();
        }

        if (properties.Count > 0)
        {
            Application.Model.Property.Load.AddOrReplacePrepared(properties);
        }

        return (entities.Count, properties.Count);
    }

    /// <summary>
    /// EntityとPropertyを同時に削除する。削除前にすべてのIDを検証し、不正な場合は何も変更しない。
    /// </summary>
    /// <param name="entityIds">削除対象のEntity識別子。空を許可</param>
    /// <param name="propertyIds">削除対象のProperty識別子。空を許可</param>
    /// <returns>削除したEntity数と、指定されたPropertyの削除数</returns>
    /// <exception cref="ArgumentNullException">いずれかの識別子一覧がnullの場合</exception>
    /// <exception cref="ArgumentException">識別子が不正、両方が空、またはルートEntityを含む場合</exception>
    /// <exception cref="KeyNotFoundException">指定対象が登録されていない場合</exception>
    public (int EntityCount, int PropertyCount) Remove(
        IReadOnlyCollection<Guid> entityIds,
        IReadOnlyCollection<Guid> propertyIds)
    {
        HashSet<Guid> entityRoots = ValidateOptionalIds(
            entityIds,
            Application.Model.Entity.Registry.Items.Keys,
            nameof(entityIds));
        HashSet<Guid> propertyRoots = ValidateOptionalIds(
            propertyIds,
            Application.Model.Property.Registry.Items.Keys,
            nameof(propertyIds));
        if (entityRoots.Count == 0 && propertyRoots.Count == 0)
        {
            throw new ArgumentException("At least one entity id or property id is required.", nameof(entityIds));
        }

        if (entityRoots.Contains(RootModelEntity.RootEntityId))
        {
            throw new ArgumentException("The root ModelEntity cannot be deleted.", nameof(entityIds));
        }

        // Entity削除は所属Propertyも削除するため、指定Propertyを先に削除して二重削除を避ける。
        int propertyCount = propertyRoots.Count > 0 ? RemovePropertiesCore(propertyRoots) : 0;
        int entityCount = entityRoots.Count > 0 ? RemoveEntitiesCore(entityRoots) : 0;
        return (entityCount, propertyCount);
    }

    /// <summary>
    /// EntityとPropertyの値を同時に部分更新する。更新前にすべてを検証し、不正な場合は何も変更しない。
    /// </summary>
    /// <param name="entityDtos">Entity部分更新DTO</param>
    /// <param name="propertyDtos">Property部分更新DTO</param>
    /// <returns>更新したEntity数とProperty数</returns>
    /// <exception cref="ArgumentNullException">いずれかのDTO集合がnullの場合</exception>
    /// <exception cref="ArgumentException">DTOが不正、ルートEntityを含む、または両方が空の場合</exception>
    /// <exception cref="KeyNotFoundException">指定対象が登録されていない場合</exception>
    public (int EntityCount, int PropertyCount) Modify(
        IReadOnlyList<ModelEntityModificationDto> entityDtos,
        IReadOnlyList<ModelPropertyModificationDto> propertyDtos)
    {
        IReadOnlyList<ModelEntityModification> entityModifications = ModelFactory.CreateEntityModifications(entityDtos);
        IReadOnlyList<ModelPropertyModification> propertyModifications = ModelFactory.CreatePropertyModifications(propertyDtos);
        if (entityModifications.Count == 0 && propertyModifications.Count == 0)
        {
            throw new ArgumentException("At least one entity or property modification is required.", nameof(entityDtos));
        }

        var entities = new List<(ModelEntity Entity, ModelEntityModification Modification)>(entityModifications.Count);
        foreach (ModelEntityModification modification in entityModifications)
        {
            if (modification.Id == RootModelEntity.RootEntityId)
            {
                throw new ArgumentException("The root ModelEntity cannot be modified.", nameof(entityDtos));
            }

            ModelEntity entity = Application.Model.Entity.Registry.Get(modification.Id)
                ?? throw new KeyNotFoundException($"Model target '{modification.Id}' was not found.");
            entities.Add((entity, modification));
        }

        var properties = new List<(ModelProperty Property, ModelPropertyModification Modification)>(propertyModifications.Count);
        foreach (ModelPropertyModification modification in propertyModifications)
        {
            ModelProperty property = Application.Model.Property.Registry.Get(modification.Id)
                ?? throw new KeyNotFoundException($"Model target '{modification.Id}' was not found.");
            properties.Add((property, modification));
        }

        foreach ((ModelEntity entity, ModelEntityModification modification) in entities)
        {
            ApplyModification(entity, modification);
        }

        foreach ((ModelProperty property, ModelPropertyModification modification) in properties)
        {
            ApplyModification(property, modification);
        }

        return (entities.Count, properties.Count);
    }

    /// <summary>
    /// 指定したEntityとPropertyをDTOとして読み出す。指定されたIDのみを対象とし、子孫や所属Propertyは含めない。
    /// </summary>
    /// <param name="entityIds">読み出すEntity識別子。空を許可</param>
    /// <param name="propertyIds">読み出すProperty識別子。空を許可</param>
    /// <returns>読み出したEntityとPropertyのDTO</returns>
    /// <exception cref="ArgumentNullException">いずれかの識別子一覧がnullの場合</exception>
    /// <exception cref="ArgumentException">識別子が不正、または両方が空の場合</exception>
    /// <exception cref="KeyNotFoundException">指定対象が登録されていない場合</exception>
    public ModelSetPayload Read(
        IReadOnlyCollection<Guid> entityIds,
        IReadOnlyCollection<Guid> propertyIds)
    {
        HashSet<Guid> entityIdSet = ValidateOptionalIds(
            entityIds,
            Application.Model.Entity.Registry.Items.Keys,
            nameof(entityIds));
        HashSet<Guid> propertyIdSet = ValidateOptionalIds(
            propertyIds,
            Application.Model.Property.Registry.Items.Keys,
            nameof(propertyIds));
        if (entityIdSet.Count == 0 && propertyIdSet.Count == 0)
        {
            throw new ArgumentException("At least one entity id or property id is required.", nameof(entityIds));
        }

        return new ModelSetPayload
        {
            Entities = entityIds
                .Distinct()
                .Select(id => ModelEntityMapper.ToDto(Application.Model.Entity.Registry.Get(id)))
                .ToList(),
            Properties = propertyIds
                .Distinct()
                .Select(id => ModelPropertyMapper.ToDto(Application.Model.Property.Registry.Get(id)))
                .ToList()
        };
    }

    /// <summary>
    /// RemoveModelSetの検証済みEntity識別子群を削除する。
    /// </summary>
    /// <param name="entityIds">削除対象のEntity識別子</param>
    /// <returns>削除したEntity数</returns>
    private int RemoveEntitiesCore(IReadOnlyCollection<Guid> entityIds)
    {
        HashSet<Guid> roots = ValidateIds(
            entityIds,
            Application.Model.Entity.Registry.Items.Keys,
            nameof(entityIds));
        if (roots.Contains(RootModelEntity.RootEntityId))
        {
            throw new ArgumentException("The root ModelEntity cannot be deleted.", nameof(entityIds));
        }

        HashSet<Guid> entityIdsToDelete = ExpandEntityDescendants(roots);
        HashSet<Guid> ownedPropertyIds = GetPropertiesOwnedBy(entityIdsToDelete);
        if (ownedPropertyIds.Count > 0)
        {
            Application.Model.Property.Load.DeleteCascade(ownedPropertyIds);
        }

        Application.Model.Entity.Load.DeletePrepared(entityIdsToDelete);
        return entityIdsToDelete.Count;
    }

    /// <summary>
    /// RemoveModelSetの検証済みProperty識別子群を削除する。
    /// </summary>
    /// <param name="propertyIds">削除対象のProperty識別子</param>
    /// <returns>削除したProperty数</returns>
    private int RemovePropertiesCore(IReadOnlyCollection<Guid> propertyIds)
    {
        HashSet<Guid> roots = ValidateIds(
            propertyIds,
            Application.Model.Property.Registry.Items.Keys,
            nameof(propertyIds));
        return Application.Model.Property.Load.DeleteCascade(roots).Count;
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Entityの削除対象をParentIdに基づいて子孫へ展開する。
    /// </summary>
    private static HashSet<Guid> ExpandEntityDescendants(HashSet<Guid> roots)
    {
        var entityIds = new HashSet<Guid>(roots);
        bool added;
        do
        {
            added = false;
            foreach (ModelEntity entity in Application.Model.Entity.Registry.Items.Values)
            {
                if (entityIds.Contains(entity.ParentId) && entityIds.Add(entity.Id))
                {
                    added = true;
                }
            }
        }
        while (added);

        return entityIds;
    }

    /// <summary>
    /// 指定Entity群を所有元とするPropertyを検索する。
    /// </summary>
    private static HashSet<Guid> GetPropertiesOwnedBy(ISet<Guid> entityIds)
    {
        var propertyIds = new HashSet<Guid>();
        foreach (ModelProperty property in Application.Model.Property.Registry.Items.Values)
        {
            var visitedPropertyIds = new HashSet<Guid>();
            ModelProperty current = property;
            while (current != null && visitedPropertyIds.Add(current.Id))
            {
                if (entityIds.Contains(current.ParentId))
                {
                    propertyIds.Add(property.Id);
                    break;
                }

                current = Application.Model.Property.Registry.Get(current.ParentId);
            }
        }

        return propertyIds;
    }

    /// <summary>
    /// ID一覧が空でもよい場合に、含まれるIDがすべて登録済みであることを検証する。
    /// </summary>
    private static HashSet<Guid> ValidateOptionalIds(
        IReadOnlyCollection<Guid> ids,
        IEnumerable<Guid> registeredIds,
        string parameterName)
    {
        if (ids == null)
        {
            throw new ArgumentNullException(parameterName);
        }

        return ids.Count == 0 ? new HashSet<Guid>() : ValidateIds(ids, registeredIds, parameterName);
    }

    /// <summary>
    /// 検証済みの部分更新をEntityへ反映する。
    /// </summary>
    private static void ApplyModification(ModelEntity entity, ModelEntityModification modification)
    {
        if (modification.Position.HasValue)
        {
            entity.Position = modification.Position.Value;
        }

        if (modification.Rotation.HasValue)
        {
            entity.Rotation = modification.Rotation.Value;
        }

        if (modification.Visibility.HasValue)
        {
            entity.Visibility = modification.Visibility.Value;
        }

        if (modification.IsCollapsed.HasValue)
        {
            entity.IsCollapsed = modification.IsCollapsed.Value;
        }

        if (modification.IsPropertyTreeVisible.HasValue)
        {
            entity.IsPropertyTreeVisible = modification.IsPropertyTreeVisible.Value;
        }
    }

    /// <summary>
    /// 検証済みの部分更新をPropertyへ反映する。
    /// </summary>
    private static void ApplyModification(ModelProperty property, ModelPropertyModification modification)
    {
        if (modification.Value != null)
        {
            property.SetValue(modification.Value);
        }

        if (modification.Visibility.HasValue)
        {
            property.Visibility = modification.Visibility.Value;
        }

        if (modification.ChangesParticleColor)
        {
            property.SetParticleColor(modification.ParticleColor);
        }
    }

    /// <summary>
    /// ID一覧が非空で、すべて登録済みであることを検証する。
    /// </summary>
    private static HashSet<Guid> ValidateIds(
        IReadOnlyCollection<Guid> ids,
        IEnumerable<Guid> registeredIds,
        string parameterName)
    {
        if (ids == null)
        {
            throw new ArgumentNullException(parameterName);
        }

        if (ids.Count == 0)
        {
            throw new ArgumentException("At least one id is required.", parameterName);
        }

        var requestedIds = new HashSet<Guid>();
        foreach (Guid id in ids)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException("Ids must not contain an empty identifier.", parameterName);
            }

            requestedIds.Add(id);
        }

        var registeredIdSet = new HashSet<Guid>(registeredIds);
        Guid missingId = requestedIds.FirstOrDefault(id => !registeredIdSet.Contains(id));
        if (missingId != Guid.Empty)
        {
            throw new KeyNotFoundException($"Model target '{missingId}' was not found.");
        }

        return requestedIds;
    }

    /// <summary>
    /// ルートEntityがEntity集合へ含まれていないことを検証する。
    /// </summary>
    private static void EnsureRootEntityIsNotIncluded(IEnumerable<ModelEntity> entities)
    {
        if (entities.Any(entity => entity.Id == RootModelEntity.RootEntityId))
        {
            throw new ArgumentException("The root ModelEntity cannot be replaced.", nameof(entities));
        }
    }

    #endregion
}
