using System.Windows.Forms;

namespace CoaXis.Ui;

/// <summary>CoaXisテーマのステータスバー。</summary>
public sealed class CoaXisStatusStrip : StatusStrip
{
    /// <summary>ステータスバーを作成する。</summary>
    public CoaXisStatusStrip() => ThemeManager.Apply(this);

    /// <inheritdoc />
    protected override void OnItemAdded(ToolStripItemEventArgs e)
    {
        base.OnItemAdded(e);
        ThemeManager.ApplyStatusItem(e.Item);
    }
}
