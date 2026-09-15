using Godot;

/// <summary>
/// 設定関連のイベント集約ハブ
/// </summary>
public partial class SettingEvent : EventBase<SettingEvent>
{
    #region --------------------------------------- Action ---------------------------------------



    #endregion

    #region --------------------------------------- Notification ---------------------------------------

    [Signal] public delegate void SettingsNotifiedEventHandler();
    /// <summary>
    /// 設定の再読み込みが完了したことを通知する
    /// </summary>
    internal void NotifySettingsNotified()
    {
        Emit(SignalName.SettingsNotified);
    }

    #endregion
}