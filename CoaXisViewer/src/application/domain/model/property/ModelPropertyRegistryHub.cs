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
    private readonly Dictionary<Guid, ModelProperty> _properties = new();

    #endregion

    #region Properties

    /// <summary>
    /// 登録されている ModelProperty の集合を取得する。
    /// </summary>
    public IReadOnlyDictionary<Guid, ModelProperty> Properties => _properties;

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
    public ModelProperty GetProperty(Guid propertyId)
    {
        return _properties.TryGetValue(propertyId, out ModelProperty property) ? property : null;
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
            ModelEntity parentEntity = Application.Model.Entity.Registry.GetEntity(property.ParentId);
            if (parentEntity != null)
            {
                return parentEntity;
            }

            property = GetProperty(property.ParentId);
        }

        return null;
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
            DisposeProperty(property.Id);
        }

        _properties.Add(property.Id, property);
        LinkPropertyToParent(property);
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

        foreach (ModelProperty child in property.Children.ToList())
        {
            property.Detach(child);
        }

        if (property.ParentId != Guid.Empty)
        {
            ModelProperty parentProperty = GetProperty(property.ParentId);
            if (parentProperty != null)
            {
                parentProperty.Detach(property);
            }
            else
            {
                Application.Model.Entity.Registry.GetEntity(property.ParentId)?.DetachProperty(property);
            }
        }

        _properties.Remove(propertyId);
        return true;
    }

    /// <summary>
    /// 未解決の ModelProperty の親子関係を解決する。
    /// </summary>
    public void ResolveHierarchy()
    {
        // Entity再登録後にも親参照を復元できるよう、全Propertyの親を再確認する。
        foreach (ModelProperty property in _properties.Values.ToList())
        {
            LinkPropertyToParent(property);
        }
    }

    /// <summary>
    /// 登録済みの ModelProperty をすべてクリアする。
    /// </summary>
    public void Clear()
    {
        foreach (Guid propertyId in new List<Guid>(_properties.Keys))
        {
            DisposeProperty(propertyId);
        }
    }

    #endregion

    #region Helpers

    /// <summary>Propertyを親Propertyの子として接続する。</summary>
    private bool LinkPropertyToParent(ModelProperty property)
    {
        if (property.ParentId == Guid.Empty)
        {
            return true;
        }

        ModelProperty parentProperty = GetProperty(property.ParentId);
        if (parentProperty != null)
        {
            parentProperty.Attach(property);
            return true;
        }

        ModelEntity parentEntity = Application.Model.Entity.Registry.GetEntity(property.ParentId);
        if (parentEntity != null)
        {
            parentEntity.AttachProperty(property);
            return true;
        }

        return false;
    }

    #endregion
}
