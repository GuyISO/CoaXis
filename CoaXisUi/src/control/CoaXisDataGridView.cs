using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;

namespace CoaXis.Ui;

/// <summary>CoaXisテーマのデータグリッド。</summary>
public sealed class CoaXisDataGridView : DataGridView, ICoaXisThemedControl
{
    private readonly List<DataGridViewRow> _previewRows = new();
    private string _designerPreviewRows = string.Empty;

    /// <summary>データグリッドを作成する。</summary>
    public CoaXisDataGridView() => ThemeManager.Apply(this);

    /// <inheritdoc />
    public void ApplyTheme(Theme theme)
    {
        Font = theme.StandardFont;
        ForeColor = theme.Text;
        EnableHeadersVisualStyles = false;
        BackgroundColor = theme.Background;
        BorderStyle = BorderStyle.None;
        GridColor = theme.Border;
        CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        RowHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        DefaultCellStyle.BackColor = theme.Surface;
        DefaultCellStyle.Font = theme.StandardFont;
        DefaultCellStyle.ForeColor = theme.Text;
        DefaultCellStyle.SelectionBackColor = theme.Selection;
        DefaultCellStyle.SelectionForeColor = theme.Text;
        foreach (DataGridViewCellStyle style in new[] { ColumnHeadersDefaultCellStyle, RowHeadersDefaultCellStyle })
        {
            style.BackColor = theme.GridHeader;
            style.Font = theme.StandardFont;
            style.ForeColor = theme.Text;
            style.SelectionBackColor = theme.GridHeader;
            style.SelectionForeColor = theme.Text;
        }
        ScrollBarTheme.Apply(this, theme);
    }

    /// <summary>デザイナーで表示する行を改行区切り、セルをタブ区切りで指定する。</summary>
    [DefaultValue("")]
    public string DesignerPreviewRows
    {
        get => _designerPreviewRows;
        set
        {
            _designerPreviewRows = value ?? string.Empty;
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
        foreach (DataGridViewRow row in _previewRows)
        {
            if (row.DataGridView == this)
            {
                Rows.Remove(row);
            }
        }
        _previewRows.Clear();

        if (Site?.DesignMode != true || Columns.Count == 0)
        {
            return;
        }

        foreach (string line in _designerPreviewRows.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
        {
            string[] cells = line.Split('\t');
            if (cells.Length > Columns.Count)
            {
                throw new InvalidOperationException("プレビュー行のセル数が列数を超えています。");
            }
            int index = Rows.Add(cells);
            _previewRows.Add(Rows[index]);
        }
    }
}
