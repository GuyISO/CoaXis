using System;

/// <summary>
/// モデル階層から実効表示状態を解決するヘルパー。
/// </summary>
public static class ModelVisibilityResolver
{
    #region Methods

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
    /// 表示設定文字列を厳密に ModelVisibility へ変換する。
    /// </summary>
    /// <exception cref="ArgumentException">定義されていない表示設定の場合</exception>
    public static ModelVisibility ParseStrict(string visibility)
    {
        if (Enum.TryParse(visibility?.Trim(), true, out ModelVisibility parsed)
            && Enum.IsDefined(parsed)
            && !string.IsNullOrWhiteSpace(visibility)
            && !int.TryParse(visibility.Trim(), out _))
        {
            return parsed;
        }

        throw new ArgumentException(
            $"Invalid Visibility '{visibility}'; expected Inherit, Visible or Invisible.",
            nameof(visibility));
    }

    /// <summary>
    /// 親の設定をたどってモデルの実効表示状態を返す。
    /// </summary>
    public static bool ResolveIsVisible(ModelEntity entity)
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

        foreach (ModelEntity ancestor in Application.Model.Entity.Registry.GetAncestors(entity.Id))
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

    /// <summary>
    /// 親プロパティ、所属モデルの順に設定をたどってPropertyの実効表示状態を返す。
    /// </summary>
    public static bool ResolveIsVisible(ModelProperty property)
    {
        var visited = new System.Collections.Generic.HashSet<Guid>();
        ModelProperty current = property;
        while (current != null && visited.Add(current.Id))
        {
            if (current.Visibility == ModelVisibility.Visible)
            {
                return true;
            }

            if (current.Visibility == ModelVisibility.Invisible)
            {
                return false;
            }

            current = current.ParentProperty;
        }

        return ResolveIsVisible(property?.GetEntity());
    }

    /// <summary>
    /// 現在の表示設定から次の切替先を返す。
    /// </summary>
    /// <param name="visibility">現在の表示設定</param>
    /// <returns>次の表示設定</returns>
    public static ModelVisibility GetNextVisibility(ModelVisibility visibility)
    {
        return visibility switch
        {
            ModelVisibility.Inherit => ModelVisibility.Visible,
            ModelVisibility.Visible => ModelVisibility.Invisible,
            _ => ModelVisibility.Inherit,
        };
    }

    #endregion
}