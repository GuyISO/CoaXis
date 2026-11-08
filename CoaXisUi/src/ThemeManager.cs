using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CoaXis.Ui;

/// <summary>
/// CoaXis UI の配色を定義する。
/// </summary>
public abstract class Theme
{
    /// <summary>アプリケーションの背景色を取得する。</summary>
    public abstract Color Background { get; }

    /// <summary>カードや入力欄の背景色を取得する。</summary>
    public abstract Color Surface { get; }

    /// <summary>境界線の色を取得する。</summary>
    public abstract Color Border { get; }

    /// <summary>通常の文字色を取得する。</summary>
    public abstract Color Text { get; }

    /// <summary>操作のアクセントカラーを取得する。</summary>
    public abstract Color Primary { get; }

    /// <summary>補助情報の文字色を取得する。</summary>
    public abstract Color MutedText { get; }

    /// <summary>選択項目の背景色を取得する。</summary>
    public abstract Color Selection { get; }

    /// <summary>選択されていないグリッドヘッダーの背景色を取得する。</summary>
    public abstract Color GridHeader { get; }

    /// <summary>タイトルバーの背景色を取得する。</summary>
    public abstract Color Header { get; }

    /// <summary>タイトルバーのボタンに重ねるホバー色を取得する。</summary>
    public abstract Color HeaderHover { get; }
}

/// <summary>
/// 明るい配色を表す。
/// </summary>
public sealed class LightTheme : Theme
{
    /// <inheritdoc />
    public override Color Background => ColorTranslator.FromHtml("#FFFFFF");

    /// <inheritdoc />
    public override Color Surface => ColorTranslator.FromHtml("#F5F5F5");

    /// <inheritdoc />
    public override Color Border => ColorTranslator.FromHtml("#D0D0D0");

    /// <inheritdoc />
    public override Color Text => ColorTranslator.FromHtml("#202020");

    /// <inheritdoc />
    public override Color Primary => ColorTranslator.FromHtml("#00B7FF");

    /// <inheritdoc />
    public override Color MutedText => ColorTranslator.FromHtml("#606060");

    /// <inheritdoc />
    public override Color Selection => ColorTranslator.FromHtml("#CDE8FF");

    /// <inheritdoc />
    public override Color GridHeader => ColorTranslator.FromHtml("#E8E8E8");

    /// <inheritdoc />
    public override Color Header => ColorTranslator.FromHtml("#F8F8F8");

    /// <inheritdoc />
    public override Color HeaderHover => ColorTranslator.FromHtml("#E5E5E5");
}

/// <summary>
/// ダーク配色を表す。
/// </summary>
public sealed class DarkTheme : Theme
{
    /// <inheritdoc />
    public override Color Background => ColorTranslator.FromHtml("#1E1E1E");

    /// <inheritdoc />
    public override Color Surface => ColorTranslator.FromHtml("#2D2D30");

    /// <inheritdoc />
    public override Color Border => ColorTranslator.FromHtml("#404040");

    /// <inheritdoc />
    public override Color Text => ColorTranslator.FromHtml("#FFFFFF");

    /// <inheritdoc />
    public override Color Primary => ColorTranslator.FromHtml("#00B7FF");

    /// <inheritdoc />
    public override Color MutedText => ColorTranslator.FromHtml("#B0B0B0");

    /// <inheritdoc />
    public override Color Selection => ColorTranslator.FromHtml("#164A69");

    /// <inheritdoc />
    public override Color GridHeader => ColorTranslator.FromHtml("#252526");

    /// <inheritdoc />
    public override Color Header => ColorTranslator.FromHtml("#202124");

    /// <inheritdoc />
    public override Color HeaderHover => ColorTranslator.FromHtml("#3A3B3E");
}

/// <summary>
/// フォームとその子コントロールへ共通テーマを適用する。
/// </summary>
public static class ThemeManager
{
    private const string DarkScrollBarTheme = "DarkMode_Explorer";
    private static Theme _currentTheme = new DarkTheme();

    /// <summary>通常のコントロールで使用する標準フォントを取得する。</summary>
    public static Font StandardFont { get; } = CreateFont("Segoe UI Variable Text", "Segoe UI", 9F, FontStyle.Regular);

    /// <summary>ボタンの文字に使用するフォントを取得する。</summary>
    public static Font ButtonFont { get; } = CreateFont("Segoe UI Variable Text Semibold", "Segoe UI Semibold", 9F, FontStyle.Regular);

