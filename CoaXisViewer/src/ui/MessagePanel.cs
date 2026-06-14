using Godot;
using System;
using System.Text;

/// <summary>
/// シーン内のデバッグログ表示を担う簡易パネルです。
/// </summary>
public partial class MessagePanel : PanelContainer
{
	#region Fields

	private const int MaxLines = 200;

    [Export] private RichTextLabel _label;
    private StringBuilder _buffer = new StringBuilder();

	#endregion

	#region Properties

    /// <summary>
    /// 現在アクティブなデバッグウィンドウインスタンスです。
    /// </summary>
    public static MessagePanel I { get; private set; }

	#endregion

	#region Lifecycle

    public override void _Ready()
    {
        // 関連ノードのキャッシュ
        if (_label == null)
        {
            _label = GetNodeOrNull<RichTextLabel>("RichTextLabel");
            _label ??= FindChild("RichTextLabel", true, false) as RichTextLabel;
        }

        // イベント購読の登録
        LogHub.I.Logged += OnLogged;
    }

    public override void _ExitTree()
    {
        // イベント購読の解除
        LogHub.I.Logged -= OnLogged;

        if (I == this)
            I = null;
    }

	#endregion

	#region Public API

    /// <summary>
    /// 1行ログを画面へ追加します。
    /// </summary>
    /// <param name="text">追加する文字列。</param>
    public void AddLine(string text)
    {
        if (_label == null)
            return;

        _buffer.AppendLine(text);

        // 行数制限
        var lines = _buffer.ToString().Split('\n');
        if (lines.Length > MaxLines)
        {
            _buffer.Clear();
            for (int i = lines.Length - MaxLines; i < lines.Length; i++)
                _buffer.AppendLine(lines[i]);
        }

        _label.Text = _buffer.ToString();
        _label.ScrollToLine(Mathf.Max(_label.GetLineCount() - 1, 0));
    }

    /// <summary>
    /// 標準出力とデバッグウィンドウへ同時にログを出力します。
    /// </summary>
    /// <param name="msg">出力するメッセージ。</param>
    public static void OnLogged(LogLevel level, string msg)
    {
        string line = $"{DateTime.Now:HH:mm:ss} [{level}] {msg}";
        MessagePanel.I?.AddLine(line);
    }

	#endregion
}



