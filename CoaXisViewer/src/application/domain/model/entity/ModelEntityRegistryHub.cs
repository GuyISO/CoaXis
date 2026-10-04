using System;
using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// ModelEntity の識別子、階層、およびルート実体を管理するハブ。
/// </summary>
public partial class ModelEntityRegistryHub : BaseHub
{
    #region Fields

    /// <summary>登録済みEntityの識別子辞書。</summary>
    private readonly Dictionary<Guid, ModelEntity> _entities = new();

    // 未解決Entityの登録順を保持し、親が後から登録された場合も兄弟順を安定させる。
    /// <summary>親へ未接続のEntity識別子。</summary>
    private readonly List<Guid> _unlinkedIds = new();

    #endregion

    #region Properties

    /// <summary>登録されている ModelEntity の集合を取得する。</summary>
    public IReadOnlyDictionary<Guid, ModelEntity> Entities => _entities;

    /// <summary>シーン全体のルート ModelEntity を取得する。</summary>
    public RootModelEntity RootEntity { get; private set; } = null!;

    #endregion

    #region Signals

    /// <summary>ModelEntity の追加通知。値は entityId から Registry を参照する。</summary>
    /// <param name="entityId">追加された部分木ルートの識別子</param>
    [Signal]
    public delegate void AddedEventHandler(string entityId);

    /// <summary>
    /// 登録済み部分木の追加を通知する。
    /// </summary>
    /// <param name="entityId">追加する部分木ルート ModelEntity の識別子</param>
    internal void NotifyAdded(Guid entityId)
    {
        EmitSignal(SignalName.Added, entityId.ToString());
    }

    /// <summary>モデル集合の置換通知。</summary>
    [Signal]
    public delegate void ModelSetReplacedEventHandler();

    /// <summary>
    /// Registryのモデル集合が置換されたことを通知する。
    /// </summary>
    internal void NotifyModelSetReplaced()
    {
        EmitSignal(SignalName.ModelSetReplaced);
    }

    /// <summary>モデル集合のクリア通知。</summary>
    [Signal]
    public delegate void ClearedEventHandler();

    /// <summary>
    /// モデル集合全体がクリアされたことを通知する。
    /// </summary>
    internal void NotifyCleared()
    {
        EmitSignal(SignalName.Cleared);
    }

    #endregion

    #region Lifecycle

    /// <summary>
    /// レジストリのルート ModelEntity を初期化する。
    /// </summary>
    public override void _Ready()
    {
        RootEntity = new RootModelEntity();
        AddChild(RootEntity.Node);
    }

    #endregion

    #region Events

    #endregion

    #region Methods

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
        var visitedEntityIds = new HashSet<Guid> { entityId };
        ModelEntity currentEntity = GetEntity(entityId);

        while (currentEntity != null)
        {
            ModelEntity parentEntity = GetParentEntity(currentEntity.Id);
            if (parentEntity == null || !visitedEntityIds.Add(parentEntity.Id))
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
    /// ModelEntity が登録されているかどうかを判定する。
    /// </summary>
    /// <param name="entityId">判定対象の ModelEntity の Id</param>
    /// <returns>登録済みの場合は true</returns>
    public bool IsEntityRegistered(Guid entityId)
    {
        return _entities.ContainsKey(entityId);
    }

    /// <summary>
    /// ModelEntity を登録する。親が未登録の場合は、親が登録されるまでリンク待機する。
    /// すでに同じ Id の ModelEntity が登録されている場合は、既存の ModelEntity を置き換える。
    /// </summary>
    /// <param name="entity">登録対象の ModelEntity</param>
    /// <exception cref="ArgumentNullException">entity が null の場合</exception>
    /// <exception cref="ArgumentException">entity.Id が空または Status が Initialized でない場合</exception>
    public void RegisterEntity(ModelEntity entity)
    {
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
            DisposeEntity(entity.Id);
        }

        _entities.Add(entity.Id, entity);
        entity.Status = ModelStatus.Registered;
        if (!LinkEntityToParent(entity))
        {
            _unlinkedIds.Add(entity.Id);
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

        foreach (ModelEntity child in entity.Children.ToList())
        {
            entity.DetachEntity(child);
            if (!_unlinkedIds.Contains(child.Id))
            {
                _unlinkedIds.Add(child.Id);
            }
        }

        foreach (ModelProperty property in entity.Properties.ToList())
        {
            entity.DetachProperty(property);
        }

        ModelEntity parent = GetEntity(entity.ParentId);
        parent?.DetachEntity(entity);

        _entities.Remove(entityId);
        _unlinkedIds.Remove(entityId);
        entity.Status = ModelStatus.Disposed;
        return true;
    }

    /// <summary>
    /// 未解決の ModelEntity の親子関係を解決する。
    /// </summary>
    public void ResolveHierarchy()
    {
        foreach (Guid id in _unlinkedIds.ToList())
        {
            if (_entities.TryGetValue(id, out ModelEntity entity) && LinkEntityToParent(entity))
            {
                _unlinkedIds.Remove(id);
            }
        }
    }

    /// <summary>
    /// 登録済みのモデル実体をルートを残してクリアする。
    /// </summary>
    public void Clear()
    {
        foreach (Guid entityId in new List<Guid>(_entities.Keys))
        {
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

        RootEntity.Clear();
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Entityを親Entityの子として接続する。
    /// </summary>
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
    /// 指定Entity配下の子孫Entityを収集する。
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
