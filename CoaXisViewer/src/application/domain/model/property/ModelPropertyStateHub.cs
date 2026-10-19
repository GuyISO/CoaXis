using Godot;
using System;

/// <summary>
/// ModelPropertyが更新した論理状態の変更通知のみを担当するハブ。
/// 通知は対象の propertyId のみを運び、値は Registry 経由で ModelProperty から参照する。
/// </summary>
public partial class ModelPropertyStateHub : BaseHub
{
    #region Fields

    #endregion

    #region Properties

    #endregion

    #region Signals

    /// <summary>Propertyの表示設定の変更通知。値は ModelProperty.Visibility を参照する。</summary>
    /// <param name="propertyId">変更された ModelProperty の識別子</param>
    [Signal] public delegate void VisibilityNotifiedEventHandler(string propertyId);

    #endregion

    #region Lifecycle

    #endregion

    #region Events

    #endregion

    #region Methods

    /// <summary>
    /// 表示設定の変更を通知する。状態の反映はModelProperty自身が行い、変更を周知するためにModelProperty自身から呼び出して使用する。
    /// </summary>
    /// <param name="propertyId">変更された ModelProperty の識別子</param>
    public void NotifyVisibility(Guid propertyId)
    {
        EmitSignal(SignalName.VisibilityNotified, propertyId.ToString());
    }

    #endregion

    #region Helpers

    #endregion
}
