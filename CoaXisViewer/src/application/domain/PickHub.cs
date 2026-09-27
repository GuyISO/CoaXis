using Godot;

/// <summary>
/// ピック関連の状態管理を担当する
/// </summary>
public partial class PickHub : Node
{
    #region Fields

    // 選択操作モードの現在値を保持するフィールド、初期値は選択操作とする
    private PickHandlingMode _handlingMode = PickHandlingMode.Selection;

    #endregion

    #region Properties

    /// <summary>
    /// 現在の選択操作モードを取得する
    /// </summary>
    internal PickHandlingMode HandlingMode => _handlingMode;

    #endregion

    #region Lifecycle

    public override void _Ready()
    {
        SubscribeApplicationEvents();
    }

    public override void _ExitTree()
    {
        UnsubscribeApplicationEvents();

        base._ExitTree();
    }

    #endregion

    #region --------------------------------------- Action ---------------------------------------

    [Signal] public delegate void AskHandlingModeRequestedEventHandler();
    /// <summary>
    /// 選択操作モードの通知をリクエストする
    /// </summary>
    internal void AskHandlingMode()
    {
        EmitSignal(SignalName.AskHandlingModeRequested);
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
        EmitSignal(SignalName.HandlingModeNotified, (int)mode);
    }

    [Signal] public delegate void ResultNotifiedEventHandler(PickResult pickResult);
    /// <summary>
    /// ピック結果の通知を行う
    /// </summary>
    /// <param name="pickResult">ピック結果</param>
    internal void NotifyResult(PickResult pickResult)
    {
        EmitSignal(SignalName.ResultNotified, pickResult);
    }

    [Signal] public delegate void ResultsNotifiedEventHandler(PickResult[] pickResults);
    /// <summary>
    /// 複数一括ピック結果の通知を行う
    /// </summary>
    /// <param name="pickResults">ピック結果の配列</param>
    /// <remarks>複数のピック結果を一括で通知する場合はレイキャストによる取得ではないので座標値などを持たない</remarks>
    internal void NotifyResults(PickResult[] pickResults)
    {
        EmitSignal(SignalName.ResultsNotified, pickResults);
    }

    #endregion
    
    #region Events

    /// <summary>
    /// Applicationイベントの購読を開始する
    /// </summary>
    private void SubscribeApplicationEvents()
    {
        Application.Pick.AskHandlingModeRequested += OnAskHandlingModeRequested;
    }

    /// <summary>
    /// Applicationイベントの購読を解除する
    /// </summary>
    private void UnsubscribeApplicationEvents()
    {
        Application.Pick.AskHandlingModeRequested -= OnAskHandlingModeRequested;
    }

    /// <summary>
    /// 選択操作モードの通知がリクエストされたときに呼び出されるイベントハンドラ
    /// </summary>
    private void OnAskHandlingModeRequested()
    {
        Application.Pick.NotifyHandlingMode(_handlingMode);
    }

    /// <summary>
    /// 選択操作モードを変更し、変更内容を通知する
    /// </summary>
    /// <param name="mode">設定する選択操作モード</param>
    public void SetHandlingMode(PickHandlingMode mode)
    {
        if (_handlingMode != mode)
        {
            _handlingMode = mode;
        }

        Application.Pick.NotifyHandlingMode(_handlingMode);
    }

    #endregion

}