using System.ComponentModel;
using System.Windows.Forms;

namespace CoaXis.Ui;

/// <summary>CoaXisテーマのフローレイアウトパネル。</summary>
public sealed class CoaXisFlowLayoutPanel : FlowLayoutPanel, ICoaXisThemedControl
{
    private CoaXisSurfaceRole _surfaceRole;

    /// <summary>フローレイアウトパネルを作成する。</summary>
    public CoaXisFlowLayoutPanel() => ThemeManager.Apply(this);

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
