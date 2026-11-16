using System.ComponentModel;
using System.Windows.Forms;

namespace CoaXis.Ui;

/// <summary>CoaXisテーマのパネル。</summary>
public sealed class CoaXisPanel : Panel, ICoaXisThemedControl
{
    private CoaXisSurfaceRole _surfaceRole;

    /// <summary>パネルを作成する。</summary>
    public CoaXisPanel() => ThemeManager.Apply(this);

    /// <inheritdoc />
    public void ApplyTheme(Theme theme)
    {
        Font = theme.StandardFont;
        ForeColor = theme.Text;
        BackColor = _surfaceRole == CoaXisSurfaceRole.Background ? theme.Background : theme.Surface;
    }

    /// <summary>パネルの背景の役割を取得または設定する。</summary>
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
