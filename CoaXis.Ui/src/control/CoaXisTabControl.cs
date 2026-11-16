using System.Drawing;
using System.Windows.Forms;

namespace CoaXis.Ui;

/// <summary>CoaXisテーマのタブコントロール。</summary>
public sealed class CoaXisTabControl : TabControl, ICoaXisThemedControl
{
    /// <summary>タブコントロールを作成する。</summary>
    public CoaXisTabControl() => ThemeManager.Apply(this);

    /// <inheritdoc />
    public void ApplyTheme(Theme theme)
    {
        Font = theme.StandardFont;
        ForeColor = theme.Text;
        BackColor = theme.Surface;
        DrawMode = TabDrawMode.OwnerDrawFixed;
        ItemSize = new Size(110, 32);
        Padding = new Point(12, 6);
        Invalidate();
    }

    /// <inheritdoc />
    protected override void OnDrawItem(DrawItemEventArgs e)
    {
        if (e.Index >= 0 && e.Index < TabPages.Count)
        {
            Theme theme = ThemeManager.CurrentTheme;
            bool selected = (e.State & DrawItemState.Selected) != 0;
            using var brush = new SolidBrush(selected ? theme.Primary : theme.SecondaryButton);
            e.Graphics.FillRectangle(brush, e.Bounds);
            TextRenderer.DrawText(e.Graphics, TabPages[e.Index].Text, Font, e.Bounds, Color.White,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
        base.OnDrawItem(e);
    }
}
