using Godot;

/// <summary>
/// ピック関連の状態管理を担当するサービス
/// </summary>
public partial class PickService : Node
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

    #region Events

    /// <summary>
    /// 選択操作モードの通知がリクエストされたときに呼び出されるイベントハンドラ
    /// </summary>
    private void OnAskHandlingModeRequested()
    {
        Application.Pick.Event.NotifyHandlingMode(_handlingMode);
    }

    /// <summary>
    /// 選択操作モードの変更がリクエストされたときに呼び出されるイベントハンドラ
    /// </summary>
    /// <param name="mode">設定する選択操作モード</param>
    private void OnSetHandlingModeRequested(PickHandlingMode mode)
    {
        SetHandlingMode(mode);
    }

    #endregion

    #region Internal Helpers

    /// <summary>
    /// Applicationイベントの購読を開始する
    /// </summary>
    private void SubscribeApplicationEvents()
    {
        Application.Pick.Event.AskHandlingModeRequested += OnAskHandlingModeRequested;
        Application.Pick.Event.SetHandlingModeRequested += OnSetHandlingModeRequested;
    }

    /// <summary>
    /// Applicationイベントの購読を解除する
    /// </summary>
    private void UnsubscribeApplicationEvents()
    {
        Application.Pick.Event.AskHandlingModeRequested -= OnAskHandlingModeRequested;
        Application.Pick.Event.SetHandlingModeRequested -= OnSetHandlingModeRequested;
    }

    /// <summary>
    /// 選択操作モードを変更し、変更内容を通知する
    /// </summary>
    /// <param name="mode">設定する選択操作モード</param>
    private void SetHandlingMode(PickHandlingMode mode)
    {
        if (_handlingMode != mode)
        {
            _handlingMode = mode;
            Application.Pick.Event.NotifyHandlingMode(mode);
        }
    }

    #endregion

}