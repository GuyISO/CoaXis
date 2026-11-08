using System.ComponentModel;
using System.Windows.Forms;

namespace CoaXis.Ui;

/// <summary>CoaXisテーマのパネル。</summary>
public sealed class CoaXisPanel : Panel
{
    /// <summary>パネルを作成する。</summary>
    public CoaXisPanel() => ThemeManager.Apply(this);

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
