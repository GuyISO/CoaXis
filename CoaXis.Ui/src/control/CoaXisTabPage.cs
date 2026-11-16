using System.ComponentModel;
using System.Windows.Forms;

namespace CoaXis.Ui;

/// <summary>CoaXisテーマのタブページ。</summary>
public sealed class CoaXisTabPage : TabPage, ICoaXisThemedControl
{
    private CoaXisSurfaceRole _surfaceRole;

    /// <summary>タブページを作成する。</summary>
    public CoaXisTabPage() => ThemeManager.Apply(this);

    /// <inheritdoc />
    public void ApplyTheme(Theme theme)
    {
        Font = theme.StandardFont;
        ForeColor = theme.Text;
        BackColor = _surfaceRole == CoaXisSurfaceRole.Background ? theme.Background : theme.Surface;
    }

    /// <summary>タブページの背景の役割を取得または設定する。</summary>
    [DefaultValue(CoaXisSurfaceRole.Surface)]
    public CoaXisSurfaceRole SurfaceRole
    {
        get => _surfaceRole;
        set
        {
            if (_surfaceRole == value)
            {
                return;
            }

            _surfaceRole = value;
            ThemeManager.Apply(this);
        }
    }
}
