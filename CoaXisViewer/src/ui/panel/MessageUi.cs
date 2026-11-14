using Godot;
using System;
using System.Text;

/// <summary>
/// 実行中にコンソール表示の代わりを担う簡易パネル
/// </summary>
public partial class MessageUi : PanelContainer
{
    #region Fields

    // 関連ノードのキャッシュ
    private RichTextLabel _label;
    private Label _fpsLabel;

    // ログのバッファ
    private readonly StringBuilder _buffer = new StringBuilder();

    #endregion

    #region Lifecycle

    /// <summary>
    /// 子ノードを取得してApplicationイベントを購読する
    /// </summary>
    public override void _Ready()
    {
        EnsureChildNodes();
        SubscribeApplicationEvents();
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
    /// <param name="line">記録されたメッセージ</param>
    private void OnLogged(int level, string line)
    {
        if (level < (int)LogLevel.Info)
        {
            return;
        }
        AddLine(line);
    }

    #endregion

    #region Helpers

    /// <summary>
    /// 1行ログを画面へ追加する
    /// </summary>
    /// <param name="text">追加する文字列</param>
    private void AddLine(string text)
    {
        if (_label == null)
            return;

        if (_buffer.Length > 0)
            _buffer.Append('\n');

        _buffer.Append(text);
        TrimBufferToMaxLines();
        RefreshLabelText();
    }

    /// <summary>
    /// バッファ行数を上限まで切り詰める
    /// </summary>
    private void TrimBufferToMaxLines()
    {
        int maxLines = Application.Setting.Current.Ui.MessageMaxLines;

        if (maxLines < UiSettings.MinMessageMaxLines)
        {
            maxLines = UiSettings.DefaultMessageMaxLines;
        }

        // 行数制限
        var lines = _buffer.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length > maxLines)
        {
            _buffer.Clear();
            for (int i = lines.Length - maxLines; i < lines.Length; i++)
            {
                if (_buffer.Length > 0)
                    _buffer.Append('\n');

                _buffer.Append(lines[i]);
            }
        }
    }

    /// <summary>
    /// 現在バッファをラベルへ反映する
    /// </summary>
    private void RefreshLabelText()
    {
        if (_label == null)
        {
            return;
        }

        _label.Text = _buffer.ToString();
        _label.ScrollToLine(Mathf.Max(_label.GetLineCount() - 1, 0));
    }

    #endregion
}



