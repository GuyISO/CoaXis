using System.Windows.Forms;

namespace CoaXis.Ui;

/// <summary>CoaXisテーマのテキストボックス。</summary>
public sealed class CoaXisTextBox : TextBox, ICoaXisThemedControl
{
    /// <summary>テキストボックスを作成する。</summary>
    public CoaXisTextBox() => ThemeManager.Apply(this);

    /// <inheritdoc />
    public void ApplyTheme(Theme theme)
    {
        Font = theme.StandardFont;
        BorderStyle = BorderStyle.FixedSingle;
        BackColor = theme.InputBackground;
        ForeColor = theme.Text;
        ScrollBarTheme.Apply(this, theme);
    }
}
