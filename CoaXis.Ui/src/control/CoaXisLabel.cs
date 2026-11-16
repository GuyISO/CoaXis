using System.ComponentModel;
using System.Windows.Forms;

namespace CoaXis.Ui;

/// <summary>CoaXisテーマのラベル。</summary>
public sealed class CoaXisLabel : Label, ICoaXisThemedControl
{
    private CoaXisLabelRole _role;

    /// <summary>ラベルを作成する。</summary>
    public CoaXisLabel() => ThemeManager.Apply(this);

    /// <inheritdoc />
    public void ApplyTheme(Theme theme)
    {
        Font = _role == CoaXisLabelRole.Brand ? theme.BrandFont : theme.StandardFont;
        ForeColor = _role == CoaXisLabelRole.Muted ? theme.MutedText : theme.Text;
    }

    /// <summary>ラベルの文字の役割を取得または設定する。</summary>
    [DefaultValue(CoaXisLabelRole.Standard)]
    public CoaXisLabelRole Role
    {
        get => _role;
        set
        {
            if (_role == value)
            {
                return;
            }

            _role = value;
            ThemeManager.Apply(this);
        }
    }
}
