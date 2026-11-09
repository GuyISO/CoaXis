using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CoaXis.Ui;

/// <summary>CoaXisテーマのチェックボックス。</summary>
public sealed class CoaXisCheckBox : CheckBox, ICoaXisThemedControl
{
    /// <summary>チェックボックスを作成する。</summary>
    public CoaXisCheckBox()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        CheckStateChanged += (_, _) => Invalidate();
        ThemeManager.Apply(this);
    }

    /// <inheritdoc />
    public void ApplyTheme(Theme theme)
    {
        Font = theme.StandardFont;
        ForeColor = theme.Text;
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        BackColor = Color.Transparent;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Theme theme = ThemeManager.CurrentTheme;
        e.Graphics.Clear(Parent?.BackColor ?? theme.Surface);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        int size = Math.Min((int)Math.Round(20 * DeviceDpi / 96.0), Math.Max(1, ClientSize.Height - 4));
        Rectangle checkBounds = GetCheckBounds(size);
        bool checkedState = CheckState != CheckState.Unchecked;
        Color checkFill = !Enabled ? theme.Border : checkedState ? theme.Primary
            : _hovered ? theme.CheckBoxHoverBackground : theme.InputBackground;
        Color checkBorder = checkedState ? theme.Primary : _hovered ? theme.Primary : theme.Border;
        using var path = CreateCheckPath(checkBounds);
        using (var fill = new SolidBrush(checkFill))
        {
            e.Graphics.FillPath(fill, path);
        }
        using (var border = new Pen(checkBorder, Math.Max(1F, DeviceDpi / 96F)))
        {
            e.Graphics.DrawPath(border, path);
        }

        if (checkedState)
        {
            using var mark = new Pen(Enabled ? Color.White : theme.MutedText, Math.Max(1.8F, 2.2F * DeviceDpi / 96F))
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            };
            if (CheckState == CheckState.Indeterminate)
            {
                e.Graphics.DrawLine(mark, checkBounds.Left + size * 0.27F, checkBounds.Top + size * 0.5F,
                    checkBounds.Right - size * 0.27F, checkBounds.Top + size * 0.5F);
            }
            else
            {
                e.Graphics.DrawLines(mark, new[]
                {
                    new PointF(checkBounds.Left + size * 0.25F, checkBounds.Top + size * 0.52F),
                    new PointF(checkBounds.Left + size * 0.43F, checkBounds.Top + size * 0.69F),
                    new PointF(checkBounds.Left + size * 0.76F, checkBounds.Top + size * 0.33F)
                });
            }
        }

        Rectangle textBounds = GetTextBounds(checkBounds);
        Color textColor = Enabled ? ForeColor : theme.MutedText;
        TextRenderer.DrawText(e.Graphics, Text, Font, textBounds, textColor, GetTextFlags(TextAlign));
        if (Focused && ShowFocusCues)
        {
            Rectangle focusBounds = Rectangle.Inflate(textBounds, -1, -2);
            if (focusBounds.Width > 0 && focusBounds.Height > 0)
            {
                ControlPaint.DrawFocusRectangle(e.Graphics, focusBounds, textColor, Parent?.BackColor ?? theme.Surface);
            }
        }
    }

    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hovered = true; Invalidate(); }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hovered = false; Invalidate(); }
    protected override void OnCheckedChanged(EventArgs e) { base.OnCheckedChanged(e); Invalidate(); }
    protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); Invalidate(); }
    protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
    protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }

    private bool _hovered;

    private Rectangle GetCheckBounds(int size)
    {
        int x = CheckAlign switch
        {
            ContentAlignment.TopRight or ContentAlignment.MiddleRight or ContentAlignment.BottomRight => ClientSize.Width - size - 2,
            ContentAlignment.TopCenter or ContentAlignment.MiddleCenter or ContentAlignment.BottomCenter => (ClientSize.Width - size) / 2,
            _ => 2
        };
        int y = CheckAlign switch
        {
            ContentAlignment.TopLeft or ContentAlignment.TopCenter or ContentAlignment.TopRight => 2,
            ContentAlignment.BottomLeft or ContentAlignment.BottomCenter or ContentAlignment.BottomRight => ClientSize.Height - size - 2,
            _ => (ClientSize.Height - size) / 2
        };
        return new Rectangle(x, y, size, size);
    }

    private Rectangle GetTextBounds(Rectangle checkBounds)
    {
        Rectangle bounds = ClientRectangle;
        if (checkBounds.Left <= 2)
        {
            bounds.X = checkBounds.Right + 8;
            bounds.Width = Math.Max(0, ClientSize.Width - bounds.X);
        }
        else if (checkBounds.Right >= ClientSize.Width - 2)
        {
            bounds.Width = Math.Max(0, checkBounds.Left - 8);
        }
        else
        {
            bounds.Y = checkBounds.Bottom + 2;
            bounds.Height = Math.Max(0, ClientSize.Height - bounds.Y);
        }

        return bounds;
    }

    private static GraphicsPath CreateCheckPath(Rectangle bounds)
    {
        var path = new GraphicsPath();
        float radius = Math.Min(4F, bounds.Width / 4F);
        path.AddArc(bounds.Left, bounds.Top, radius * 2, radius * 2, 180, 90);
        path.AddArc(bounds.Right - radius * 2, bounds.Top, radius * 2, radius * 2, 270, 90);
        path.AddArc(bounds.Right - radius * 2, bounds.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
        path.CloseFigure();
        return path;
    }

    private static TextFormatFlags GetTextFlags(ContentAlignment alignment)
    {
        TextFormatFlags horizontal = alignment switch
        {
            ContentAlignment.TopCenter or ContentAlignment.MiddleCenter or ContentAlignment.BottomCenter => TextFormatFlags.HorizontalCenter,
            ContentAlignment.TopRight or ContentAlignment.MiddleRight or ContentAlignment.BottomRight => TextFormatFlags.Right,
            _ => TextFormatFlags.Left
        };
        TextFormatFlags vertical = alignment switch
        {
            ContentAlignment.TopLeft or ContentAlignment.TopCenter or ContentAlignment.TopRight => TextFormatFlags.Top,
            ContentAlignment.BottomLeft or ContentAlignment.BottomCenter or ContentAlignment.BottomRight => TextFormatFlags.Bottom,
            _ => TextFormatFlags.VerticalCenter
        };
        return horizontal | vertical | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix;
    }
}
