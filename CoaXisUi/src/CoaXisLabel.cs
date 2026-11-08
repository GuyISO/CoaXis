using System.ComponentModel;
using System.Windows.Forms;

namespace CoaXis.Ui;

/// <summary>CoaXisテーマのラベル。</summary>
public sealed class CoaXisLabel : Label
{
    /// <summary>ラベルを作成する。</summary>
    public CoaXisLabel() => ThemeManager.Apply(this);

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
