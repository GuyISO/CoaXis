using Godot;
using System;

/// <summary>
/// ModelEntityのプロパティ表示要求をUIコンポーネントへ通知する。
/// </summary>
public partial class UiPropertyHub : BaseHub
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
    /// 指定したModelEntityのProperty OverlayTree表示を要求する。状態の正本は ModelEntity.IsPropertyTreeVisible が持つ。
    /// </summary>
    /// <param name="entityId">表示対象のModelEntity識別子。</param>
    public void ShowModelEntityProperties(Guid entityId)
    {
        SetPropertyTreeVisible(entityId, true);
    }

    /// <summary>
    /// 指定したModelEntityのProperty OverlayTree非表示を要求する。
    /// </summary>
    /// <param name="entityId">非表示対象のModelEntity識別子。</param>
    public void HideModelEntityProperties(Guid entityId)
    {
        SetPropertyTreeVisible(entityId, false);
    }

    /// <summary>
    /// 指定したModelEntityのProperty OverlayTree表示を反転する。
    /// </summary>
    /// <param name="entityId">対象のModelEntity識別子。</param>
    public void ToggleModelEntityProperties(Guid entityId)
    {
        ModelEntity entity = Application.Model.Entity.Registry.Get(entityId);
        if (entity == null)
        {
            Application.Log.Warn($"UiPropertyHub: property toggle target not found. entityId='{entityId}'");
            return;
        }

        entity.IsPropertyTreeVisible = !entity.IsPropertyTreeVisible;
    }

    #endregion

    #region Helpers

    /// <summary>
    /// 対象ModelEntityのOverlayTree表示状態を設定する。通知は ModelEntity が行う。
    /// </summary>
    /// <param name="entityId">対象のModelEntity識別子。</param>
    /// <param name="isVisible">表示する場合は true。</param>
    private static void SetPropertyTreeVisible(Guid entityId, bool isVisible)
    {
        ModelEntity entity = entityId == Guid.Empty ? null : Application.Model.Entity.Registry.Get(entityId);
        if (entity == null)
        {
            Application.Log.Warn($"UiPropertyHub: property tree request ignored because target was not found. entityId='{entityId}'");
            return;
        }

        entity.IsPropertyTreeVisible = isVisible;
    }

    #endregion
}
