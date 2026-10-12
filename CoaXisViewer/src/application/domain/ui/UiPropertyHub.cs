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

    /// <summary>ModelEntityのプロパティ表示要求通知。</summary>
    /// <param name="entityId">表示対象のModelEntity識別子。</param>
    [Signal] public delegate void ShowRequestedEventHandler(string entityId);

    /// <summary>ModelEntityのプロパティ非表示要求通知。</summary>
    /// <param name="entityId">非表示対象のModelEntity識別子。</param>
    [Signal] public delegate void HideRequestedEventHandler(string entityId);

    #endregion

    #region Lifecycle

    #endregion

    #region Events

    #endregion

    #region Methods

    /// <summary>
    /// 指定したModelEntityのプロパティ表示を要求する。
    /// </summary>
    /// <param name="entityId">表示対象のModelEntity識別子。</param>
    public void ShowModelEntityProperties(Guid entityId)
    {
        if (entityId == Guid.Empty)
        {
            Application.Log.Warn("UiPropertyHub: property display request ignored because entityId is empty.");
            return;
        }

        EmitSignal(SignalName.ShowRequested, entityId.ToString());
    }

    /// <summary>
    /// 指定したModelEntityのプロパティ非表示を要求する。
    /// </summary>
    /// <param name="entityId">非表示対象のModelEntity識別子。</param>
    public void HideModelEntityProperties(Guid entityId)
    {
        if (entityId == Guid.Empty)
        {
            Application.Log.Warn("UiPropertyHub: property hide request ignored because entityId is empty.");
            return;
        }

        EmitSignal(SignalName.HideRequested, entityId.ToString());
    }

    #endregion

    #region Helpers

    #endregion
}
