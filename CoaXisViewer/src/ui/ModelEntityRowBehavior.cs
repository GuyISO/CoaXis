using Godot;
using System;

/// <summary>
/// UI上の ModelEntity 行に共通する表示と操作を提供する。
/// </summary>
internal static class ModelEntityRowBehavior
{
    /// <summary>
    /// 現在の ModelEntity 選択ハイライト色を取得する。
    /// </summary>
    public static Color GetSelectionColor()
    {
        return Color.FromHtml(Application.Setting.Current.Color.HierarchySelectedColor);
    }

    /// <summary>
    /// Tree上の ModelEntity 行へ選択状態に応じた背景色を適用する。
    /// </summary>
    /// <param name="item">背景色を適用する TreeItem</param>
    /// <param name="entityId">行が表す ModelEntity の識別子</param>
    /// <param name="selectionColor">選択時に適用する背景色</param>
    public static void ApplySelectionHighlight(TreeItem item, Guid entityId, Color selectionColor)
    {
        if (item == null)
        {
            return;
        }

        item.SetCustomBgColor(0, Application.Model.Entity.Selection.Contains(entityId)
            ? selectionColor
            : Colors.Transparent);
    }

    /// <summary>
    /// ItemList上の ModelEntity 行へ選択状態に応じた背景色を適用する。
    /// </summary>
    /// <param name="list">背景色を適用する ItemList</param>
    /// <param name="index">対象行のインデックス</param>
    /// <param name="entityId">行が表す ModelEntity の識別子</param>
    /// <param name="selectionColor">選択時に適用する背景色</param>
    public static void ApplySelectionHighlight(ItemList list, int index, Guid entityId, Color selectionColor)
    {
        list.SetItemCustomBgColor(index, Application.Model.Entity.Selection.Contains(entityId)
            ? selectionColor
            : Colors.Transparent);
    }

    /// <summary>
    /// 指定した ModelEntity のコンテキストメニューを表示する。
    /// </summary>
    /// <param name="entityId">メニュー対象の ModelEntity 識別子</param>
    public static void ShowContextMenu(Guid entityId)
    {
        Application.Ui.Menu.ShowModelEntityMenu(entityId);
    }

    /// <summary>
    /// 指定した ModelEntity に対する Manipulate 操作を通知する。
    /// </summary>
    /// <param name="entityId">操作対象の ModelEntity 識別子</param>
    public static void Manipulate(Guid entityId)
    {
        Application.Model.Entity.Manipulate(entityId);
    }
}
