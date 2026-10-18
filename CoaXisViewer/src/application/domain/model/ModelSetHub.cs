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
    /// Entity DTO群を追加し、既存IDはDTOの値で置換する。
    /// </summary>
    /// <param name="entityDtos">追加または置換するEntity DTO</param>
    /// <returns>追加または置換したEntity数</returns>
    /// <exception cref="ArgumentNullException">entityDtosがnullの場合</exception>
    /// <exception cref="ArgumentException">DTOが不正、またはルートEntityを含む場合</exception>
    public int AddOrReplaceEntities(IReadOnlyList<ModelEntityDto> entityDtos)
    {
        IReadOnlyList<ModelEntity> existingEntities = Application.Model.Entity.Registry.Items.Values.ToList();
        IReadOnlyList<ModelEntity> entities = ModelFactory.CreateEntityUpdates(entityDtos, existingEntities);
        EnsureRootEntityIsNotIncluded(entities);
        Application.Model.Entity.Load.AddOrReplacePrepared(entities);
        Application.Model.Property.Registry.ResolveHierarchy();
        return entities.Count;
    }

    /// <summary>
    /// Property DTO群を追加し、既存IDはDTOの値で置換する。
    /// </summary>
    /// <param name="propertyDtos">追加または置換するProperty DTO</param>
    /// <returns>追加または置換したProperty数</returns>
    /// <exception cref="ArgumentNullException">propertyDtosがnullの場合</exception>
    public int AddOrReplaceProperties(IReadOnlyList<ModelPropertyDto> propertyDtos)
    {
        IReadOnlyList<ModelProperty> existingProperties = Application.Model.Property.Registry.Items.Values.ToList();
        IReadOnlyList<ModelProperty> properties = ModelFactory.CreatePropertyUpdates(propertyDtos, existingProperties);
        Application.Model.Property.Load.AddOrReplacePrepared(properties);
        return properties.Count;
    }

    /// <summary>
    /// 指定Entityとその子孫、および所属Propertyを削除する。
    /// </summary>
    /// <param name="entityIds">削除対象のEntity識別子</param>
    /// <returns>削除したEntity数</returns>
    /// <exception cref="ArgumentNullException">entityIdsがnullの場合</exception>
    /// <exception cref="ArgumentException">識別子一覧が空、不正、またはルートEntityを含む場合</exception>
    /// <exception cref="KeyNotFoundException">指定Entityが登録されていない場合</exception>
    public int DeleteEntities(IReadOnlyCollection<Guid> entityIds)
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
    /// 指定Propertyとその子孫を削除する。
    /// </summary>
    /// <param name="propertyIds">削除対象のProperty識別子</param>
    /// <returns>削除したProperty数</returns>
    /// <exception cref="ArgumentNullException">propertyIdsがnullの場合</exception>
    /// <exception cref="ArgumentException">識別子一覧が空または不正な場合</exception>
    /// <exception cref="KeyNotFoundException">指定Propertyが登録されていない場合</exception>
    public int DeleteProperties(IReadOnlyCollection<Guid> propertyIds)
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
