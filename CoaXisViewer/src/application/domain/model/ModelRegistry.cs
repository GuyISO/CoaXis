using System;
using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// モデル実体（ModelEntity）と属性（ModelProperty）のインスタンスおよびUUIDを一元管理する Autoload ノード
/// RootModelEntity をルートとして管理する
/// </summary>
public partial class ModelRegistry : Node
{
    #region Fields

    // Guid をキーにして ModelEntity を管理する辞書
    private readonly Dictionary<Guid, ModelEntity> _entities = new();

    // Guid をキーにして ModelProperty を管理する辞書
    private readonly Dictionary<Guid, ModelProperty> _properties = new();

    // 親へまだリンクできていない ModelEntity の Id 一覧。入力順を保持し、Treeの兄弟順を安定させる。
    private readonly List<Guid> _unlinkedIds = new();

    // 親へまだリンクできていない ModelProperty の Id 集合
    private readonly HashSet<Guid> _unlinkedPropertyIds = new();

    private RootModelEntity _rootEntity = null!;

    #endregion

    #region Properties

    // 登録されている ModelEntity の集合を取得する
    public IReadOnlyDictionary<Guid, ModelEntity> Entities => _entities;

    // 登録されている ModelProperty の集合を取得する
    public IReadOnlyDictionary<Guid, ModelProperty> Properties => _properties;

    /// <summary>
    /// シーン全体のルート ModelEntity を取得する
    /// </summary>
    public RootModelEntity RootEntity => _rootEntity;

    #endregion

    #region Lifecycle

