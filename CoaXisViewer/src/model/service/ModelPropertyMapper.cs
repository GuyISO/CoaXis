using CoaXis.Protocol.Viewer;
using Godot;
using System;
using System.Text.RegularExpressions;

/// <summary>
/// モデル属性DTOをModelPropertyへ変換する
/// </summary>
public static partial class ModelPropertyMapper
{
    #region Methods

    /// <summary>
    /// DTOをModelPropertyへ変換する
    /// </summary>
    /// <param name="dto">変換元のモデル属性DTO</param>
    /// <returns>変換済みのModelProperty</returns>
    /// <exception cref="ArgumentNullException">dtoがnullの場合</exception>
    /// <exception cref="ArgumentException">アイコンID、パーティクルの種類または色が不正な場合</exception>
    public static ModelProperty Map(ModelPropertyDto dto)
    {
        if (dto == null)
        {
            throw new ArgumentNullException(nameof(dto));
        }

        if (dto.IconId == null || (!string.IsNullOrEmpty(dto.IconId) && !ModelIconId.IsValid(dto.IconId)))
        {
            throw new ArgumentException(
                $"ModelProperty '{dto.Id}' has an invalid IconId '{dto.IconId}'.",
                nameof(dto));
        }

        Guid resolvedParentId = dto.ParentId ?? Guid.Empty;
        return new ModelProperty(
            dto.Id,
            resolvedParentId,
            dto.PropertyType,
            dto.ValueType,
            dto.Value,
            ModelVisibilityResolver.Parse(dto.Visibility),
            dto.IconId,
            ParseParticleType(dto),
            ParseParticleColor(dto),
            dto.ParticleTexturePath?.Trim());
    }

    #endregion

    #region Helpers

    /// <summary>
    /// パーティクルの種類名を列挙値へ変換する
    /// </summary>
    private static ModelParticleType ParseParticleType(ModelPropertyDto dto)
    {
        if (!ModelPropertyParticleType.IsValid(dto.ParticleType))
        {
            throw new ArgumentException(
                $"ModelProperty '{dto.Id}' has an invalid ParticleType '{dto.ParticleType}'; expected None, Point or Box.",
                nameof(dto));
        }

        return Enum.Parse<ModelParticleType>(dto.ParticleType, ignoreCase: true);
    }

    /// <summary>
    /// パーティクルの色文字列を解析する、空の場合は null（既定色）を返す
    /// </summary>
    private static Color? ParseParticleColor(ModelPropertyDto dto)
    {
        if (dto.ParticleColor == null)
        {
            throw new ArgumentException($"ModelProperty '{dto.Id}' has a null ParticleColor.", nameof(dto));
        }

        string text = dto.ParticleColor.Trim();
        if (text.Length == 0)
        {
            return null;
        }

        if (HexColorPattern().IsMatch(text))
        {
            return Color.FromHtml(text);
        }

        throw new ArgumentException(
            $"ModelProperty '{dto.Id}' has an invalid ParticleColor '{dto.ParticleColor}'; expected '#RRGGBB'.",
            nameof(dto));
    }

    /// <summary>
    /// パーティクル色の形式（#RRGGBB）に一致する正規表現を取得する
    /// </summary>
    [GeneratedRegex("^#[0-9A-Fa-f]{6}$")]
    private static partial Regex HexColorPattern();

    #endregion
}
