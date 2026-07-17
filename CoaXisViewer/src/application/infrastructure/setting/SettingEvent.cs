using Godot;

/// <summary>
/// 設定関連のイベント集約ハブ
/// </summary>
public partial class SettingEvent : EventBase<SettingEvent>
{
    #region --------------------------------------- Action ---------------------------------------

    [Signal] public delegate void AskValueRequestedEventHandler();
    /// <summary>
    /// 設定値の通知をリクエストする
    /// </summary>
    internal void AskValue()
    {
        Emit(SignalName.AskValueRequested);
    }

    #endregion

    #region --------------------------------------- Notification ---------------------------------------

    [Signal] public delegate void ValueNotifiedEventHandler(string value);
    /// <summary>
    /// 設定値の通知を行う
    /// </summary>
    /// <param name="value">通知する値</param>
    internal void NotifyValue(string value)
    {
        Emit(SignalName.ValueNotified, value);
    }

    #endregion
}