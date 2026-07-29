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

    // ログのバッファ
    private readonly StringBuilder _buffer = new StringBuilder();

    #endregion

    #region Lifecycle

    public override void _Ready()
    {
        EnsureChildNodes();
        SubscribeApplicationEvents();
        OnSettingsNotified();
    }

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
        _label = GetNodeOrNull<RichTextLabel>("RichTextLabel");
    }

    /// <summary>
    /// Applicationイベントの購読を開始する
    /// </summary>
    private void SubscribeApplicationEvents()
    {
        Application.Log.Notified += OnLogNotified;
        Application.Setting.Event.SettingsNotified += OnSettingsNotified;
    }

    /// <summary>
    /// Applicationイベントの購読を解除する
    /// </summary>
    private void UnsubscribeApplicationEvents()
    {
        Application.Log.Notified -= OnLogNotified;
        Application.Setting.Event.SettingsNotified -= OnSettingsNotified;
    }

    /// <summary>
    /// ログ出力と同時に画面へログを表示する
    /// </summary>
    /// <param name="line">記録されたメッセージ</param>
    private void OnLogNotified(string line)
    {
        AddLine(line);
    }

    /// <summary>
    /// 設定変更通知を受けたときに表示を再整形する
    /// </summary>
    private void OnSettingsNotified()
    {
        TrimBufferToMaxLines();
        RefreshLabelText();
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
        TrimBufferToMaxLines();
        RefreshLabelText();
    }

    /// <summary>
    /// バッファ行数を上限まで切り詰める
    /// </summary>
    private void TrimBufferToMaxLines()
    {
        int maxLines = Application.Setting.Service.Current.Ui.MessageMaxLines;

        if (maxLines < UiSettings.MinMessageMaxLines)
        {
            maxLines = UiSettings.DefaultMessageMaxLines;
        }

        // 行数制限
        var lines = _buffer.ToString().Split('\n');
        if (lines.Length > maxLines)
        {
            _buffer.Clear();
            for (int i = lines.Length - maxLines; i < lines.Length; i++)
                _buffer.AppendLine(lines[i]);
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



