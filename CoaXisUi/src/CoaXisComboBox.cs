using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace CoaXis.Ui;

/// <summary>CoaXisテーマのコンボボックス。</summary>
public sealed class CoaXisComboBox : ComboBox
{
    private string _designerPreviewItems = string.Empty;
    private string[] _previewItems = Array.Empty<string>();

    /// <summary>コンボボックスを作成する。</summary>
    public CoaXisComboBox() => ThemeManager.Apply(this);

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
