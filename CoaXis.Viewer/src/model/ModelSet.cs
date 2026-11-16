using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

/// <summary>
/// 検証済みのModelEntityとModelPropertyの集合。
/// </summary>
public sealed class ModelSet
{
    #region Properties

    /// <summary>検証済みのモデル実体一覧を取得する。</summary>
    public ReadOnlyCollection<ModelEntity> Entities { get; }

    /// <summary>検証済みのモデル属性一覧を取得する。</summary>
    public ReadOnlyCollection<ModelProperty> Properties { get; }

    #endregion

    #region Lifecycle

    /// <summary>
    /// モデル実体と属性を読み取り専用の集合として保持する。
    /// </summary>
    /// <param name="entities">検証済みモデル実体</param>
    /// <param name="properties">検証済みモデル属性</param>
    /// <exception cref="ArgumentNullException">いずれかの集合がnullの場合</exception>
    internal ModelSet(
        IReadOnlyList<ModelEntity> entities,
        IReadOnlyList<ModelProperty> properties)
    {
        if (entities == null)
        {
            throw new ArgumentNullException(nameof(entities));
        }

        if (properties == null)
        {
            throw new ArgumentNullException(nameof(properties));
        }

        Entities = Array.AsReadOnly(entities.ToArray());
        Properties = Array.AsReadOnly(properties.ToArray());
    }

    #endregion
}