    /// <summary>
    /// レジストリのルート ModelEntity を初期化する
    /// </summary>
    public override void _Ready()
    {
        _rootEntity = new RootModelEntity();
        AddChild(_rootEntity.Node);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// ModelEntity を取得する。存在しない場合は null を返す。
    /// </summary>
    /// <param name="entityId">取得対象の ModelEntity の Id</param>
    /// <returns>該当する ModelEntity。存在しない場合は null</returns>
    public ModelEntity GetEntity(Guid entityId)
    {
        return _entities.TryGetValue(entityId, out ModelEntity entity) ? entity : null;
    }

    /// <summary>
    /// ModelProperty を取得する。存在しない場合は null を返す。
    /// </summary>
    /// <param name="propertyId">取得対象の ModelProperty の Id</param>
    /// <returns>該当する ModelProperty。存在しない場合は null</returns>
    public ModelProperty GetProperty(Guid propertyId)
    {
        return _properties.TryGetValue(propertyId, out ModelProperty property) ? property : null;
    }

    /// <summary>
    /// 指定した ModelEntity の直接の親を取得する。
    /// </summary>
    /// <param name="entityId">親を取得する ModelEntity の識別子</param>
    /// <returns>登録済みの親 ModelEntity。親がない場合や未解決の場合は null</returns>
    public ModelEntity GetParentEntity(Guid entityId)
    {
        ModelEntity entity = GetEntity(entityId);
        if (entity == null || entity.ParentId == Guid.Empty)
        {
            return null;
        }

        return GetEntity(entity.ParentId);
    }

    /// <summary>
    /// 指定した ModelEntity の直接の親からルート方向へ祖先を取得する。
    /// </summary>
    /// <param name="entityId">祖先を取得する ModelEntity の識別子</param>
    /// <returns>直接の親から順に並んだ祖先 ModelEntity の一覧</returns>
    public IReadOnlyList<ModelEntity> GetAncestorEntities(Guid entityId)
    {
        var ancestors = new List<ModelEntity>();
        var visitedEntityIds = new HashSet<Guid>();
        ModelEntity currentEntity = GetEntity(entityId);

        while (currentEntity != null && visitedEntityIds.Add(currentEntity.Id))
        {
            ModelEntity parentEntity = GetParentEntity(currentEntity.Id);
            if (parentEntity == null)
            {
                break;
            }

            // 循環参照では同じ Entity を一覧へ重複追加せず、取得可能な祖先で停止する。
            if (!visitedEntityIds.Add(parentEntity.Id))
            {
                break;
            }

            ancestors.Add(parentEntity);
            currentEntity = parentEntity;
        }

        return ancestors;
    }

    /// <summary>
    /// 指定した ModelEntity の配下にある子孫 ModelEntity を、子から孫へたどる順番で取得する。
    /// </summary>
    /// <param name="entityId">子孫を取得する ModelEntity の識別子</param>
    /// <returns>直接の子を先に並べ、その後に各子の配下を並べた子孫 ModelEntity の一覧</returns>
    public IReadOnlyList<ModelEntity> GetDescendantEntities(Guid entityId)
    {
        var descendants = new List<ModelEntity>();
        var visitedEntityIds = new HashSet<Guid>();
        ModelEntity entity = GetEntity(entityId);
        if (entity == null)
        {
            return descendants;
        }

        visitedEntityIds.Add(entity.Id);
        CollectDescendantEntities(entity, descendants, visitedEntityIds);
        return descendants;
    }

    /// <summary>
    /// 指定した ModelProperty が所属する ModelEntity を取得する。
    /// </summary>
    /// <param name="propertyId">所属 Entity を取得する ModelProperty の識別子</param>
    /// <returns>所属する ModelEntity。未登録または未解決の場合は null</returns>
    public ModelEntity GetOwningEntity(Guid propertyId)
    {
        ModelProperty property = GetProperty(propertyId);
        var visitedPropertyIds = new HashSet<Guid>();

        while (property != null && visitedPropertyIds.Add(property.Id))
        {
            ModelEntity parentEntity = GetEntity(property.ParentId);
            if (parentEntity != null)
            {
                return parentEntity;
            }

            property = GetProperty(property.ParentId);
        }

        return null;
    }

    /// <summary>
    /// ModelEntity が登録されているかどうかを判定する。
    /// </summary>
    /// <param name="entityId">判定対象の ModelEntity の Id</param>
    /// <returns>登録済みの場合は true</returns>
    public bool IsEntityRegistered(Guid entityId)
    {
        return _entities.ContainsKey(entityId);
    }

    /// <summary>
    /// ModelProperty が登録されているかどうかを判定する。
    /// </summary>
    /// <param name="propertyId">判定対象の ModelProperty の Id</param>
    /// <returns>登録済みの場合は true</returns>
    public bool IsPropertyRegistered(Guid propertyId)
    {
        return _properties.ContainsKey(propertyId);
    }

    /// <summary>
    /// ModelEntity を登録する。親が登録済みの場合は直ちに親へリンクし、未登録の場合は親が登録されるまで待機する。
    /// すでに同じ Id の ModelEntity が登録されている場合は、既存の ModelEntity を置き換える。
    /// </summary>
    /// <param name="modelEntity">登録対象の ModelEntity</param>
    /// <exception cref="ArgumentNullException">entity が null の場合</exception>
    /// <exception cref="ArgumentException">entity.Id が空または Status が Initialized でない場合</exception>
    public void RegisterEntity(ModelEntity entity)
    {
        // レジストリは「新規に生成された未登録モデル」だけを受け入れる
        // 既存データの再利用や終了済みデータを混ぜると、再読込時に状態が壊れるため、ここで明確に弾く
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        if (entity.Id == Guid.Empty)
        {
            throw new ArgumentException("ModelEntity id must not be empty.", nameof(entity));
        }

        if (entity.Status != ModelStatus.Initialized)
        {
            throw new ArgumentException("ModelEntity must be in Initialized status.", nameof(entity));
        }

        if (IsEntityRegistered(entity.Id))
        {
            // すでに登録されている場合は、既存の ModelEntity を置き換える
            DisposeEntity(entity.Id);
        }

        // 親が登録済みなら即時にリンクし、未登録の場合だけ後続の階層解決へ回す
        _entities.Add(entity.Id, entity);
        entity.Status = ModelStatus.Registered;
        if (!LinkEntityToParent(entity))
        {
            _unlinkedIds.Add(entity.Id);
        }
    }

    /// <summary>
    /// ModelProperty を登録する。親が未登録の場合は、親が登録されるまでリンク待機する。
    /// すでに同じ Id の ModelProperty が登録されている場合は、既存の ModelProperty を置き換える。
    /// </summary>
    /// <param name="property">登録対象の ModelProperty</param>
    /// <exception cref="ArgumentNullException">property が null の場合</exception>
    /// <exception cref="ArgumentException">property.Id が空の場合</exception>
    public void RegisterProperty(ModelProperty property)
    {
        // レジストリは有効なプロパティのみを受け入れる
        if (property == null)
        {
            throw new ArgumentNullException(nameof(property));
        }

        if (property.Id == Guid.Empty)
        {
            throw new ArgumentException("ModelProperty id must not be empty.", nameof(property));
        }

        if (IsPropertyRegistered(property.Id))
        {
            // すでに登録されている場合は既存のプロパティを置き換える
            DisposeProperty(property.Id);
        }

        // 親が登録済みなら即時にリンクし、未登録の場合だけ後続の階層解決へ回す
        _properties.Add(property.Id, property);
        if (!LinkPropertyToParent(property))
        {
            _unlinkedPropertyIds.Add(property.Id);
        }
    }

    /// <summary>
    /// ModelEntity 自身を破棄し、直下の子とルートプロパティは親未登録として保持する。
    /// </summary>
    /// <param name="entityId">破棄対象の ModelEntity の Id</param>
    /// <returns>削除に成功した場合は true</returns>
    public bool DisposeEntity(Guid entityId)
    {
        if (!_entities.TryGetValue(entityId, out ModelEntity entity))
        {
            return false;
        }

        // 直下の子だけ親から外し、親が再登録されるまでリンク待ちへ戻す
        foreach (ModelEntity child in entity.Children.ToList())
        {
            entity.DetachEntity(child);
            if (!_unlinkedIds.Contains(child.Id))
            {
                _unlinkedIds.Add(child.Id);
            }
        }

        // ルートプロパティも削除せず、対象モデルとの参照だけを外してリンク待ちへ戻す
        foreach (ModelProperty property in entity.Properties.ToList())
        {
            entity.DetachProperty(property);
            _unlinkedPropertyIds.Add(property.Id);
        }

        // 親からの参照に登録済みの場合は解除する
        ModelEntity parent = GetEntity(entity.ParentId);
        parent?.DetachEntity(entity);

        // レジストリから削除する
        _entities.Remove(entityId);
        _unlinkedIds.Remove(entityId);
        entity.Status = ModelStatus.Disposed;

        return true;
    }

    /// <summary>
    /// ModelProperty を破棄し、レジストリおよび親子関係から解除する。
    /// </summary>
    /// <param name="propertyId">破棄対象の ModelProperty の Id</param>
    /// <returns>削除に成功した場合は true</returns>
    public bool DisposeProperty(Guid propertyId)
    {
        if (!_properties.TryGetValue(propertyId, out ModelProperty property))
        {
            return false;
        }

        // 子孫の ModelProperty から先に削除する
        foreach (ModelProperty child in property.Children.ToList())
        {
            property.Detach(child);
            if (!_unlinkedPropertyIds.Contains(child.Id))
            {
                _unlinkedPropertyIds.Add(child.Id);
            }
        }

        // 親プロパティまたは親モデル実体からの参照を解除する
        if (property.ParentId != Guid.Empty)
        {
            if (_properties.TryGetValue(property.ParentId, out ModelProperty parentProperty))
            {
                parentProperty.Detach(property);
            }
            else if (_entities.TryGetValue(property.ParentId, out ModelEntity parentEntity))
            {
                parentEntity.DetachProperty(property);
            }
        }

        // レジストリから削除する
        _properties.Remove(propertyId);
        _unlinkedPropertyIds.Remove(propertyId);

        return true;
    }

    /// <summary>
    /// 未解決の ModelEntity と ModelProperty の親子関係を解決する。
    /// </summary>
    public void ResolveHierarchy()
    {
        // 先に親概念である ModelEntity の親子関係を解決する
        foreach (Guid id in _unlinkedIds.ToList())
        {
            if (_entities.TryGetValue(id, out ModelEntity entity) && LinkEntityToParent(entity))
            {
                _unlinkedIds.Remove(id);
            }
        }

        foreach (Guid id in _unlinkedPropertyIds.ToList())
        {
            if (_properties.TryGetValue(id, out ModelProperty property) && LinkPropertyToParent(property))
            {
                _unlinkedPropertyIds.Remove(id);
            }
        }
    }

    /// <summary>
    /// 登録済みのモデルとプロパティをルートを残してクリアする
    /// </summary>
    public void Clear()
    {
        // 再読み込み時に古い非同期タスクが後からモデルを更新しないよう、
        // まず待機中のロード処理を止めてから実体を削除する。
        Application.Model.EntityFactory.ClearPendingLoads();

        var entityIds = new List<Guid>(_entities.Keys);
        foreach (Guid entityId in entityIds)
        {
            // ルート ModelEntity は削除対象外とする
            if (entityId == RootModelEntity.RootEntityId)
            {
                continue;
            }

            ModelEntity modelEntity = GetEntity(entityId);
            if (modelEntity?.Node != null && IsInstanceValid(modelEntity.Node))
            {
                modelEntity.Node.QueueFree();
            }

            DisposeEntity(entityId);
        }

        // Entity 破棄時に未リンクへ戻った Property も含め、再読み込み前に全件破棄する。
        foreach (Guid propertyId in new List<Guid>(_properties.Keys))
        {
            DisposeProperty(propertyId);
        }

        _rootEntity.Clear();
        Application.Model.Event.NotifyRegistryCleared();
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 登録済みの親 ModelEntity にリンクする。親が未登録の場合は、親が登録されるまで待機する。
    /// </summary>
    /// <param name="modelEntity">リンクする ModelEntity</param>
    /// <returns>親へのリンクに成功した場合は true</returns>
    private bool LinkEntityToParent(ModelEntity modelEntity)
    {
        if (modelEntity.ParentId == Guid.Empty)
        {
            return true;
        }

        if (_entities.TryGetValue(modelEntity.ParentId, out ModelEntity parent))
        {
            parent.AttachEntity(modelEntity);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 登録済みの親 ModelProperty または親 ModelEntity にリンクする。
    /// 親が未登録の場合は、親が登録されるまで待機する。
    /// </summary>
    /// <param name="property">リンク対象の ModelProperty</param>
    /// <returns>親へのリンクに成功した場合は true</returns>
    private bool LinkPropertyToParent(ModelProperty property)
    {
        if (property.ParentId == Guid.Empty)
        {
            return true;
        }

        // 1. 親が ModelProperty の場合
        if (_properties.TryGetValue(property.ParentId, out ModelProperty parentProperty))
        {
            parentProperty.Attach(property);
            return true;
        }

        // 2. 親が ModelEntity の場合（ルートプロパティ）
        if (_entities.TryGetValue(property.ParentId, out ModelEntity parentModel))
        {
            parentModel.AttachProperty(property);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 指定した Entity の子孫を深さ優先順で収集する。
    /// </summary>
    private static void CollectDescendantEntities(
        ModelEntity entity,
        ICollection<ModelEntity> descendants,
        ISet<Guid> visitedEntityIds)
    {
        foreach (ModelEntity childEntity in entity.Children)
        {
            if (!visitedEntityIds.Add(childEntity.Id))
            {
                continue;
            }

            descendants.Add(childEntity);
            CollectDescendantEntities(childEntity, descendants, visitedEntityIds);
        }
    }

    #endregion
}
