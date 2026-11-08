using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace CoaXis.Ui;

/// <summary>CoaXisテーマのツリービュー。</summary>
public sealed class CoaXisTreeView : TreeView
{
    private bool _designerExpandAll;

    /// <summary>ツリービューを作成する。</summary>
    public CoaXisTreeView() => ThemeManager.Apply(this);

    /// <summary>デザイナーでツリーを展開表示するかを指定する。</summary>
    [DefaultValue(false)]
    public bool DesignerExpandAll
    {
        get => _designerExpandAll;
        set
        {
            _designerExpandAll = value;
            UpdateDesignerPreview();
        }
    }

    /// <inheritdoc />
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        UpdateDesignerPreview();
    }

    private void UpdateDesignerPreview()
    {
        if (Site?.DesignMode == true && _designerExpandAll)
        {
            ExpandAll();
        }
    }
}
