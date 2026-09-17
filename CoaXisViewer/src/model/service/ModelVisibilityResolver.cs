using System;

/// <summary>
/// モデル階層から実効表示状態を解決するヘルパー。
/// </summary>
public static class ModelVisibilityResolver
{
    /// <summary>
    /// JSON の表示設定文字列を ModelVisibility に変換する。
    /// </summary>
    public static ModelVisibility Parse(string visibility)
    {
        return Enum.TryParse(visibility, true, out ModelVisibility parsedVisibility)
            ? parsedVisibility
            : ModelVisibility.Inherit;
    }

    /// <summary>
    /// 親の設定をたどってモデルの実効表示状態を返す。
    /// </summary>
    public static bool IsVisible(ModelEntity entity)
    {
        if (entity == null)
        {
            return true;
        }

        if (entity.Visibility == ModelVisibility.Visible)
        {
            return true;
        }

        if (entity.Visibility == ModelVisibility.Invisible)
        {
            return false;
        }

        foreach (ModelEntity ancestor in Application.Model.Registry.GetAncestorEntities(entity.Id))
        {
            if (ancestor.Visibility == ModelVisibility.Visible)
            {
                return true;
            }

            if (ancestor.Visibility == ModelVisibility.Invisible)
            {
                return false;
            }
        }

        return true;
    }
}