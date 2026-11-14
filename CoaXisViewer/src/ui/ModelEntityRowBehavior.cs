using Godot;
using System;
using System.Collections.Generic;
using CoaXis.Protocol.Viewer;

/// <summary>
/// UI上の ModelEntity 行に共通する表示と操作を提供する。
/// </summary>
internal static class ModelEntityRowBehavior
{
    /// <summary>Visibilityボタンの識別子。</summary>
    public const int VisibilityButtonId = 1;
    /// <summary>Propertyボタンの識別子。</summary>
    public const int PropertyButtonId = 2;
    private const int HierarchyIndent = 16;

    /// <summary>
    /// Entityの階層の深さに応じて左側に余白を付けたアイコンを取得する。
    /// </summary>
    /// <param name="entity">表示するEntity</param>
    /// <returns>階層に応じた余白を持つアイコン</returns>
    public static Texture2D GetIndentedIcon(ModelEntity entity)
    {
        Texture2D icon = Application.Asset.Icon.GetModelIcon(entity.IconId, Constant.Ui.Tree.HierarchyVisibleIconSize);
        IReadOnlyList<ModelEntity> ancestors = Application.Model.Entity.Registry.GetAncestors(entity.Id);
        int depth = ancestors.Count;
        if (depth > 0 && ancestors[depth - 1].Id == ViewerConstants.RootModelEntityId)
        {
            depth--;
        }

        int indent = checked(depth * HierarchyIndent);
        if (icon == null)
        {
            // アイコンがない行も、アイコン付きの行と名前の開始位置を揃える。
            int iconSize = Constant.Ui.Tree.HierarchyVisibleIconSize;
            return new AtlasTexture { Region = new Rect2(0, 0, checked(indent + iconSize), iconSize) };
        }

        if (depth == 0)
        {
            return icon;
        }

        return new AtlasTexture
        {
            Atlas = icon,
            Region = new Rect2(0, 0, icon.GetWidth(), icon.GetHeight()),
            Margin = new Rect2(indent, 0, indent, 0),
        };
    }

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
    /// Entity行のPropertyボタンとVisibilityボタンを更新する。
    /// </summary>
    /// <param name="item">ボタンを持つ行</param>
    /// <param name="entity">表示状態の正本</param>
    /// <param name="propertyCount">Propertyボタンに表示する数</param>
    public static void RebuildButtons(TreeItem item, ModelEntity entity, int propertyCount)
    {
        foreach (int buttonId in new[] { PropertyButtonId, VisibilityButtonId })
        {
            int index = item.GetButtonById(0, buttonId);
            if (index >= 0)
            {
                item.EraseButton(0, index);
            }
        }

        Texture2D propertyIcon = Application.Asset.Icon.GetProperty(
            propertyCount, entity.IsPropertyTreeVisible, Constant.Ui.Tree.HierarchyVisibleIconSize);
        item.AddButton(0, propertyIcon, id: PropertyButtonId, disabled: entity.Properties.Count == 0);
        Texture2D visibilityIcon = Application.Asset.Icon.GetVisibility(
            entity.Visibility, entity.IsVisible, Constant.Ui.Tree.HierarchyVisibleIconSize);
        item.AddButton(0, visibilityIcon, id: VisibilityButtonId);
    }

    /// <summary>
    /// Entity行のVisibilityボタンを実効表示状態に合わせる。
    /// </summary>
    /// <param name="item">ボタンを持つ行</param>
    /// <param name="entity">表示状態の正本</param>
    public static void UpdateVisibilityButton(TreeItem item, ModelEntity entity)
    {
        int index = item.GetButtonById(0, VisibilityButtonId);
        if (index >= 0)
        {
            Texture2D icon = Application.Asset.Icon.GetVisibility(
                entity.Visibility, entity.IsVisible, Constant.Ui.Tree.HierarchyVisibleIconSize);
            item.SetButton(0, index, icon);
        }
    }

    /// <summary>
    /// Entityと各子孫Entityが持つProperty数の合計を取得する。
    /// </summary>
    /// <param name="root">集計対象のルートEntity</param>
    /// <returns>Entity IDごとの部分木内Property総数</returns>
    public static Dictionary<Guid, int> BuildPropertySubtreeCounts(ModelEntity root)
    {
        Dictionary<Guid, int> counts = new();
        HashSet<Guid> visited = new();
        AddPropertySubtreeCounts(root, counts, visited);
        return counts;
    }

    /// <summary>
    /// 折り畳み状態に応じたPropertyボタン表示数を取得する。
    /// </summary>
    /// <param name="entity">表示数を求めるEntity</param>
    /// <param name="propertySubtreeCounts">Entityごとの部分木内Property総数</param>
    /// <returns>Propertyボタンに表示する数</returns>
    public static int ResolvePropertyCount(ModelEntity entity, IReadOnlyDictionary<Guid, int> propertySubtreeCounts)
    {
        return entity.IsCollapsed
            ? propertySubtreeCounts.GetValueOrDefault(entity.Id, entity.PropertyCountRecursive)
            : entity.PropertyCountRecursive;
    }

    private static int AddPropertySubtreeCounts(ModelEntity entity, Dictionary<Guid, int> counts, HashSet<Guid> visited)
    {
        if (entity == null || !visited.Add(entity.Id))
        {
            return 0;
        }

        int count = entity.PropertyCountRecursive;
        foreach (ModelEntity child in entity.Children)
        {
            count = checked(count + AddPropertySubtreeCounts(child, counts, visited));
        }

        counts[entity.Id] = count;
        return count;
    }

    /// <summary>
    /// 指定したEntityのPropertyまたはVisibilityボタンの操作を実行する。
    /// </summary>
    /// <param name="entityId">操作対象のModelEntity識別子</param>
    /// <param name="buttonId">押されたボタンの識別子</param>
    public static void HandleButtonClick(Guid entityId, long buttonId)
    {
        switch (buttonId)
        {
            case PropertyButtonId:
                Application.Ui.Property.ToggleModelEntityProperties(entityId);
                break;
            case VisibilityButtonId:
                ModelEntity entity = Application.Model.Entity.Registry.Get(entityId);
                if (entity == null)
                {
                    Application.Log.Warn($"ModelEntityRowBehavior: visibility target not found. entityId='{entityId}'");
                    return;
                }

                entity.ToggleVisibility();
                break;
        }
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
