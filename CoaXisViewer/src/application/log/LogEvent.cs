using Godot;

/// <summary>
/// ログ機能のイベント集約ハブ
/// </summary>
public partial class LogEvent : EventBase<LogEvent>
{
    #region --------------------------------------- Action ---------------------------------------

    // ログはイベントによる要求はせず、直接 LogService に出力するため、Action は定義しない

    #endregion

    #region --------------------------------------- Notification ---------------------------------------

    [Signal] public delegate void LogNotifiedEventHandler(string message);
    /// <summary>
    /// ログ出力を通知する
    /// </summary>
    /// <param name="message">通知するログメッセージ</param>
    internal void NotifyLog(string message)
    {
        // Emitを使用するとSignal記録と同時にログが出力され無限ループするため、直接 EmitSignal を使用する
        EmitSignal(SignalName.LogNotified, message);
    }

    #endregion
}