using System.ComponentModel;
using System.Windows.Forms;

namespace CoaXis.Ui;

/// <summary>CoaXisテーマのラベル。</summary>
public sealed class CoaXisLabel : Label, ICoaXisThemedControl
{
    /// <summary>ラベルを作成する。</summary>
    public CoaXisLabel() => ThemeManager.Apply(this);

    /// <inheritdoc />
    public void ApplyTheme(Theme theme)
    {
        Font = Tag is string tag && tag == "Brand" ? theme.BrandFont : theme.StandardFont;
        ForeColor = Tag is string labelTag && labelTag == "Muted" ? theme.MutedText : theme.Text;
    }

    /// <summary>テーマの文字種別を指定する。</summary>
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
