using Godot;

/// <summary>
/// 選択関連のイベント集約ハブ
/// </summary>
public partial class SelectionEvent : EventBase<SelectionEvent>
{
    #region --------------------------------------- Action ---------------------------------------

    [Signal] public delegate void SetModeRequestedEventHandler(SelectionMode mode);
    /// <summary>
    /// 選択モードの設定をリクエストする
    /// </summary>
    /// <param name="mode">設定する選択モード</param>
    internal void SetMode(SelectionMode mode)
    {
        Emit(SignalName.SetModeRequested, (int)mode);
    }

    [Signal] public delegate void ClearRequestedEventHandler();
    /// <summary>
    /// 選択のクリアをリクエストする
    /// </summary>
    internal void Clear()
    {
        Emit(SignalName.ClearRequested);
    }

    #endregion

    #region --------------------------------------- Notification ---------------------------------------

    [Signal] public delegate void ModeNotifiedEventHandler(SelectionMode mode);
    /// <summary>
    /// 選択モードの通知を行う
    /// </summary>
    /// <param name="mode">通知する選択モード</param>
    internal void NotifyMode(SelectionMode mode)
    {
        Emit(SignalName.ModeNotified, (int)mode);
    }

    [Signal] public delegate void ModelStateNotifiedEventHandler(AnyModel model, bool isSelected);
    /// <summary>
    /// モデルの選択状態の通知を行う
    /// </summary>
    /// <param name="model">選択状態が変化したモデル</param>
    /// <param name="isSelected">モデルが選択されている場合はtrue、選択されていない場合はfalse</param>
    internal void NotifyModelState(AnyModel model, bool isSelected)
    {
        Emit(SignalName.ModelStateNotified, model, isSelected);
    }

    [Signal] public delegate void ClearedNotifiedEventHandler();
    /// <summary>
    /// 選択がクリアされたことを通知する
    /// </summary>
    internal void NotifyCleared()
    {
        Emit(SignalName.ClearedNotified);
    }

    #endregion
}