    /// <summary>ブランド見出しで使用するフォントを取得する。</summary>
    public static Font BrandFont { get; } = CreateFont("Segoe UI Variable Display", "Segoe UI", 16F, FontStyle.Bold);

    internal static Font TitleFont { get; } = CreateFont("Segoe UI Variable Text Semibold", "Segoe UI Semibold", 15F, FontStyle.Regular);

    /// <summary>現在のテーマを取得する。</summary>
    public static Theme CurrentTheme => _currentTheme;

    /// <summary>以降のテーマ適用で使用するテーマを設定する。</summary>
    /// <param name="theme">適用するテーマ。</param>
    public static void SetTheme(Theme theme)
    {
        _currentTheme = theme ?? throw new ArgumentNullException(nameof(theme));
    }

    /// <summary>指定コントロールとその子孫へ現在のテーマを適用する。</summary>
    /// <param name="root">テーマ適用の起点となるコントロール。</param>
    public static void Apply(Control root)
    {
        ArgumentNullException.ThrowIfNull(root);
        ApplyControl(root);
    }

    private static Font CreateFont(string preferredFamily, string fallbackFamily, float size, FontStyle style)
    {
        var font = new Font(preferredFamily, size, style);
        if (string.Equals(font.FontFamily.Name, preferredFamily, StringComparison.OrdinalIgnoreCase))
        {
            return font;
        }

        font.Dispose();
        Trace.TraceWarning("フォント '{0}' が使用できないため '{1}' を使用します。", preferredFamily, fallbackFamily);
        return new Font(fallbackFamily, size, style);
    }

    private static void ApplyControl(Control control)
    {
        control.Font = control.Tag is string tag && tag == "Brand" ? BrandFont : StandardFont;
        control.ForeColor = _currentTheme.Text;

        if (control is Form || control.Tag is string backgroundTag && backgroundTag == "Background")
        {
            control.BackColor = _currentTheme.Background;
        }
        else if (control is Panel or TableLayoutPanel or FlowLayoutPanel or SplitContainer or TabControl or TabPage)
        {
            control.BackColor = _currentTheme.Surface;
        }

        switch (control)
        {
            case Button button:
                ApplyButton(button);
                break;
            case TextBox textBox:
                textBox.BorderStyle = BorderStyle.FixedSingle;
                textBox.BackColor = _currentTheme.Surface;
                textBox.ForeColor = _currentTheme.Text;
                break;
            case ComboBox comboBox:
                comboBox.FlatStyle = FlatStyle.Flat;
                comboBox.BackColor = _currentTheme.Surface;
                comboBox.ForeColor = _currentTheme.Text;
                break;
            case ListBox listBox:
                listBox.BackColor = _currentTheme.Background;
                listBox.ForeColor = _currentTheme.Text;
                break;
            case DataGridView grid:
                ApplyGrid(grid);
                break;
            case TreeView tree:
                tree.BorderStyle = BorderStyle.FixedSingle;
                tree.BackColor = _currentTheme.Surface;
                tree.ForeColor = _currentTheme.Text;
                tree.LineColor = _currentTheme.Border;
                break;
            case CheckBox checkBox:
                checkBox.FlatStyle = FlatStyle.Flat;
                break;
            case Splitter splitter:
                splitter.BackColor = _currentTheme.Border;
                break;
            case TabControl tabControl:
                ApplyTabs(tabControl);
                break;
            case StatusStrip statusStrip:
                statusStrip.BackColor = _currentTheme.Surface;
                statusStrip.ForeColor = _currentTheme.MutedText;
                statusStrip.SizingGrip = false;
                foreach (ToolStripItem item in statusStrip.Items)
                {
                    ApplyStatusItem(item);
                }
                break;
            case Label label when label.Tag is string labelTag && labelTag == "Muted":
                label.ForeColor = _currentTheme.MutedText;
                break;
        }

        if (HasScrollBars(control))
        {
            control.HandleCreated -= ApplyScrollBarThemeOnHandleCreated;
            control.HandleCreated += ApplyScrollBarThemeOnHandleCreated;
            if (control.IsHandleCreated)
            {
                ApplyScrollBarTheme(control);
            }
        }

        foreach (Control child in control.Controls)
        {
            ApplyControl(child);
        }

        if (control is CoaXisHeaderControl header)
        {
            header.ApplyTheme(_currentTheme);
        }
    }

