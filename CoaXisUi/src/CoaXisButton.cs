using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CoaXis.Ui;

/// <summary>CoaXis共通テーマに合わせて描画する丸角ボタン。</summary>
public sealed class CoaXisButton : Button
{
    private bool _hovered;
    private bool _pressed;

    /// <summary>ボタンを作成する。</summary>
    public CoaXisButton()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        ThemeManager.Apply(this);
    }

    /// <summary>テーマのボタン種別を指定する。</summary>
    [DefaultValue(null)]
    public new object Tag
    {
        get => base.Tag;
        set
        {
            base.Tag = value;
            ThemeManager.Apply(this);
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(Parent?.BackColor ?? SystemColors.Control);
        if (ClientSize.Width < 2 || ClientSize.Height < 2)
        {
            return;
        }

        Theme theme = ThemeManager.CurrentTheme;
        bool primary = Tag is string tag && tag == "Primary";
        Color background = !Enabled ? theme.Border
            : _pressed ? Blend(BackColor, theme.Text, 0.18)
            : _hovered ? Blend(BackColor, theme.Text, 0.10)
            : BackColor;
        Color foreground = Enabled ? ForeColor : theme.MutedText;
        float diameter = Math.Min(12F * DeviceDpi / 96F, Math.Min(ClientSize.Width - 1, ClientSize.Height - 1));
        var bounds = new RectangleF(0.5F, 0.5F, ClientSize.Width - 1F, ClientSize.Height - 1F);
        using var path = new GraphicsPath();
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var brush = new SolidBrush(background);
        e.Graphics.FillPath(brush, path);
        if (!primary && Enabled)
        {
            using var pen = new Pen(theme.Border);
            e.Graphics.DrawPath(pen, path);
        }

        TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, foreground,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        if (Focused && ShowFocusCues)
        {
            var focus = Rectangle.Inflate(ClientRectangle, -5, -5);
            if (focus.Width > 0 && focus.Height > 0)
            {
                ControlPaint.DrawFocusRectangle(e.Graphics, focus, foreground, background);
            }
        }
    }

    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hovered = true; Invalidate(); }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hovered = false; _pressed = false; Invalidate(); }
    protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); if (e.Button == MouseButtons.Left) { _pressed = true; Invalidate(); } }
    protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); _pressed = false; Invalidate(); }
    protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); Invalidate(); }
    protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
    protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); _pressed = false; Invalidate(); }
    protected override void OnKeyDown(KeyEventArgs e) { base.OnKeyDown(e); if (e.KeyCode == Keys.Space) { _pressed = true; Invalidate(); } }
    protected override void OnKeyUp(KeyEventArgs e) { base.OnKeyUp(e); if (e.KeyCode == Keys.Space) { _pressed = false; Invalidate(); } }

    private static Color Blend(Color from, Color to, double amount) => Color.FromArgb(
        (int)(from.R + (to.R - from.R) * amount),
        (int)(from.G + (to.G - from.G) * amount),
        (int)(from.B + (to.B - from.B) * amount));
}
