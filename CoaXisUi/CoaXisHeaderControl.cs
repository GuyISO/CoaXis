using System;
using System.Drawing;
using System.Windows.Forms;

namespace CoaXis.Ui;

/// <summary>CoaXis共通のタイトル、テーマ切替、ウィンドウ操作を表示する。</summary>
public sealed class CoaXisHeaderControl : Panel
{
    private static readonly Font TitleFont = ThemeManager.TitleFont;
    private static readonly Font WindowButtonFont = new("Segoe MDL2 Assets", 10F);
    private readonly Button _themeButton = new() { Dock = DockStyle.Right, Width = 96 };
    private readonly Button _minimizeButton = new() { Dock = DockStyle.Right, Width = 46, Text = "\uE921", AccessibleName = "最小化" };
    private readonly CaptionHitTestButton _maximizeButton = new() { Dock = DockStyle.Right, Width = 46, AccessibleName = "最大化" };
    private readonly Button _closeButton = new() { Dock = DockStyle.Right, Width = 46, Text = "\uE8BB", AccessibleName = "閉じる" };
    private Theme _theme = ThemeManager.CurrentTheme;
    private string _applicationTitle = string.Empty;
    private bool _maximizeHovered;

    /// <summary>ヘッダーを作成する。</summary>
    public CoaXisHeaderControl()
    {
        Dock = DockStyle.Top;
        Height = 46;
        DoubleBuffered = true;
        AccessibleName = "CoaXis タイトルバー";
        Controls.Add(_themeButton);
        Controls.Add(_minimizeButton);
        Controls.Add(_maximizeButton);
        Controls.Add(_closeButton);

        _themeButton.Click += OnThemeClick;
        _minimizeButton.Click += OnMinimizeClick;
        _maximizeButton.Click += OnMaximizeClick;
        _closeButton.Click += OnCloseClick;
    }

    /// <summary>CoaXisの後ろに表示するアプリケーション名を取得または設定する。</summary>
    public string ApplicationTitle
    {
        get => _applicationTitle;
        set
        {
            _applicationTitle = value ?? string.Empty;
            Invalidate();
        }
    }

    internal bool IsMaximizeButton(Point screenPoint) =>
        _maximizeButton.IsHandleCreated && _maximizeButton.RectangleToScreen(_maximizeButton.ClientRectangle).Contains(screenPoint);

    internal void SetMaximizeHovered(bool hovered)
    {
        if (_maximizeHovered == hovered)
        {
            return;
        }

        _maximizeHovered = hovered;
        _maximizeButton.BackColor = hovered ? _theme.HeaderHover : _theme.Header;
    }

    internal void UpdateWindowState()
    {
        bool maximized = FindForm()?.WindowState == FormWindowState.Maximized;
        _maximizeButton.Text = maximized ? "\uE923" : "\uE922";
        _maximizeButton.AccessibleName = maximized ? "元に戻す" : "最大化";
        SetMaximizeHovered(false);
    }

    internal void ApplyTheme(Theme theme)
    {
        _theme = theme;
        BackColor = theme.Header;
        ForeColor = theme.Text;
        foreach (Button button in Controls)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = theme.HeaderHover;
            button.FlatAppearance.MouseDownBackColor = theme.Border;
            button.UseVisualStyleBackColor = false;
            button.BackColor = theme.Header;
            button.ForeColor = theme.Text;
            button.Padding = Padding.Empty;
            button.Cursor = Cursors.Default;
            button.Font = WindowButtonFont;
        }

        _themeButton.Font = ThemeManager.ButtonFont;
        _themeButton.Text = theme is DarkTheme ? "☀ Light" : "🌙 Dark";
        _themeButton.AccessibleName = theme is DarkTheme ? "ライトテーマに切り替え" : "ダークテーマに切り替え";
        _closeButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(196, 43, 28);
        _closeButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(160, 30, 20);
        _closeButton.MouseEnter -= OnCloseMouseEnter;
        _closeButton.MouseEnter += OnCloseMouseEnter;
        _closeButton.MouseLeave -= OnCloseMouseLeave;
        _closeButton.MouseLeave += OnCloseMouseLeave;
        _maximizeButton.BackColor = _maximizeHovered ? theme.HeaderHover : theme.Header;
        Invalidate();
    }

    /// <inheritdoc />
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        int iconSize = (int)Math.Round(22 * DeviceDpi / 96.0);
        int left = (int)Math.Round(14 * DeviceDpi / 96.0);
        var icon = new Rectangle(left, (Height - iconSize) / 2, iconSize, iconSize);
        using var pen = new Pen(_theme.Primary, Math.Max(1F, 2F * DeviceDpi / 96F));
        e.Graphics.DrawRectangle(pen, icon);
        e.Graphics.DrawLine(pen, icon.Left + 5, icon.Top + 5, icon.Right - 5, icon.Bottom - 5);
        e.Graphics.DrawLine(pen, icon.Right - 5, icon.Top + 5, icon.Left + 5, icon.Bottom - 5);

        int titleLeft = icon.Right + (int)Math.Round(12 * DeviceDpi / 96.0);
        var flags = TextFormatFlags.NoPadding | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine;
        Size brandSize = TextRenderer.MeasureText(e.Graphics, "CoaXis", TitleFont, Size.Empty, flags);
        int titleWidth = Math.Max(0, _themeButton.Left - titleLeft - 8);
        var brandBounds = new Rectangle(titleLeft, 0, Math.Min(brandSize.Width, titleWidth), Height);
        TextRenderer.DrawText(e.Graphics, "CoaXis", TitleFont, brandBounds, _theme.Primary, flags | TextFormatFlags.EndEllipsis);
        int nameLeft = titleLeft + brandSize.Width + (int)Math.Round(8 * DeviceDpi / 96.0);
        var nameBounds = new Rectangle(nameLeft, 0, Math.Max(0, _themeButton.Left - nameLeft - 8), Height);
        TextRenderer.DrawText(e.Graphics, _applicationTitle, TitleFont, nameBounds, _theme.Text, flags | TextFormatFlags.EndEllipsis);
    }

    /// <inheritdoc />
    protected override void WndProc(ref Message m)
    {
        if (m.Msg == 0x0084)
        {
            m.Result = new IntPtr(-1);
            return;
        }

        base.WndProc(ref m);
    }

    private void OnThemeClick(object sender, EventArgs e)
    {
        ThemeManager.SetTheme(_theme is DarkTheme ? new LightTheme() : new DarkTheme());
        ThemeManager.Apply(FindForm() ?? (Control)this);
    }

    private void OnMinimizeClick(object sender, EventArgs e)
    {
        Form form = FindForm();
        if (form != null)
        {
            form.WindowState = FormWindowState.Minimized;
        }
    }

    private void OnMaximizeClick(object sender, EventArgs e)
    {
        Form form = FindForm();
        if (form != null)
        {
            form.WindowState = form.WindowState == FormWindowState.Maximized
                ? FormWindowState.Normal
                : FormWindowState.Maximized;
        }
    }

    private void OnCloseClick(object sender, EventArgs e) => FindForm()?.Close();

    private void OnCloseMouseEnter(object sender, EventArgs e) => _closeButton.ForeColor = Color.White;

    private void OnCloseMouseLeave(object sender, EventArgs e) => _closeButton.ForeColor = _theme.Text;

    private sealed class CaptionHitTestButton : Button
    {
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0084)
            {
                m.Result = new IntPtr(-1);
                return;
            }

            base.WndProc(ref m);
        }
    }
}
