using System.Windows.Forms;

namespace CoaXis.Ui;

/// <summary>表示専用のCoaXisテーマのテキストボックス。</summary>
public sealed class CoaXisDisplayTextBox : TextBox, ICoaXisThemedControl
{
    /// <summary>表示専用のテキストボックスを作成する。</summary>
    public CoaXisDisplayTextBox()
    {
        ReadOnly = true;
        ThemeManager.Apply(this);
    }

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
