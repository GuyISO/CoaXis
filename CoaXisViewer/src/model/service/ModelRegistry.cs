using System;
using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// モデル実体（ModelEntity）と属性（ModelProperty）のインスタンスおよびUUIDを一元管理する Autoload ノード
/// </summary>
public partial class ModelRegistry : Node
{
    #region Fields

    // Guid をキーにして ModelEntity を管理する辞書
    private readonly Dictionary<Guid, ModelEntity> _entities = new();

    // 親へまだリンクできていない ModelEntity の Id 集合。ResolveHierarchy の走査対象をこれだけに絞ることで、登録済み全件の再走査(O(n²))を避ける
    private readonly HashSet<Guid> _unlinkedIds = new();

    // Guid をキーにして ModelProperty を管理する辞書
    private readonly Dictionary<Guid, ModelProperty> _properties = new();

    // 親へまだリンクできていない ModelProperty の Id 集合
    private readonly HashSet<Guid> _unlinkedPropertyIds = new();

    #endregion

    #region Properties

    // 登録されている ModelEntity の集合を取得する
    public IReadOnlyDictionary<Guid, ModelEntity> Entities => _entities;

    // 登録されている ModelProperty の集合を取得する
    public IReadOnlyDictionary<Guid, ModelProperty> Properties => _properties;

    #endregion

    #region Public Methods

    /// <summary>
    /// ModelEntity を取得する。存在しない場合は null を返す。
    /// </summary>
    /// <param name="entityId">取得対象の ModelEntity の Id</param>
    /// <returns>該当する ModelEntity。存在しない場合は null</returns>
    public ModelEntity GetEntity(Guid entityId)
    {
        return _entities.TryGetValue(entityId, out ModelEntity modelEntity) ? modelEntity : null;
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
    /// ModelEntity を登録する。親が未登録の場合は、親が登録されるまで待機する。
    /// すでに同じ Id の ModelEntity が登録されている場合は、既存の ModelEntity を置き換える。
    /// </summary>
    /// <param name="modelEntity">登録対象の ModelEntity</param>
    /// <exception cref="ArgumentNullException">modelEntity が null の場合</exception>
    /// <exception cref="ArgumentException">modelEntity.Id が空または Initialized 状態でない場合</exception>
    public void RegisterEntity(ModelEntity modelEntity)
    {
        // レジストリは「新規に生成された未登録モデル」だけを受け入れる
        // 既存データの再利用や終了済みデータを混ぜると、再読込時に状態が壊れるため、ここで明確に弾く
        if (modelEntity == null)
        {
            throw new ArgumentNullException(nameof(modelEntity));
        }

        if (modelEntity.Id == Guid.Empty)
        {
            throw new ArgumentException("ModelEntity id must not be empty.", nameof(modelEntity));
        }

        if (modelEntity.Status != ModelStatus.Initialized)
        {
            throw new ArgumentException("ModelEntity must be in Initialized status.", nameof(modelEntity));
        }

        if (IsEntityRegistered(modelEntity.Id))
        {
            // すでに登録されている場合は、既存の ModelEntity を置き換える
            DisposeEntity(modelEntity.Id);
        }

        // レジストリに登録する
        _entities.Add(modelEntity.Id, modelEntity);
        modelEntity.Status = ModelStatus.Registered;
        _unlinkedIds.Add(modelEntity.Id);
    }

    /// <summary>
    /// ModelEntity を破棄し、レジストリおよび親子関係から解除する。
    /// </summary>
    /// <param name="entityId">破棄対象の ModelEntity の Id</param>
    /// <returns>削除に成功した場合は true</returns>
    public bool DisposeEntity(Guid entityId)
    {
        // 削除時は子孫から順に解除し、親参照も切ってから辞書から外す
        // これを行わないと、親子関係やツリーの参照が残って、再読込後に古いノードが見える
        if (!_entities.TryGetValue(entityId, out ModelEntity modelEntity))
        {
            return false;
        }

        // 子孫の ModelEntity から先に削除する
        foreach (ModelEntity child in modelEntity.Children.ToList())
        {
            DisposeEntity(child.Id);
        }

        // 紐付いているルートプロパティも連動して削除する
        foreach (ModelProperty property in modelEntity.Properties.ToList())
        {
            DisposeProperty(property.Id);
        }

        // 親からの参照に登録済みの場合は解除する
        ModelEntity parent = GetEntity(modelEntity.ParentId);
        parent?.Detach(modelEntity);

        // レジストリから削除する
        _entities.Remove(entityId);
        _unlinkedIds.Remove(entityId);
        modelEntity.Status = ModelStatus.Disposed;

        return true;
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
    /// ModelProperty が登録されているかどうかを判定する。
    /// </summary>
    /// <param name="propertyId">判定対象の ModelProperty の Id</param>
    /// <returns>登録済みの場合は true</returns>
    public bool IsPropertyRegistered(Guid propertyId)
    {
        return _properties.ContainsKey(propertyId);
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

        // レジストリに登録し、親リンク解決キューに追加する
        _properties.Add(property.Id, property);
        _unlinkedPropertyIds.Add(property.Id);
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
            DisposeProperty(child.Id);
        }

        // 親プロパティまたは親モデル実体からの参照を解除する
        if (property.ParentId != Guid.Empty)
        {
            if (_properties.TryGetValue(property.ParentId, out ModelProperty parentProp))
            {
                parentProp.Detach(property);
            }
            else if (_entities.TryGetValue(property.ParentId, out ModelEntity parentModel))
            {
                parentModel.DetachProperty(property);
            }
        }

        // レジストリから削除する
        _properties.Remove(propertyId);
        _unlinkedPropertyIds.Remove(propertyId);

        return true;
    }

    /// <summary>
    /// 未解決の ModelEntity の親子関係を解決する。
    /// </summary>
    public void ResolveEntityHierarchy()
    {
        // 未リンクIdだけを対象にする。登録済み全件を毎回舐めると件数の二乗オーダーになるため避ける
        if (_unlinkedIds.Count == 0)
        {
            return;
        }

        foreach (Guid id in _unlinkedIds.ToList())
        {
            if (_entities.TryGetValue(id, out ModelEntity modelEntity) && LinkToRegisteredParent(modelEntity))
            {
                _unlinkedIds.Remove(id);
            }
        }
    }

    /// <summary>
    /// 未解決の ModelProperty の親子関係を解決する。
    /// </summary>
    public void ResolvePropertyHierarchy()
    {
        if (_unlinkedPropertyIds.Count == 0)
        {
            return;
        }

        foreach (Guid id in _unlinkedPropertyIds.ToList())
        {
            if (_properties.TryGetValue(id, out ModelProperty property) && LinkToRegisteredPropertyParent(property))
            {
                _unlinkedPropertyIds.Remove(id);
            }
        }
    }
    
    #endregion

    #region Private Methods

    /// <summary>
    /// 登録済みの親 ModelEntity にリンクする。親が未登録の場合は、親が登録されるまで待機する。
    /// </summary>
    /// <param name="modelEntity">リンクする ModelEntity</param>
    /// <returns>親へのリンクに成功した場合は true</returns>
    private bool LinkToRegisteredParent(ModelEntity modelEntity)
    {
        if (modelEntity.ParentId == Guid.Empty)
        {
            return true;
        }

        if (_entities.TryGetValue(modelEntity.ParentId, out ModelEntity parent))
        {
            parent.Attach(modelEntity);
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
    private bool LinkToRegisteredPropertyParent(ModelProperty property)
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

    #endregion
}
