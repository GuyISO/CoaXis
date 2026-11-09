using System.Windows.Forms;

namespace CoaXis.Ui;

/// <summary>CoaXisテーマのタブページ。</summary>
public sealed class CoaXisTabPage : TabPage, ICoaXisThemedControl
{
    /// <summary>タブページを作成する。</summary>
    public CoaXisTabPage() => ThemeManager.Apply(this);

    /// <inheritdoc />
    public void ApplyTheme(Theme theme)
    {
        Font = theme.StandardFont;
        ForeColor = theme.Text;
        BackColor = Tag is string tag && tag == "Background" ? theme.Background : theme.Surface;
    }
}
