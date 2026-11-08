using System;
using System.Drawing;
using System.Windows.Forms;

namespace CoaXis.Ui;

/// <summary>CoaXisテーマのモーダルメッセージを表示する。</summary>
public static class CoaXisMessageBox
{
    /// <summary>現在のアクティブウィンドウをオーナーとしてメッセージを表示する。</summary>
    /// <param name="text">表示するメッセージ。</param>
    /// <param name="caption">タイトルバーに表示するタイトル。</param>
    /// <param name="buttons">表示するボタンの組み合わせ。</param>
    /// <param name="icon">表示する標準アイコン。</param>
    /// <returns>選択されたボタンの結果。</returns>
    public static DialogResult Show(
        string text,
        string caption,
        MessageBoxButtons buttons = MessageBoxButtons.OK,
        MessageBoxIcon icon = MessageBoxIcon.None) => Show(null, text, caption, buttons, icon);

    /// <summary>オーナーの操作を待機させてメッセージを表示する。</summary>
    /// <param name="owner">ダイアログのオーナー。nullの場合は現在のアクティブウィンドウを使用する。</param>
    /// <param name="text">表示するメッセージ。</param>
    /// <param name="caption">タイトルバーに表示するタイトル。</param>
    /// <param name="buttons">表示するボタンの組み合わせ。</param>
    /// <param name="icon">表示する標準アイコン。</param>
    /// <returns>選択されたボタンの結果。</returns>
    public static DialogResult Show(
        IWin32Window owner,
        string text,
        string caption,
        MessageBoxButtons buttons = MessageBoxButtons.OK,
        MessageBoxIcon icon = MessageBoxIcon.None)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(caption);
        (string Text, DialogResult Result)[] choices = GetChoices(buttons);
        Icon messageIcon = GetIcon(icon);
        Screen screen = owner != null ? Screen.FromHandle(owner.Handle)
            : Form.ActiveForm != null ? Screen.FromControl(Form.ActiveForm)
            : Screen.FromPoint(Cursor.Position);
        using var dialog = new MessageDialog(text, caption, choices, messageIcon, screen.WorkingArea.Height);
        return owner == null ? dialog.ShowDialog() : dialog.ShowDialog(owner);
    }

    private static (string Text, DialogResult Result)[] GetChoices(MessageBoxButtons buttons) => buttons switch
    {
        MessageBoxButtons.OK => new[] { ("OK", DialogResult.OK) },
        MessageBoxButtons.OKCancel => new[] { ("OK", DialogResult.OK), ("キャンセル", DialogResult.Cancel) },
        MessageBoxButtons.YesNo => new[] { ("はい", DialogResult.Yes), ("いいえ", DialogResult.No) },
        MessageBoxButtons.YesNoCancel => new[] { ("はい", DialogResult.Yes), ("いいえ", DialogResult.No), ("キャンセル", DialogResult.Cancel) },
        MessageBoxButtons.RetryCancel => new[] { ("再試行", DialogResult.Retry), ("キャンセル", DialogResult.Cancel) },
        MessageBoxButtons.AbortRetryIgnore => new[] { ("中止", DialogResult.Abort), ("再試行", DialogResult.Retry), ("無視", DialogResult.Ignore) },
        MessageBoxButtons.CancelTryContinue => new[] { ("キャンセル", DialogResult.Cancel), ("再試行", DialogResult.TryAgain), ("続行", DialogResult.Continue) },
        _ => throw new ArgumentOutOfRangeException(nameof(buttons), buttons, "未対応のボタン指定です。")
    };

    private static Icon GetIcon(MessageBoxIcon icon) => icon switch
    {
        MessageBoxIcon.None => null,
        MessageBoxIcon.Error => SystemIcons.Error,
        MessageBoxIcon.Question => SystemIcons.Question,
        MessageBoxIcon.Warning => SystemIcons.Warning,
        MessageBoxIcon.Information => SystemIcons.Information,
        _ => throw new ArgumentOutOfRangeException(nameof(icon), icon, "未対応のアイコン指定です。")
    };

    private sealed class MessageDialog : CoaXisForm
    {
        private readonly Image _icon;
        private readonly bool _canCancel;
        private readonly bool _closeReturnsOk;

        public MessageDialog(string text, string caption, (string Text, DialogResult Result)[] choices, Icon icon, int screenHeight)
        {
            Text = caption;
            _closeReturnsOk = choices.Length == 1;
            HeaderTitle = caption;
            Header.SetDialogMode();
            ShowInTaskbar = false;
            MinimizeBox = false;
            MaximizeBox = false;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.Font;

            var message = new CoaXisLabel
            {
                AutoSize = true,
                MaximumSize = new Size(420, 0),
                Text = text,
                Margin = new Padding(0)
            };
            var content = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                WrapContents = false,
                Padding = new Padding(20, 20, 20, 10),
                Tag = "Background"
            };
            if (icon != null)
            {
                _icon = icon.ToBitmap();
                content.Controls.Add(new PictureBox
                {
                    Image = _icon,
                    Size = new Size(32, 32),
                    SizeMode = PictureBoxSizeMode.CenterImage,
                    Margin = new Padding(0, 0, 16, 0),
                    AccessibleName = icon == SystemIcons.Error ? "エラー" : "メッセージアイコン"
                });
            }
            content.Controls.Add(message);

            var buttonBar = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Height = 62,
                Padding = new Padding(12, 8, 12, 8),
                Tag = "Background"
            };
            for (int i = choices.Length - 1; i >= 0; i--)
            {
                var button = new CoaXisButton
                {
                    Text = choices[i].Text,
                    DialogResult = choices[i].Result,
                    MinimumSize = new Size(86, 34),
                    AutoSize = true,
                    Tag = i == 0 ? "Primary" : null
                };
                buttonBar.Controls.Add(button);
                if (i == 0)
                {
                    AcceptButton = button;
                }
                if (choices[i].Result == DialogResult.Cancel || choices.Length == 1)
                {
                    CancelButton = button;
                    _canCancel = true;
                }
            }

            Controls.Add(content);
            Controls.Add(buttonBar);
            int measuredHeight = TextRenderer.MeasureText(text, ThemeManager.StandardFont, new Size(420, int.MaxValue),
                TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl).Height;
            int maxHeight = Math.Max(200, screenHeight - 80);
            ClientSize = new Size(520, Math.Min(maxHeight, Math.Max(180, measuredHeight + 140)));
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (DialogResult == DialogResult.Cancel && _closeReturnsOk)
            {
                DialogResult = DialogResult.OK;
            }
            else if (!_canCancel && DialogResult == DialogResult.Cancel && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
            }
            base.OnFormClosing(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _icon?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
