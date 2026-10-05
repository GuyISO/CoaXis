using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// ModelProperty の識別子と階層を管理するハブ。
/// </summary>
public partial class ModelPropertyRegistryHub : BaseHub
{
    #region Fields

    /// <summary>登録済みPropertyの識別子辞書。</summary>
    private readonly Dictionary<Guid, ModelProperty> _items = new();

    #endregion

    #region Properties

    /// <summary>登録されている ModelProperty の集合を取得する。</summary>
    public IReadOnlyDictionary<Guid, ModelProperty> Items => _items;

    #endregion

    #region Signals

    #endregion

    #region Lifecycle

    #endregion

    #region Events

    #endregion

    #region Methods

    /// <summary>
    /// ModelProperty を取得する。存在しない場合は null を返す。
    /// </summary>
    /// <param name="propertyId">取得対象の ModelProperty の Id</param>
    /// <returns>該当する ModelProperty。存在しない場合は null</returns>
    public ModelProperty Get(Guid propertyId)
    {
        return _items.TryGetValue(propertyId, out ModelProperty property) ? property : null;
    }

    /// <summary>
    /// 指定した ModelProperty が所属する ModelEntity を取得する。
    /// </summary>
    /// <param name="propertyId">所属 Entity を取得する ModelProperty の識別子</param>
    /// <returns>所属する ModelEntity。未登録または未解決の場合は null</returns>
    public ModelEntity GetOwningEntity(Guid propertyId)
    {
        ModelProperty property = Get(propertyId);
        var visitedPropertyIds = new HashSet<Guid>();

        while (property != null && visitedPropertyIds.Add(property.Id))
        {
            ModelEntity parentEntity = Application.Model.Entity.Registry.Get(property.ParentId);
            if (parentEntity != null)
            {
                return parentEntity;
            }

            property = Get(property.ParentId);
        }

        return null;
    }

    /// <summary>
    /// ModelProperty が登録されているかどうかを判定する。
    /// </summary>
    /// <param name="propertyId">判定対象の ModelProperty の Id</param>
    /// <returns>登録済みの場合は true</returns>
    public bool IsRegistered(Guid propertyId)
    {
        return _items.ContainsKey(propertyId);
    }

    /// <summary>
    /// ModelProperty を登録する。親が未登録の場合は、親が登録されるまでリンク待機する。
    /// すでに同じ Id の ModelProperty が登録されている場合は、既存の ModelProperty を置き換える。
    /// </summary>
    /// <param name="property">登録対象の ModelProperty</param>
    /// <exception cref="ArgumentNullException">property が null の場合</exception>
    /// <exception cref="ArgumentException">property.Id が空の場合</exception>
    public void Register(ModelProperty property)
    {
        if (property == null)
        {
            throw new ArgumentNullException(nameof(property));
        }

        if (property.Id == Guid.Empty)
        {
            throw new ArgumentException("ModelProperty id must not be empty.", nameof(property));
        }

        if (IsRegistered(property.Id))
        {
            Dispose(property.Id);
        }

        _items.Add(property.Id, property);
        LinkToParent(property);
    }

    /// <summary>
    /// ModelProperty を破棄し、レジストリおよび親子関係から解除する。
    /// </summary>
    /// <param name="propertyId">破棄対象の ModelProperty の Id</param>
    /// <returns>削除に成功した場合は true</returns>
    public bool Dispose(Guid propertyId)
    {
        if (!_items.TryGetValue(propertyId, out ModelProperty property))
        {
            return false;
        }

        foreach (ModelProperty child in property.Children.ToList())
        {
            property.Detach(child);
        }

        if (property.ParentId != Guid.Empty)
        {
            ModelProperty parentProperty = Get(property.ParentId);
            if (parentProperty != null)
            {
                parentProperty.Detach(property);
            }
            else
            {
                Application.Model.Entity.Registry.Get(property.ParentId)?.DetachProperty(property);
            }
        }

        _items.Remove(propertyId);
        return true;
    }

    /// <summary>
    /// 未解決の ModelProperty の親子関係を解決する。
    /// </summary>
    public void ResolveHierarchy()
    {
        // Entity再登録後にも親参照を復元できるよう、全Propertyの親を再確認する。
        foreach (ModelProperty item in _items.Values.ToList())
        {
            LinkToParent(item);
        }
    }

    /// <summary>
    /// 登録済みの ModelProperty をすべてクリアする。
    /// </summary>
    public void Clear()
    {
        foreach (Guid propertyId in new List<Guid>(_items.Keys))
        {
            Dispose(propertyId);
        }
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Propertyを親Propertyの子として接続する。
    /// </summary>
    private bool LinkToParent(ModelProperty property)
    {
        if (property.ParentId == Guid.Empty)
        {
            return true;
        }

        ModelProperty parentProperty = Get(property.ParentId);
        if (parentProperty != null)
        {
            parentProperty.Attach(property);
            return true;
        }

        ModelEntity parentEntity = Application.Model.Entity.Registry.Get(property.ParentId);
        if (parentEntity != null)
        {
            parentEntity.AttachProperty(property);
            return true;
        }

        return false;
    }

    #endregion
}
