using System.Windows.Forms;

namespace CoaXis.Ui;

/// <summary>CoaXisテーマのリストボックス。</summary>
public sealed class CoaXisListBox : ListBox
{
    /// <summary>リストボックスを作成する。</summary>
    public CoaXisListBox() => ThemeManager.Apply(this);
}
