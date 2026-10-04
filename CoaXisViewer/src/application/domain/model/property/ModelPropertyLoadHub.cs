// TODO: リファクタリング確認後に削除
using CoaXis.Protocol.Viewer;
using System;
using System.Collections.Generic;

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
    /// 指定したDTOからモデル属性を生成し、Property Registryへ登録して階層を解決する。
    /// </summary>
    /// <param name="propertyDtos">読み込むモデル属性DTOの集合</param>
    /// <returns>登録されたModelPropertyの一覧</returns>
    /// <exception cref="ArgumentNullException">propertyDtosがnullの場合</exception>
    public IReadOnlyList<ModelProperty> LoadProperties(IReadOnlyList<ModelPropertyDto> propertyDtos)
    {
        if (propertyDtos == null)
        {
            throw new ArgumentNullException(nameof(propertyDtos));
        }

        IReadOnlyList<ModelProperty> properties = ModelPropertyFactory.Create(propertyDtos);
        // 全件を登録してから階層を解決し、入力順に依存せず親子関係を確定する。
        foreach (ModelProperty property in properties)
        {
            Application.Model.Property.Registry.RegisterProperty(property);
        }

        Application.Model.Property.Registry.ResolveHierarchy();
        return properties;
    }

    #endregion

    #region Helpers

    #endregion
}
