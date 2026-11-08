using System.Windows.Forms;

namespace CoaXis.Ui;

/// <summary>CoaXisテーマのチェックボックス。</summary>
public sealed class CoaXisCheckBox : CheckBox
{
    /// <summary>チェックボックスを作成する。</summary>
    public CoaXisCheckBox() => ThemeManager.Apply(this);
}
