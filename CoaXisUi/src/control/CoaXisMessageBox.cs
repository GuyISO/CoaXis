using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Svg;

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
        (string ResourceName, string AccessibleName) messageIcon = GetIcon(icon);
        using var dialog = new MessageDialog(text, caption, choices, messageIcon);
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

    private static (string ResourceName, string AccessibleName) GetIcon(MessageBoxIcon icon) => icon switch
    {
        MessageBoxIcon.None => (null, null),
        MessageBoxIcon.Error => (CoaXisUiAssets.MessageErrorIconResourceName, "エラー"),
        MessageBoxIcon.Question => (CoaXisUiAssets.MessageQuestionIconResourceName, "質問"),
        MessageBoxIcon.Warning => (CoaXisUiAssets.MessageWarningIconResourceName, "警告"),
        MessageBoxIcon.Information => (CoaXisUiAssets.MessageInfoIconResourceName, "情報"),
        _ => throw new ArgumentOutOfRangeException(nameof(icon), icon, "未対応のアイコン指定です。")
    };

    private sealed class MessageDialog : CoaXisForm
    {
        private readonly SvgDocument _iconDocument;
        private readonly PictureBox _iconBox;
        private readonly CoaXisLabel _message;
        private readonly FlowLayoutPanel _content;
        private readonly FlowLayoutPanel _buttonBar;
        private Image _icon;
        private readonly bool _canCancel;
        private readonly bool _closeReturnsOk;

        public MessageDialog(
            string text,
            string caption,
            (string Text, DialogResult Result)[] choices,
            (string ResourceName, string AccessibleName) icon)
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
            AutoScaleMode = AutoScaleMode.None;

            _message = new CoaXisLabel
            {
                AutoSize = true,
                MaximumSize = new Size(420, 0),
                Text = text,
                Margin = new Padding(0)
            };
            _content = new CoaXisFlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                WrapContents = false,
                Padding = new Padding(20, 20, 20, 10),
                SurfaceRole = CoaXisSurfaceRole.Background
            };
            if (icon.ResourceName != null)
            {
                using Stream iconStream = CoaXisUiAssets.OpenStream(icon.ResourceName);
                _iconDocument = SvgDocument.Open<SvgDocument>(iconStream)
                    ?? throw new InvalidDataException($"メッセージアイコンを読み込めません: {icon.ResourceName}");
                _iconBox = new PictureBox
                {
                    Size = new Size(32, 32),
                    SizeMode = PictureBoxSizeMode.Normal,
                    Margin = new Padding(0, 0, 16, 0),
                    AccessibleName = icon.AccessibleName
                };
                _content.Controls.Add(_iconBox);
            }
            _content.Controls.Add(_message);

            _buttonBar = new CoaXisFlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Height = 62,
                Padding = new Padding(12, 8, 12, 8),
                SurfaceRole = CoaXisSurfaceRole.Background
            };
            for (int i = choices.Length - 1; i >= 0; i--)
            {
                var button = new CoaXisButton
                {
                    Text = choices[i].Text,
                    DialogResult = choices[i].Result,
                    MinimumSize = new Size(86, 34),
                    AutoSize = true,
                    Variant = i == 0 ? CoaXisButtonVariant.Primary : CoaXisButtonVariant.Secondary
                };
                _buttonBar.Controls.Add(button);
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

            Controls.Add(_content);
            Controls.Add(_buttonBar);
            ClientSize = new Size(520, 180);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            UpdateDialogLayout();
        }

        protected override void OnDpiChanged(DpiChangedEventArgs e)
        {
            base.OnDpiChanged(e);
            if (IsHandleCreated && _message != null)
            {
                UpdateDialogLayout();
            }
        }

        private void UpdateDialogLayout()
        {
            int dpi = DeviceDpi;
            int Scale(int value) => (int)Math.Round(value * dpi / 96.0);
            Rectangle workingArea = Screen.FromControl(this).WorkingArea;
            Point center = new(Left + Width / 2, Top + Height / 2);
            int frameWidth = Width - ClientSize.Width;
            int frameHeight = Height - ClientSize.Height;
            int maxWidth = Math.Max(1, workingArea.Width - frameWidth);
            int maxHeight = Math.Max(1, workingArea.Height - frameHeight);
            int clientWidth = Math.Min(Scale(520), maxWidth);

            SuspendLayout();
            _content.SuspendLayout();
            _buttonBar.SuspendLayout();
            Header.SetDialogDpi(dpi);
            _content.Padding = new Padding(Scale(20), Scale(20), Scale(20), Scale(10));
            _buttonBar.Height = Scale(62);
            _buttonBar.Padding = new Padding(Scale(12), Scale(8), Scale(12), Scale(8));
            foreach (Control control in _buttonBar.Controls)
            {
                control.MinimumSize = new Size(Scale(86), Scale(34));
                control.Margin = new Padding(Scale(3));
            }

            int iconWidth = 0;
            if (_iconBox != null)
            {
                int iconSize = Scale(32);
                _iconBox.Size = new Size(iconSize, iconSize);
                _iconBox.Margin = new Padding(0, 0, Scale(16), 0);
                Bitmap rendered = _iconDocument.Draw(iconSize, iconSize);
                _iconBox.Image = rendered;
                _icon?.Dispose();
                _icon = rendered;
                iconWidth = iconSize + Scale(16);
            }

            int messageWidth = Math.Max(1, clientWidth - _content.Padding.Horizontal - iconWidth);
            _message.MaximumSize = new Size(messageWidth, 0);
            using Graphics graphics = CreateGraphics();
            int messageHeight = TextRenderer.MeasureText(graphics, _message.Text, _message.Font,
                new Size(messageWidth, int.MaxValue), TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl).Height;
            int contentHeight = _content.Padding.Vertical + Math.Max(messageHeight, _iconBox?.Height ?? 0);
            int clientHeight = Math.Min(maxHeight, Math.Max(Scale(180), Header.Height + _buttonBar.Height + contentHeight));
            ClientSize = new Size(clientWidth, clientHeight);
            _buttonBar.ResumeLayout(true);
            _content.ResumeLayout(true);
            ResumeLayout(true);

            int left = Math.Clamp(center.X - Width / 2, workingArea.Left, Math.Max(workingArea.Left, workingArea.Right - Width));
            int top = Math.Clamp(center.Y - Height / 2, workingArea.Top, Math.Max(workingArea.Top, workingArea.Bottom - Height));
            Location = new Point(left, top);
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
