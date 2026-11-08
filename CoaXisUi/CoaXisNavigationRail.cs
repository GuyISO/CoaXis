using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Svg;

namespace CoaXis.Ui;

/// <summary>テーマ対応のナビゲーションレールとページ領域を提供する。</summary>
public sealed class CoaXisNavigationRail : TableLayoutPanel
{
    private readonly FlowLayoutPanel _rail = new()
    {
        Dock = DockStyle.Fill,
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        Padding = new Padding(4, 12, 4, 0)
    };
    private readonly Panel _pages = new() { Dock = DockStyle.Fill, Tag = "Background" };
    private readonly List<(CoaXisNavigationButton Button, Control Page)> _destinations = new();
    private readonly ToolTip _toolTip = new();
    private Control _selectedPage;

    /// <summary>ナビゲーションレールとページ領域を作成する。</summary>
    public CoaXisNavigationRail()
    {
        Dock = DockStyle.Fill;
        Margin = Padding.Empty;
        ColumnCount = 2;
        RowCount = 1;
        ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60));
        ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(_rail, 0, 0);
        Controls.Add(_pages, 1, 0);
    }

    /// <summary>選択されているページを取得する。</summary>
    public Control SelectedPage => _selectedPage;

    /// <summary>選択ページが変更されたときに発生する。</summary>
    public event EventHandler SelectedPageChanged;

    /// <summary>ナビゲーション項目と対応するページを追加する。</summary>
    /// <param name="title">項目の表示名とアクセシブル名。</param>
    /// <param name="iconAssembly">SVGアイコンを含むアセンブリ。</param>
    /// <param name="iconResourceName">SVGアイコンのマニフェストリソース名。</param>
    /// <param name="page">項目を選択したときに表示するページ。</param>
    public void AddPage(string title, Assembly iconAssembly, string iconResourceName, Control page)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(iconAssembly);
        ArgumentException.ThrowIfNullOrWhiteSpace(iconResourceName);
        ArgumentNullException.ThrowIfNull(page);
        if (page == this || ContainsPage(page))
        {
            throw new ArgumentException("ページはこのナビゲーションに一度だけ追加できます。", nameof(page));
        }

        var button = new CoaXisNavigationButton(iconAssembly, iconResourceName)
        {
            AccessibleName = title,
            Width = 46,
            Height = 46,
            Margin = new Padding(0, 0, 0, 8)
        };
        _toolTip.SetToolTip(button, title);
        page.Dock = DockStyle.Fill;
        page.Visible = false;
        _pages.Controls.Add(page);
        _rail.Controls.Add(button);
        _destinations.Add((button, page));
        button.Click += (_, _) => SelectPage(page);

        if (_destinations.Count == 1)
        {
            SelectPage(page);
        }
    }

    /// <summary>追加済みのページを選択する。</summary>
    /// <param name="page">表示するページ。</param>
    public void SelectPage(Control page)
    {
        ArgumentNullException.ThrowIfNull(page);
        if (!ContainsPage(page))
        {
            throw new ArgumentException("指定したページはこのナビゲーションに追加されていません。", nameof(page));
        }

        if (_selectedPage == page)
        {
            return;
        }

        foreach ((CoaXisNavigationButton button, Control destinationPage) in _destinations)
        {
            bool active = destinationPage == page;
            destinationPage.Visible = active;
            button.Tag = active ? "Primary" : null;
            button.AccessibleDescription = active ? "選択中" : null;
            ThemeManager.Apply(button);
        }

        _selectedPage = page;
        SelectedPageChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _toolTip.Dispose();
        }

        base.Dispose(disposing);
    }

    private bool ContainsPage(Control page)
    {
        foreach ((_, Control destinationPage) in _destinations)
        {
            if (destinationPage == page)
            {
                return true;
            }
        }

        return false;
    }
}

internal sealed class CoaXisNavigationButton : Button
{
    private readonly SvgDocument _icon;
    private Bitmap _renderedIcon;
    private Color _renderedColor;
    private int _renderedSize;
    private bool _hovered;

    public CoaXisNavigationButton(Assembly iconAssembly, string iconResourceName)
    {
        using Stream stream = iconAssembly.GetManifestResourceStream(iconResourceName)
            ?? throw new InvalidOperationException($"ナビゲーションアイコンが見つかりません: {iconResourceName}");
        _icon = SvgDocument.Open<SvgDocument>(stream)
            ?? throw new InvalidDataException($"ナビゲーションアイコンを読み込めません: {iconResourceName}");
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(Parent?.BackColor ?? SystemColors.Control);
        Theme theme = ThemeManager.CurrentTheme;
        bool selected = Tag is string tag && tag == "Primary";
        Color iconColor = selected ? theme.Primary : theme.Text;
        int iconSize = Math.Max(1, (int)Math.Round(24 * DeviceDpi / 96.0));

        if (selected || _hovered)
        {
            var rectangle = Rectangle.Inflate(ClientRectangle, -2, -2);
            if (rectangle.Width > 1 && rectangle.Height > 1)
            {
                float diameter = Math.Min(12F * DeviceDpi / 96F, Math.Min(rectangle.Width, rectangle.Height));
                using var path = new GraphicsPath();
                path.AddArc(rectangle.Left, rectangle.Top, diameter, diameter, 180, 90);
                path.AddArc(rectangle.Right - diameter, rectangle.Top, diameter, diameter, 270, 90);
                path.AddArc(rectangle.Right - diameter, rectangle.Bottom - diameter, diameter, diameter, 0, 90);
                path.AddArc(rectangle.Left, rectangle.Bottom - diameter, diameter, diameter, 90, 90);
                path.CloseFigure();
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var brush = new SolidBrush(selected ? theme.Selection : theme.HeaderHover);
                e.Graphics.FillPath(brush, path);
            }
        }

        if (_renderedIcon == null || _renderedSize != iconSize || _renderedColor != iconColor)
        {
            _icon.Stroke = new SvgColourServer(iconColor);
            Bitmap rendered = _icon.Draw(iconSize, iconSize);
            _renderedIcon?.Dispose();
            _renderedIcon = rendered;
            _renderedColor = iconColor;
            _renderedSize = iconSize;
        }

        e.Graphics.DrawImageUnscaled(_renderedIcon, (Width - iconSize) / 2, (Height - iconSize) / 2);
        if (Focused && ShowFocusCues)
        {
            ControlPaint.DrawFocusRectangle(e.Graphics, Rectangle.Inflate(ClientRectangle, -4, -4));
        }
    }

    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hovered = true; Invalidate(); }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hovered = false; Invalidate(); }
    protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
    protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _renderedIcon?.Dispose();
        }

        base.Dispose(disposing);
    }
}
