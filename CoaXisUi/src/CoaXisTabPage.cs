using System.Windows.Forms;

namespace CoaXis.Ui;

/// <summary>CoaXisテーマのタブページ。</summary>
public sealed class CoaXisTabPage : TabPage
{
    /// <summary>タブページを作成する。</summary>
    public CoaXisTabPage() => ThemeManager.Apply(this);
}
