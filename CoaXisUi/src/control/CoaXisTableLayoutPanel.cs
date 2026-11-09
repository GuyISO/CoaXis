using System.ComponentModel;
using System.Windows.Forms;

namespace CoaXis.Ui;

/// <summary>CoaXisテーマの表形式レイアウトパネル。</summary>
public sealed class CoaXisTableLayoutPanel : TableLayoutPanel, ICoaXisThemedControl
{
    /// <summary>表形式レイアウトパネルを作成する。</summary>
    public CoaXisTableLayoutPanel() => ThemeManager.Apply(this);

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
