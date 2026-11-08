using System.Windows.Forms;

namespace CoaXis.Ui;

/// <summary>CoaXisテーマのテキストボックス。</summary>
public sealed class CoaXisTextBox : TextBox
{
    /// <summary>テキストボックスを作成する。</summary>
    public CoaXisTextBox() => ThemeManager.Apply(this);
}
