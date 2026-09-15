using System;
using System.Collections.Generic;

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
    public static bool IsVisible(ModelEntity modelEntity)
    {
        return IsVisible(modelEntity, new HashSet<Guid>());
    }

    private static bool IsVisible(ModelEntity modelEntity, HashSet<Guid> visitedEntityIds)
    {
        if (modelEntity == null || !visitedEntityIds.Add(modelEntity.Id))
        {
            return true;
        }

        switch (modelEntity.Visibility)
        {
            case ModelVisibility.Visible:
                return true;
            case ModelVisibility.Invisible:
                return false;
            default:
                return IsVisible(modelEntity.Parent, visitedEntityIds);
        }
    }
}