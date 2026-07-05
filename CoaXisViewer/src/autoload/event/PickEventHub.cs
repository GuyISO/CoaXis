using Godot;

/// <summary>
/// ピック関連のイベント集約ハブ
/// Autoloadに登録してシングルトン参照する
/// </summary>
public partial class PickEventHub : EventHubBase<PickEventHub>
{
    #region --------------------------------------- Request ---------------------------------------

    [Signal] public delegate void NotifyPickHandlingModeRequestedEventHandler(PickHandlingMode mode);
    /// <summary>
    /// 選択操作モードの通知をリクエストする
    /// </summary>
    public static void RequestNotifyPickHandlingMode()
    {
        TryEmitSignal(SignalName.NotifyPickHandlingModeRequested);
    }

    #endregion

    #region --------------------------------------- Notification ---------------------------------------

    [Signal] public delegate void PickHandlingModeNotifiedEventHandler(PickHandlingMode mode);
    /// <summary>
    /// 選択操作モードの通知を行う
    /// </summary>
    /// <param name="mode">通知する選択操作モード</param>
    public static void NotifyPickHandlingMode(PickHandlingMode mode)
    {
        TryEmitSignal(SignalName.PickHandlingModeNotified, (int)mode);
    }

    [Signal] public delegate void PickResultsNotifiedEventHandler(PickResult[] pickResults);
    /// <summary>
    /// ピック結果の通知を行う
    /// </summary>
    /// <param name="pickResults">ピック結果の配列</param>
    public static void NotifyPickResults(PickResult[] pickResults)
    {
        TryEmitSignal(SignalName.PickResultsNotified, pickResults);
    }

    #endregion
}