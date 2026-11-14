using System;
using System.Collections.Generic;
using System.Linq;
using CoaXis.Protocol.Viewer;

/// <summary>
/// Entityと所属Propertyの条件からEntityを検索するハブ。
/// </summary>
public partial class ModelSearchHub : BaseHub
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

    /// <summary>登録済みEntityの種類一覧を取得する。</summary>
    public IReadOnlyList<string> GetEntityTypes()
    {
        return Application.Model.Entity.Registry.Items.Values
            .Where(entity => entity.Id != ViewerConstants.RootModelEntityId)
            .Select(entity => entity.Type)
            .Where(type => !string.IsNullOrEmpty(type))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(type => type, StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>登録済みPropertyの種類一覧を取得する。</summary>
    public IReadOnlyList<string> GetPropertyTypes()
    {
        return Application.Model.Property.Registry.Items.Values
            .Select(property => property.PropertyType)
            .Where(type => !string.IsNullOrEmpty(type))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(type => type, StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>登録済みPropertyの値の型一覧を取得する。</summary>
    public IReadOnlyList<string> GetPropertyValueTypes()
    {
        return Application.Model.Property.Registry.Items.Values
            .Select(property => property.ValueType)
            .Where(type => !string.IsNullOrEmpty(type))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(type => type, StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>指定されたすべての条件に一致するEntityの識別子を取得する。</summary>
    /// <param name="entityType">Entityの種類。空の場合は指定なし</param>
    /// <param name="entityName">Entity名の部分文字列。空の場合は指定なし</param>
    /// <param name="propertyType">Propertyの種類。空の場合は指定なし</param>
    /// <param name="propertyValueType">Propertyの値の型。空の場合は指定なし</param>
    /// <param name="propertyValue">Property値の部分文字列。空の場合は指定なし</param>
    /// <returns>一致するEntityの識別子</returns>
    public IReadOnlyList<Guid> Find(string entityType, string entityName, string propertyType, string propertyValueType, string propertyValue)
    {
        bool hasPropertyFilter = !string.IsNullOrEmpty(propertyType)
            || !string.IsNullOrEmpty(propertyValueType)
            || !string.IsNullOrEmpty(propertyValue);
        var matchingEntityIds = new HashSet<Guid>();
        if (hasPropertyFilter)
        {
            foreach (ModelProperty property in Application.Model.Property.Registry.Items.Values)
            {
                if (!MatchesType(property.PropertyType, propertyType)
                    || !MatchesType(property.ValueType, propertyValueType)
                    || !Contains(property.Value, propertyValue))
                {
                    continue;
                }

                ModelEntity owner = Application.Model.Property.Registry.GetOwningEntity(property.Id);
                if (owner != null)
                {
                    matchingEntityIds.Add(owner.Id);
                }
            }
        }

        return Application.Model.Entity.Registry.Items.Values
            .Where(entity => entity.Id != ViewerConstants.RootModelEntityId
                && MatchesType(entity.Type, entityType)
                && Contains(entity.Name, entityName)
                && (!hasPropertyFilter || matchingEntityIds.Contains(entity.Id)))
            .OrderBy(entity => entity.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(entity => entity.Type, StringComparer.Ordinal)
            .ThenBy(entity => entity.Id)
            .Select(entity => entity.Id)
            .ToArray();
    }

    #endregion

    #region Helpers

    /// <summary>種類が未指定または完全一致かを判定する。</summary>
    private static bool MatchesType(string value, string filter)
    {
        return string.IsNullOrEmpty(filter) || string.Equals(value, filter, StringComparison.Ordinal);
    }

    /// <summary>文字列が未指定または大文字小文字を区別せず部分一致するかを判定する。</summary>
    private static bool Contains(string value, string filter)
    {
        return string.IsNullOrEmpty(filter)
            || (value != null && value.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    #endregion
}