    internal static void ApplyStatusItem(ToolStripItem item)
    {
        item.Font = StandardFont;
        item.ForeColor = _currentTheme.MutedText;
        item.BackColor = _currentTheme.Surface;
    }

    private static bool HasScrollBars(Control control)
    {
        return control is ListBox or DataGridView or TreeView ||
            control is TextBox textBox && textBox.ScrollBars != ScrollBars.None;
    }

    private static void ApplyScrollBarThemeOnHandleCreated(object sender, EventArgs e)
    {
        if (sender is Control control)
        {
            ApplyScrollBarTheme(control);
        }
    }

    private static void ApplyScrollBarTheme(Control control)
    {
        string theme = _currentTheme is DarkTheme ? DarkScrollBarTheme : null;
        int result = SetWindowTheme(control.Handle, theme, null);
        if (result < 0)
        {
            Trace.TraceWarning("スクロールバーのテーマ適用に失敗しました: 0x{0:X8}", result);
        }
    }

    private static void ApplyButton(Button button)
    {
        bool isPrimary = button.Tag is string tag && tag == "Primary";
        bool isActiveTheme =
            button.Tag is string themeTag &&
            ((themeTag == "ThemeDark" && _currentTheme is DarkTheme) ||
             (themeTag == "ThemeLight" && _currentTheme is LightTheme));

        button.Font = ButtonFont;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.UseVisualStyleBackColor = false;
        button.BackColor = isPrimary || isActiveTheme ? _currentTheme.Primary : _currentTheme.Surface;
        button.ForeColor = isPrimary || isActiveTheme
            ? Color.FromArgb(30, 30, 30)
            : _currentTheme.Text;
        button.Cursor = Cursors.Hand;
        button.Padding = new Padding(10, 4, 10, 4);
    }

    private static void ApplyGrid(DataGridView grid)
    {
        grid.EnableHeadersVisualStyles = false;
        grid.BackgroundColor = _currentTheme.Background;
        grid.BorderStyle = BorderStyle.None;
        grid.GridColor = _currentTheme.Border;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        grid.RowHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        grid.DefaultCellStyle.BackColor = _currentTheme.Surface;
        grid.DefaultCellStyle.Font = StandardFont;
        grid.DefaultCellStyle.ForeColor = _currentTheme.Text;
        grid.DefaultCellStyle.SelectionBackColor = _currentTheme.Selection;
        grid.DefaultCellStyle.SelectionForeColor = _currentTheme.Text;
        grid.ColumnHeadersDefaultCellStyle.BackColor = _currentTheme.GridHeader;
        grid.ColumnHeadersDefaultCellStyle.Font = StandardFont;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = _currentTheme.Text;
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = _currentTheme.GridHeader;
        grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = _currentTheme.Text;
        grid.RowHeadersDefaultCellStyle.BackColor = _currentTheme.GridHeader;
        grid.RowHeadersDefaultCellStyle.Font = StandardFont;
        grid.RowHeadersDefaultCellStyle.ForeColor = _currentTheme.Text;
        grid.RowHeadersDefaultCellStyle.SelectionBackColor = _currentTheme.GridHeader;
        grid.RowHeadersDefaultCellStyle.SelectionForeColor = _currentTheme.Text;
    }

    private static void ApplyTabs(TabControl tabControl)
    {
        tabControl.DrawMode = TabDrawMode.OwnerDrawFixed;
        tabControl.ItemSize = new Size(110, 32);
        tabControl.Padding = new Point(12, 6);
        tabControl.DrawItem -= DrawTab;
        tabControl.DrawItem += DrawTab;
    }

    private static void DrawTab(object sender, DrawItemEventArgs args)
    {
        if (sender is not TabControl tabControl || args.Index < 0)
        {
            return;
        }

        bool selected = (args.State & DrawItemState.Selected) == DrawItemState.Selected;
        Color background = selected ? _currentTheme.Primary : _currentTheme.Surface;
        Color foreground = selected
            ? Color.FromArgb(30, 30, 30)
            : _currentTheme.Text;

        using var brush = new SolidBrush(background);
        args.Graphics.FillRectangle(brush, args.Bounds);
        TextRenderer.DrawText(
            args.Graphics,
            tabControl.TabPages[args.Index].Text,
            StandardFont,
            args.Bounds,
            foreground,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }

    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern int SetWindowTheme(IntPtr window, string subAppName, string subIdList);
}
