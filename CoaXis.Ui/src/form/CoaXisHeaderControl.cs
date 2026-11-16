using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Svg;

namespace CoaXis.Ui;

/// <summary>CoaXis共通のタイトル、テーマ切替、ウィンドウ操作を表示する。</summary>
public sealed class CoaXisHeaderControl : Panel, ICoaXisThemedControl
{
    private static readonly Font WindowButtonFont = new("Segoe MDL2 Assets", 10F);
    private readonly Button _themeButton = new() { Dock = DockStyle.Right, Width = 96 };
    private readonly Button _minimizeButton = new() { Dock = DockStyle.Right, Width = 46, Text = "\uE921", AccessibleName = "最小化" };
    private readonly CaptionHitTestButton _maximizeButton = new() { Dock = DockStyle.Right, Width = 46, AccessibleName = "最大化" };
    private readonly Button _closeButton = new() { Dock = DockStyle.Right, Width = 46, Text = "\uE8BB", AccessibleName = "閉じる" };
    private readonly SvgDocument _logoDocument;
    private Bitmap _renderedLogo;
    private Size _renderedLogoSize;
    private Theme _theme = ThemeManager.CurrentTheme;
    private string _applicationTitle = string.Empty;
    private bool _maximizeHovered;
    private bool _showThemeButton = true;

    /// <summary>ヘッダーを作成する。</summary>
    public CoaXisHeaderControl()
    {
        Dock = DockStyle.Top;
        Height = 36;
        DoubleBuffered = true;
        SetStyle(ControlStyles.ResizeRedraw, true);
        AccessibleName = "CoaXis タイトルバー";
        using Stream logoStream = CoaXisUiAssets.OpenStream(CoaXisUiAssets.LogoResourceName);
        _logoDocument = SvgDocument.Open<SvgDocument>(logoStream)
            ?? throw new InvalidDataException($"CoaXisロゴを読み込めません: {CoaXisUiAssets.LogoResourceName}");
        Controls.Add(_themeButton);
        Controls.Add(_minimizeButton);
        Controls.Add(_maximizeButton);
        Controls.Add(_closeButton);

        _themeButton.Click += OnThemeClick;
        _minimizeButton.Click += OnMinimizeClick;
        _maximizeButton.Click += OnMaximizeClick;
        _closeButton.Click += OnCloseClick;
        ApplyTheme(ThemeManager.CurrentTheme);
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

    internal void SetDialogMode()
    {
        _showThemeButton = false;
        _themeButton.Visible = false;
        _minimizeButton.Visible = false;
        _maximizeButton.Visible = false;
    }

    internal void SetDialogDpi(int dpi)
    {
        Height = (int)Math.Round(36 * dpi / 96.0);
        _closeButton.Width = (int)Math.Round(46 * dpi / 96.0);
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

        _themeButton.Font = theme.ButtonFont;
        _themeButton.Visible = _showThemeButton && (theme is LightTheme or DarkTheme);
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

    void ICoaXisThemedControl.ApplyTheme(Theme theme) => ApplyTheme(theme);

    /// <inheritdoc />
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        int dpiScale = DeviceDpi;
        Rectangle contentBounds = DisplayRectangle;
        int logoHeight = Math.Max(1, Math.Min(
            (int)Math.Round(40 * dpiScale / 96.0),
            contentBounds.Height - (int)Math.Round(6 * dpiScale / 96.0)));
        int logoWidth = Math.Max(1, (int)Math.Round(logoHeight * _logoDocument.ViewBox.Width / _logoDocument.ViewBox.Height));
        var logoSize = new Size(logoWidth, logoHeight);
        if (_renderedLogo == null || _renderedLogoSize != logoSize)
        {
            Bitmap rendered = _logoDocument.Draw(logoWidth, logoHeight);
            _renderedLogo?.Dispose();
            _renderedLogo = rendered;
            _renderedLogoSize = logoSize;
        }

        int left = (int)Math.Round(8 * DeviceDpi / 96.0);
        var logoBounds = new Rectangle(left, contentBounds.Top + (contentBounds.Height - logoHeight) / 2, logoWidth, logoHeight);
        e.Graphics.DrawImageUnscaled(_renderedLogo, logoBounds.Location);

        int titleLeft = logoBounds.Right + (int)Math.Round(10 * DeviceDpi / 96.0);
        var flags = TextFormatFlags.NoPadding | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine;
        int controlsLeft = _themeButton.Visible ? _themeButton.Left : _closeButton.Left;
        int titleWidth = Math.Max(0, controlsLeft - titleLeft - 8);
        var nameBounds = new Rectangle(titleLeft, contentBounds.Top, titleWidth, contentBounds.Height);
        Color titleColor = _theme.Text;
        TextRenderer.DrawText(e.Graphics, _applicationTitle, _theme.TitleFont, nameBounds, titleColor, flags | TextFormatFlags.EndEllipsis);
    }

    /// <inheritdoc />
    protected override void OnPaddingChanged(EventArgs e)
    {
        base.OnPaddingChanged(e);
        Invalidate();
    }

    /// <inheritdoc />
    protected override void OnDpiChangedAfterParent(EventArgs e)
    {
        base.OnDpiChangedAfterParent(e);
        Invalidate();
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _renderedLogo?.Dispose();
        }

        base.Dispose(disposing);
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
        if (_theme is not LightTheme && _theme is not DarkTheme)
        {
            return;
        }

        ThemeManager.SetTheme(_theme is DarkTheme ? new LightTheme() : new DarkTheme());
        if (FindForm() is not CoaXisForm)
        {
            ThemeManager.Apply(FindForm() ?? (Control)this);
        }
    }

    private void OnMinimizeClick(object sender, EventArgs e)
    {
        Form form = FindForm();
        if (form != null)
        {
            form.WindowState = FormWindowState.Minimized;
        }
    }

    private void OnMaximizeClick(object sender, EventArgs e) => ToggleWindowState();

    internal void ToggleWindowState()
    {
        Form form = FindForm();
        if (form != null && form.MaximizeBox)
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
