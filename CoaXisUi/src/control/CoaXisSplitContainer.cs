using System.Windows.Forms;

namespace CoaXis.Ui;

/// <summary>CoaXisテーマの分割コンテナー。</summary>
public sealed class CoaXisSplitContainer : SplitContainer, ICoaXisThemedControl
{
    /// <summary>分割コンテナーを作成する。</summary>
    public CoaXisSplitContainer() => ThemeManager.Apply(this);

    /// <inheritdoc />
    public void ApplyTheme(Theme theme)
    {
        Font = theme.StandardFont;
        ForeColor = theme.Text;
        BackColor = theme.Surface;
    }
}
