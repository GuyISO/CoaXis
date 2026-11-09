using System.Windows.Forms;

namespace CoaXis.Ui;

/// <summary>CoaXisテーマのステータスバー。</summary>
public sealed class CoaXisStatusStrip : StatusStrip, ICoaXisThemedControl
{
    /// <summary>ステータスバーを作成する。</summary>
    public CoaXisStatusStrip() => ThemeManager.Apply(this);

    /// <inheritdoc />
    public void ApplyTheme(Theme theme)
    {
        Font = theme.StandardFont;
        BackColor = theme.Surface;
        ForeColor = theme.MutedText;
        SizingGrip = false;
        foreach (ToolStripItem item in Items)
        {
            ApplyItem(item, theme);
        }
    }

    /// <inheritdoc />
    protected override void OnItemAdded(ToolStripItemEventArgs e)
    {
        base.OnItemAdded(e);
        ApplyItem(e.Item, ThemeManager.CurrentTheme);
    }

    private static void ApplyItem(ToolStripItem item, Theme theme)
    {
        item.Font = theme.StandardFont;
        item.ForeColor = theme.MutedText;
        item.BackColor = theme.Surface;
    }
}
