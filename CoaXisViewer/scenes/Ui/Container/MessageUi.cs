using Godot;
using System;
using System.Text;

/// <summary>
/// 実行中にコンソール表示の代わりを担う簡易パネル
/// </summary>
public partial class MessageUi : PanelContainer
{
    #region Fields

    [Export] private int _maxLines = 50;

    // 関連ノードのキャッシュ
    private RichTextLabel _label;

    // ログのバッファ
    private readonly StringBuilder _buffer = new StringBuilder();

    #endregion

    #region Lifecycle

    public override void _Ready()
    {
        // 関連ノードのキャッシュ
        _label = GetNodeOrNull<RichTextLabel>("RichTextLabel");

        // イベント購読の登録
        LogHub.Instance.Logged += OnLogged;
    }

    public override void _ExitTree()
    {
        // イベント購読の解除
        LogHub.Instance.Logged -= OnLogged;
    }

    #endregion

    #region Events

    /// <summary>
    /// ログ出力と同時に画面へログを表示する
    /// </summary>
    /// <param name="line">記録されたメッセージ</param>
    private void OnLogged(string line)
    {
        AddLine(line);
    }

    #endregion

    #region Internal Helpers

    /// <summary>
    /// 1行ログを画面へ追加する
    /// </summary>
    /// <param name="text">追加する文字列</param>
    private void AddLine(string text)
    {
        if (_label == null)
            return;

        _buffer.AppendLine(text);

        // 行数制限
        var lines = _buffer.ToString().Split('\n');
        if (lines.Length > _maxLines)
        {
            _buffer.Clear();
            for (int i = lines.Length - _maxLines; i < lines.Length; i++)
                _buffer.AppendLine(lines[i]);
        }

        _label.Text = _buffer.ToString();
        _label.ScrollToLine(Mathf.Max(_label.GetLineCount() - 1, 0));
    }

    #endregion
}



