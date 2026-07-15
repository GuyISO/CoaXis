using Godot;

/// <summary>
/// ピック関連の状態管理を担当するサービス
/// </summary>
public partial class PickService : Node
{
    #region Fields

    // 選択操作モードの現在値を保持するフィールド、初期値は選択操作とする
    private PickHandlingMode _currentPickHandlingMode = PickHandlingMode.Selection;

    #endregion

    #region Lifecycle

    public override void _Ready()
    {
        Application.Pick.Event.AskPickHandlingModeRequested += OnAskPickHandlingModeRequested;
        Application.Pick.Event.PickHandlingModeNotified += OnPickHandlingModeNotified;
    }

    public override void _ExitTree()
    {
        Application.Pick.Event.AskPickHandlingModeRequested -= OnAskPickHandlingModeRequested;
        Application.Pick.Event.PickHandlingModeNotified -= OnPickHandlingModeNotified;

        base._ExitTree();
    }

    #endregion

    #region Events

    /// <summary>
    /// 選択操作モードの通知がリクエストされたときに呼び出されるイベントハンドラ
    /// </summary>
    private void OnAskPickHandlingModeRequested()
    {
        Application.Pick.Event.NotifyPickHandlingMode(_currentPickHandlingMode);
    }

    /// <summary>
    /// 選択操作モードが通知されたときに呼び出されるイベントハンドラ
    /// </summary>
    /// <param name="mode">通知された選択操作モード</param>
    private void OnPickHandlingModeNotified(PickHandlingMode mode)
    {
        _currentPickHandlingMode = mode;
    }

    #endregion

    #region Public API

    /// <summary>
    /// 現在の選択操作モードを取得する
    /// </summary>
    internal PickHandlingMode CurrentHandlingMode => _currentPickHandlingMode;

    /// <summary>
    /// 選択操作モードを変更し、変更内容を通知する
    /// </summary>
    /// <param name="mode">設定する選択操作モード</param>
    internal void SetHandlingMode(PickHandlingMode mode)
    {
        _currentPickHandlingMode = mode;
        Application.Pick.Event.NotifyPickHandlingMode(mode);
    }

    #endregion
}