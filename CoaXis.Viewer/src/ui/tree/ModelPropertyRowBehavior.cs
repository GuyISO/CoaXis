using System;
using System.Collections.Generic;
using Godot;

/// <summary>
/// UI上の ModelProperty 行に共通する表示と操作を提供する。
/// </summary>
internal static class ModelPropertyRowBehavior
{
    /// <summary>Visibilityボタンの識別子。</summary>
    public const int VisibilityButtonId = ModelEntityRowBehavior.VisibilityButtonId;
    /// <summary>Particleアイコンボタンの識別子。</summary>
    public const int ParticleButtonId = 3;
    private const int HierarchyIndent = 16;

    /// <summary>Propertyの表示文字列を取得する。</summary>
    public static string GetText(ModelProperty property)
    {
        return string.IsNullOrEmpty(property.Value)
            ? property.PropertyType
            : $"{property.PropertyType} : {property.Value}";
    }

    /// <summary>Property階層の深さをアイコン余白で表したテクスチャを取得する。</summary>
    public static Texture2D GetIndentedIcon(ModelProperty property)
    {
        return GetIndentedIcon(property, 0);
    }

    /// <summary>Property階層に加え、所有Entityまでの階層深度をアイコン余白で表したテクスチャを取得する。</summary>
    /// <param name="property">表示するProperty</param>
    /// <param name="hierarchyOffset">Property階層の前へ加算する深度</param>
    public static Texture2D GetIndentedIcon(ModelProperty property, int hierarchyOffset)
    {
        int depth = checked(Math.Max(0, hierarchyOffset) + GetDepth(property));
        int indent = checked(depth * HierarchyIndent);
        Texture2D icon = GetModelIcon(property.IconId);
        if (icon == null)
        {
            int iconSize = Constant.Ui.Tree.HierarchyVisibleIconSize;
            return new AtlasTexture { Region = new Rect2(0, 0, checked(indent + iconSize), iconSize) };
        }

        if (indent == 0)
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

    /// <summary>Propertyの行表示と共通メタデータ、ボタンを設定する。</summary>
    /// <param name="item">設定対象のTreeItem</param>
    /// <param name="property">表示するProperty</param>
    /// <param name="offsetByHierarchy">Propertyの親階層をアイコン位置に反映する場合はtrue</param>
    public static void Configure(TreeItem item, ModelProperty property, bool offsetByHierarchy = false)
    {
        if (item == null || property == null)
        {
            return;
        }

        item.SetText(0, GetText(property));
        Texture2D icon = offsetByHierarchy ? GetIndentedIcon(property) : GetModelIcon(property.IconId);
        if (icon != null)
        {
            item.SetIcon(0, icon);
        }

        item.SetMeta("PropertyValue", Variant.CreateFrom(property.Value ?? string.Empty));
        item.SetMeta("PropertyId", Variant.CreateFrom(property.Id.ToString()));

        Texture2D particleIcon = GetParticleIcon(property);
        if (particleIcon != null)
        {
            item.AddButton(0, particleIcon, id: ParticleButtonId);
        }
        item.AddButton(0, GetVisibilityIcon(property), id: VisibilityButtonId);
    }

    /// <summary>Property行の値表示とコンテキストメニュー用メタデータを更新する。</summary>
    public static void UpdateText(TreeItem item, ModelProperty property)
    {
        if (item == null || property == null)
        {
            return;
        }

        item.SetText(0, GetText(property));
        item.SetMeta("PropertyValue", Variant.CreateFrom(property.Value ?? string.Empty));
    }

    /// <summary>Property行のアイコンとVisibilityボタンを現在のテーマ色で更新する。</summary>
    /// <param name="item">更新対象のTreeItem</param>
    /// <param name="property">行が表すProperty</param>
    /// <param name="offsetByHierarchy">Propertyの親階層をアイコン位置に反映する場合はtrue</param>
    public static void UpdateThemeIcons(TreeItem item, ModelProperty property, bool offsetByHierarchy = false)
    {
        if (item == null || property == null)
        {
            return;
        }

        Texture2D icon = offsetByHierarchy ? GetIndentedIcon(property) : GetModelIcon(property.IconId);
        if (icon != null)
        {
            item.SetIcon(0, icon);
        }
        UpdateVisibilityButton(item, property);
    }

    /// <summary>Propertyの実効表示状態に合わせてVisibilityボタンを更新する。</summary>
    public static void UpdateVisibilityButton(TreeItem item, ModelProperty property)
    {
        int index = item.GetButtonById(0, VisibilityButtonId);
        if (index >= 0)
        {
            item.SetButton(0, index, GetVisibilityIcon(property));
        }
    }

    /// <summary>Property選択状態に応じた背景色を行へ適用する。</summary>
    public static void ApplySelectionHighlight(TreeItem item, Guid propertyId, Color selectionColor)
    {
        if (item == null)
        {
            return;
        }

        item.SetCustomBgColor(0, Application.Model.Property.Selection.Contains(propertyId)
            ? selectionColor
            : Colors.Transparent);
    }

    /// <summary>Property行のボタン操作を実行する。</summary>
    public static void HandleButtonClick(Guid propertyId, long buttonId)
    {
        if (buttonId != VisibilityButtonId)
        {
            return;
        }

        ModelProperty property = Application.Model.Property.Registry.Get(propertyId);
        if (property == null)
        {
            Application.Log.Warn($"ModelPropertyRowBehavior: visibility target not found. propertyId='{propertyId}'");
            return;
        }

        property.ToggleVisibility();
    }

    /// <summary>TreeItemに保存されたProperty識別子を取得する。</summary>
    public static Guid GetPropertyId(TreeItem item)
    {
        return item != null
            && Guid.TryParse(item.GetMeta("PropertyId", Variant.CreateFrom(string.Empty)).AsString(), out Guid propertyId)
            ? propertyId
            : Guid.Empty;
    }

    /// <summary>TreeItemに保存されたProperty値を取得する。</summary>
    public static string GetPropertyValue(TreeItem item)
    {
        return item?.GetMeta("PropertyValue", Variant.CreateFrom(string.Empty)).AsString() ?? string.Empty;
    }

    /// <summary>Propertyの値を対象にコンテキストメニューを表示する。</summary>
    public static void ShowContextMenu(TreeItem item)
    {
        Guid propertyId = GetPropertyId(item);
        ModelProperty property = Application.Model.Property.Registry.Get(propertyId);
        Guid entityId = property?.GetEntity()?.Id ?? Guid.Empty;
        Application.Ui.Menu.ShowModelPropertyMenu(GetPropertyValue(item), entityId);
    }

    /// <summary>Propertyに対するManipulate操作を通知する。</summary>
    public static void Manipulate(Guid propertyId)
    {
        Application.Model.Property.Manipulate(propertyId);
    }

    private static int GetDepth(ModelProperty property)
    {
        int depth = 0;
        var visited = new HashSet<Guid>();
        ModelProperty ancestor = property.ParentProperty;
        while (ancestor != null && visited.Add(ancestor.Id))
        {
            depth++;
            ancestor = ancestor.ParentProperty;
        }

        return depth;
    }

    private static Texture2D GetModelIcon(string iconId)
    {
        return string.IsNullOrEmpty(iconId)
            ? null
            : Application.Asset.Icon.GetModelIcon(
                iconId, Application.Ui.Theme.TreeIconColor, Constant.Ui.Tree.HierarchyVisibleIconSize);
    }

    private static Texture2D GetVisibilityIcon(ModelProperty property)
    {
        return ModelEntityRowBehavior.GetVisibilityIcon(property.Visibility, property.IsVisible);
    }

    private static Texture2D GetParticleIcon(ModelProperty property)
    {
        if (!property.IsParticleEnabled)
        {
            return null;
        }

        Texture2D source = Application.Asset.Texture.GetParticle(property.ParticleTextureId);
        Image image = source?.GetImage();
        if (image == null)
        {
            return null;
        }

        image = (Image)image.Duplicate();
        if (image.IsCompressed())
        {
            image.Decompress();
        }

        image.Convert(Image.Format.Rgba8);
        int size = Constant.Ui.Tree.HierarchyVisibleIconSize;
        image.Resize(size, size, Image.Interpolation.Lanczos);

        Color tint = property.ParticleColor ?? Colors.White;
        for (int y = 0; y < image.GetHeight(); y++)
        {
            for (int x = 0; x < image.GetWidth(); x++)
            {
                image.SetPixel(x, y, image.GetPixel(x, y) * tint);
            }
        }

        return ImageTexture.CreateFromImage(image);
    }
}
