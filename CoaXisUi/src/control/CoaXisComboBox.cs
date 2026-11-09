using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace CoaXis.Ui;

/// <summary>CoaXisテーマのコンボボックス。</summary>
public sealed class CoaXisComboBox : ComboBox, ICoaXisThemedControl
{
    private string _designerPreviewItems = string.Empty;
    private string[] _previewItems = Array.Empty<string>();

    /// <summary>コンボボックスを作成する。</summary>
    public CoaXisComboBox() => ThemeManager.Apply(this);

    /// <inheritdoc />
    public void ApplyTheme(Theme theme)
    {
        Font = theme.StandardFont;
        FlatStyle = FlatStyle.Flat;
        DrawMode = DrawMode.OwnerDrawFixed;
        UpdateItemHeight();
        BackColor = theme.InputBackground;
        ForeColor = theme.Text;
        Invalidate();
    }

    /// <inheritdoc />
    protected override void OnDpiChangedAfterParent(EventArgs e)
    {
        base.OnDpiChangedAfterParent(e);
        UpdateItemHeight();
    }

    /// <inheritdoc />
    protected override void OnDrawItem(DrawItemEventArgs e)
    {
        Theme theme = ThemeManager.CurrentTheme;
        bool selected = e.Index >= 0 && (e.State & DrawItemState.Selected) != 0
            && (e.State & DrawItemState.ComboBoxEdit) == 0;
        using var brush = new SolidBrush(selected ? theme.Primary : theme.InputBackground);
        e.Graphics.FillRectangle(brush, e.Bounds);
        if (e.Index >= 0 && e.Index < Items.Count)
        {
            TextRenderer.DrawText(e.Graphics, GetItemText(Items[e.Index]), Font,
                Rectangle.Inflate(e.Bounds, -10, 0), selected ? Color.White : theme.Text,
                TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }
        base.OnDrawItem(e);
    }

    /// <inheritdoc />
    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams parameters = base.CreateParams;
            parameters.Style &= ~0x00800000;
            parameters.ExStyle &= ~0x00000200;
            return parameters;
        }
    }

    /// <summary>デザイナーで表示する項目を改行区切りで指定する。</summary>
    [DefaultValue("")]
    public string DesignerPreviewItems
    {
        get => _designerPreviewItems;
        set
        {
            _designerPreviewItems = value ?? string.Empty;
            UpdateDesignerPreview();
        }
    }

    /// <inheritdoc />
    public override ISite Site
    {
        get => base.Site;
        set
        {
            base.Site = value;
            UpdateDesignerPreview();
        }
    }

    private void UpdateItemHeight()
    {
        ItemHeight = Math.Max(Font.Height, (int)Math.Round(20 * DeviceDpi / 96.0));
    }

    private void UpdateDesignerPreview()
    {
        foreach (string item in _previewItems)
        {
            Items.Remove(item);
        }
        _previewItems = Array.Empty<string>();
        if (Site?.DesignMode != true)
        {
            return;
        }

        _previewItems = _designerPreviewItems.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        Items.AddRange(_previewItems);
        if (_previewItems.Length > 0)
        {
            SelectedItem = _previewItems[0];
        }
    }
}
