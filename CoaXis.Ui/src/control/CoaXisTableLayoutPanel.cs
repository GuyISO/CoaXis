using System.ComponentModel;
using System.Windows.Forms;

namespace CoaXis.Ui;

/// <summary>CoaXisテーマの表形式レイアウトパネル。</summary>
public sealed class CoaXisTableLayoutPanel : TableLayoutPanel, ICoaXisThemedControl
{
    private CoaXisSurfaceRole _surfaceRole;

    /// <summary>表形式レイアウトパネルを作成する。</summary>
    public CoaXisTableLayoutPanel() => ThemeManager.Apply(this);

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
