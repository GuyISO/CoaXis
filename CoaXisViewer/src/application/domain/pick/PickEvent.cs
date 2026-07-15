using Godot;

/// <summary>
/// ピック関連のイベント集約ハブ
/// </summary>
public partial class PickEvent : EventBase<PickEvent>
{
    #region --------------------------------------- Action ---------------------------------------

    [Signal] public delegate void AskPickHandlingModeRequestedEventHandler();
    /// <summary>
    /// 選択操作モードの通知をリクエストする
    /// </summary>
    internal void AskPickHandlingMode()
    {
        Emit(SignalName.AskPickHandlingModeRequested);
    }

    #endregion

    #region --------------------------------------- Notification ---------------------------------------

    [Signal] public delegate void PickHandlingModeNotifiedEventHandler(PickHandlingMode mode);
    /// <summary>
    /// 選択操作モードの通知を行う
    /// </summary>
    /// <param name="mode">通知する選択操作モード</param>
    internal void NotifyPickHandlingMode(PickHandlingMode mode)
    {
        Emit(SignalName.PickHandlingModeNotified, (int)mode);
    }

    [Signal] public delegate void PickResultNotifiedEventHandler(PickResult pickResult);
    /// <summary>
    /// ピック結果の通知を行う
    /// </summary>
    /// <param name="pickResult">ピック結果</param>
    internal void NotifyPickResult(PickResult pickResult)
    {
        Emit(SignalName.PickResultNotified, pickResult);
    }

    [Signal] public delegate void PickResultsNotifiedEventHandler(PickResult[] pickResults);
    /// <summary>
    /// 複数一括ピック結果の通知を行う
    /// </summary>
    /// <param name="pickResults">ピック結果の配列</param>
    /// <remarks>複数のピック結果を一括で通知する場合はレイキャストによる取得ではないので座標値などを持たない</remarks>
    internal void NotifyPickResults(PickResult[] pickResults)
    {
        Emit(SignalName.PickResultsNotified, pickResults);
    }

    #endregion
}