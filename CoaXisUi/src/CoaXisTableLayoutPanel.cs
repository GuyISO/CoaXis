using System.ComponentModel;
using System.Windows.Forms;

namespace CoaXis.Ui;

/// <summary>CoaXisテーマの表形式レイアウトパネル。</summary>
public sealed class CoaXisTableLayoutPanel : TableLayoutPanel
{
    /// <summary>表形式レイアウトパネルを作成する。</summary>
    public CoaXisTableLayoutPanel() => ThemeManager.Apply(this);

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
