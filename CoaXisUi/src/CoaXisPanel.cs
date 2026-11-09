using System.ComponentModel;
using System.Windows.Forms;

namespace CoaXis.Ui;

/// <summary>CoaXisテーマのパネル。</summary>
public sealed class CoaXisPanel : Panel, ICoaXisThemedControl
{
    /// <summary>パネルを作成する。</summary>
    public CoaXisPanel() => ThemeManager.Apply(this);

    /// <inheritdoc />
    public void ApplyTheme(Theme theme)
    {
        Font = theme.StandardFont;
        ForeColor = theme.Text;
        BackColor = Tag is string tag && tag == "Background" ? theme.Background : theme.Surface;
    }

    /// <summary>テーマの背景種別を指定する。</summary>
    [DefaultValue(null)]
    public new object Tag
    {
        get => base.Tag;
        set
        {
            base.Tag = value;
            ThemeManager.Apply(this);
        }
    }
}
