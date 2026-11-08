using System.ComponentModel;
using System.Windows.Forms;

namespace CoaXis.Ui;

/// <summary>CoaXisテーマのフローレイアウトパネル。</summary>
public sealed class CoaXisFlowLayoutPanel : FlowLayoutPanel
{
    /// <summary>フローレイアウトパネルを作成する。</summary>
    public CoaXisFlowLayoutPanel() => ThemeManager.Apply(this);

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
