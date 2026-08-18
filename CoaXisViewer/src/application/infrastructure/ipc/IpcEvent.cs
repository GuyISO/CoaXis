using Godot;

/// <summary>
/// IPC 関連のイベント集約ハブ
/// </summary>
public partial class IpcEvent : EventBase<IpcEvent>
{
    #region --------------------------------------- Action ---------------------------------------



    #endregion

    #region --------------------------------------- Notification ---------------------------------------

    [Signal] public delegate void MessageReceivedEventHandler(string eventType);
    /// <summary>
    /// IPC メッセージを受信したことを通知する
    /// </summary>
    /// <param name="eventType">受信したメッセージの eventType</param>
    internal void NotifyMessageReceived(string eventType)
    {
        Emit(SignalName.MessageReceived, eventType);
    }

    [Signal] public delegate void MessageHandledEventHandler(string eventType, bool ok, string errorCode);
    /// <summary>
    /// IPC メッセージの処理結果を通知する
    /// </summary>
    /// <param name="eventType">処理したメッセージの eventType</param>
    /// <param name="ok">処理が成功した場合は true</param>
    /// <param name="errorCode">失敗時の標準化エラーコード</param>
    internal void NotifyMessageHandled(string eventType, bool ok, string errorCode)
    {
        Emit(SignalName.MessageHandled, eventType, ok, errorCode);
    }

    #endregion
}
