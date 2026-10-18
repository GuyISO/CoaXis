using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// ModelPropertyの生成と登録を管理するハブ。
/// </summary>
public partial class ModelPropertyLoadHub : BaseHub
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
    /// 検証済みPropertyを追加し、同じ識別子がある場合は置換する。
    /// </summary>
    /// <param name="properties">追加または置換するProperty</param>
    internal void AddOrReplacePrepared(IReadOnlyList<ModelProperty> properties)
    {
        RegisterPrepared(properties);
        Application.Model.Property.Registry.ResolveHierarchy();
    }

    /// <summary>
    /// 指定Propertyとその子孫を削除する。
    /// </summary>
    /// <param name="propertyIds">削除対象のProperty識別子</param>
    /// <returns>削除したProperty識別子</returns>
    internal IReadOnlyCollection<Guid> DeleteCascade(IReadOnlyCollection<Guid> propertyIds)
    {
        if (propertyIds == null)
        {
            throw new ArgumentNullException(nameof(propertyIds));
        }

        var idsToDelete = new HashSet<Guid>(propertyIds);
        bool added;
        do
        {
            added = false;
            foreach (ModelProperty property in Application.Model.Property.Registry.Items.Values)
            {
                if (idsToDelete.Contains(property.ParentId) && idsToDelete.Add(property.Id))
                {
                    added = true;
                }
            }
        }
        while (added);

        foreach (Guid propertyId in idsToDelete.OrderByDescending(GetDepth))
        {
            Application.Model.Property.Registry.Dispose(propertyId);
        }

        Application.Model.Property.Registry.ResolveHierarchy();
        return idsToDelete;
    }

    #endregion

    #region Helpers

    /// <summary>
    /// 指定Propertyの親階層の深さを取得する。
    /// </summary>
    private static int GetDepth(Guid propertyId)
    {
        int depth = 0;
        ModelProperty property = Application.Model.Property.Registry.Get(propertyId);
        var visited = new HashSet<Guid>();
        while (property != null && property.ParentId != Guid.Empty && visited.Add(property.Id))
        {
            depth++;
            property = Application.Model.Property.Registry.Get(property.ParentId);
        }

        return depth;
    }

    /// <summary>
    /// 事前生成済みのPropertyをRegistryへ登録する。
    /// </summary>
    /// <param name="properties">登録する検証済みモデル属性</param>
    private void RegisterPrepared(IReadOnlyList<ModelProperty> properties)
    {
        if (properties == null)
        {
            throw new ArgumentNullException(nameof(properties));
        }

        // 全件を登録してから階層を解決し、入力順に依存せず親子関係を確定する。
        foreach (ModelProperty property in properties)
        {
            if (property == null)
            {
                throw new ArgumentException("ModelProperty must not be null.", nameof(properties));
            }

            Application.Model.Property.Registry.Register(property);
        }
    }

    #endregion
}
