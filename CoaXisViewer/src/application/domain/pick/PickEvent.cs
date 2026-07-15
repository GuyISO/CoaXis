using Godot;

/// <summary>
/// ピック関連のイベント集約ハブ
/// </summary>
public partial class PickEvent : EventBase<PickEvent>
{
    #region --------------------------------------- Action ---------------------------------------

    [Signal] public delegate void AskHandlingModeRequestedEventHandler();
    /// <summary>
    /// 選択操作モードの通知をリクエストする
    /// </summary>
    internal void AskHandlingMode()
    {
        Emit(SignalName.AskHandlingModeRequested);
    }

    [Signal] public delegate void SetHandlingModeRequestedEventHandler(PickHandlingMode mode);
    /// <summary>
    /// 選択操作モードの変更をリクエストする
    /// </summary>
    /// <param name="mode">設定する選択操作モード</param>
    internal void SetHandlingMode(PickHandlingMode mode)
    {
        Emit(SignalName.SetHandlingModeRequested, (int)mode);
    }

    #endregion

    #region --------------------------------------- Notification ---------------------------------------

    [Signal] public delegate void HandlingModeNotifiedEventHandler(PickHandlingMode mode);
    /// <summary>
    /// 選択操作モードの通知を行う
    /// </summary>
    /// <param name="mode">通知する選択操作モード</param>
    internal void NotifyHandlingMode(PickHandlingMode mode)
    {
        Emit(SignalName.HandlingModeNotified, (int)mode);
    }

    [Signal] public delegate void ResultNotifiedEventHandler(PickResult pickResult);
    /// <summary>
    /// ピック結果の通知を行う
    /// </summary>
    /// <param name="pickResult">ピック結果</param>
    internal void NotifyResult(PickResult pickResult)
    {
        Emit(SignalName.ResultNotified, pickResult);
    }

    [Signal] public delegate void ResultsNotifiedEventHandler(PickResult[] pickResults);
    /// <summary>
    /// 複数一括ピック結果の通知を行う
    /// </summary>
    /// <param name="pickResults">ピック結果の配列</param>
    /// <remarks>複数のピック結果を一括で通知する場合はレイキャストによる取得ではないので座標値などを持たない</remarks>
    internal void NotifyResults(PickResult[] pickResults)
    {
        Emit(SignalName.ResultsNotified, pickResults);
    }

    #endregion
}