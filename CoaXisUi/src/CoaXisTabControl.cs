using System.Windows.Forms;

namespace CoaXis.Ui;

/// <summary>CoaXisテーマのタブコントロール。</summary>
public sealed class CoaXisTabControl : TabControl
{
    /// <summary>タブコントロールを作成する。</summary>
    public CoaXisTabControl() => ThemeManager.Apply(this);
}
