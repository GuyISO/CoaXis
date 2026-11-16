using Godot;

/// <summary>
/// 実行中にコンソール表示の代わりを担う簡易パネル
/// </summary>
public partial class MessageUi : PanelContainer
{
    #region Fields

    // 関連ノードのキャッシュ
    private RichTextLabel _label;
    private Label _fpsLabel;

    #endregion

    #region Lifecycle

    /// <summary>
    /// 子ノードを取得してApplicationイベントを購読する
    /// </summary>
    public override void _Ready()
    {
        EnsureChildNodes();
        SubscribeApplicationEvents();
        RefreshLabelText();
    }

    /// <summary>
    /// FPS 表示を更新する
    /// </summary>
    /// <param name="delta">前フレームからの経過秒</param>
    public override void _Process(double delta)
    {
        if (_fpsLabel == null)
        {
            return;
        }

        string text = $"FPS: {Engine.GetFramesPerSecond()}";
        if (_fpsLabel.Text != text)
        {
            _fpsLabel.Text = text;
        }
    }

    /// <summary>
    /// Applicationイベントの購読を解除する
    /// </summary>
    public override void _ExitTree()
    {
        UnsubscribeApplicationEvents();

        base._ExitTree();
    } 

    #endregion

    #region Events

    /// <summary>
    /// 子ノードのキャッシュを行う
    /// </summary>
    private void EnsureChildNodes()
    {
        _label = GetNodeOrNull<RichTextLabel>("VBoxContainer/RichTextLabel");
        _fpsLabel = GetNodeOrNull<Label>("VBoxContainer/FpsLabel");
    }

    /// <summary>
    /// Applicationイベントの購読を開始する
    /// </summary>
    private void SubscribeApplicationEvents()
    {
        Application.Log.Logged += OnLogged;
    }

    /// <summary>
    /// Applicationイベントの購読を解除する
    /// </summary>
    private void UnsubscribeApplicationEvents()
    {
        Application.Log.Logged -= OnLogged;
    }

    /// <summary>
    /// ログ出力と同時に画面へログを表示する
    /// </summary>
    /// <param name="level">ログレベル。</param>
    /// <param name="line">記録されたメッセージ。</param>
    private void OnLogged(int level, string line)
    {
        if (level < (int)LogLevel.Info)
        {
            return;
        }
        RefreshLabelText();
    }

    #endregion

    #region Helpers

    /// <summary>
    /// 現在のログ履歴をラベルへ反映する
    /// </summary>
    private void RefreshLabelText()
    {
        if (_label == null)
        {
            return;
        }

        _label.Text = string.Join("\n", Application.Log.History);
        _label.ScrollToLine(Mathf.Max(_label.GetLineCount() - 1, 0));
    }

    #endregion
}



