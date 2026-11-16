using System.Windows.Forms;

namespace CoaXis.Ui;

/// <summary>CoaXisテーマのリストボックス。</summary>
public sealed class CoaXisListBox : ListBox, ICoaXisThemedControl
{
    /// <summary>リストボックスを作成する。</summary>
    public CoaXisListBox() => ThemeManager.Apply(this);

    /// <inheritdoc />
    public void ApplyTheme(Theme theme)
    {
        Font = theme.StandardFont;
        BorderStyle = BorderStyle.None;
        BackColor = theme.DisplayBackground;
        ForeColor = theme.Text;
        ScrollBarTheme.Apply(this, theme);
    }
}
