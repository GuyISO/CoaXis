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

        string particleTextureId = dto.ParticleTextureId?.Trim();
        if (particleTextureId == null || (particleTextureId.Length > 0 && !ModelParticleTextureId.IsValid(particleTextureId)))
        {
            throw new ArgumentException(
                $"ModelProperty '{dto.Id}' has an invalid ParticleTextureId '{dto.ParticleTextureId}'.",
                nameof(dto));
        }

        ModelParticleEmitterType particleEmitterType = ParseParticleEmitterType(dto);
        ModelParticleBehaviorType particleBehaviorType = ParseParticleBehaviorType(dto);
        if ((particleEmitterType == ModelParticleEmitterType.None)
            != (particleBehaviorType == ModelParticleBehaviorType.None))
        {
            throw new ArgumentException(
                $"ModelProperty '{dto.Id}' must specify both ParticleEmitterType and ParticleBehaviorType, or set both to None.",
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
            particleEmitterType,
            particleBehaviorType,
            ParseParticleColor(dto),
            particleTextureId);
    }

    #endregion

    #region Helpers

    /// <summary>
    /// パーティクル発生源名を列挙値へ変換する
    /// </summary>
    private static ModelParticleEmitterType ParseParticleEmitterType(ModelPropertyDto dto)
    {
        if (!TryParseEnumName(dto.ParticleEmitterType, out ModelParticleEmitterType emitterType))
        {
            throw new ArgumentException(
                $"ModelProperty '{dto.Id}' has an invalid ParticleEmitterType '{dto.ParticleEmitterType}'; expected None, Point, Box, Sphere or Surface.",
                nameof(dto));
        }

        return emitterType;
    }

    /// <summary>
    /// パーティクル挙動名を列挙値へ変換する
    /// </summary>
    private static ModelParticleBehaviorType ParseParticleBehaviorType(ModelPropertyDto dto)
    {
        if (!TryParseEnumName(dto.ParticleBehaviorType, out ModelParticleBehaviorType behaviorType))
        {
            throw new ArgumentException(
                $"ModelProperty '{dto.Id}' has an invalid ParticleBehaviorType '{dto.ParticleBehaviorType}'; expected None, Disperse or Sparkle.",
                nameof(dto));
        }

        return behaviorType;
    }

    /// <summary>
    /// enumの定義名として有効な文字列を大文字小文字を区別せず列挙値に変換する
    /// </summary>
    private static bool TryParseEnumName<TEnum>(string value, out TEnum parsed)
        where TEnum : struct, Enum
    {
        string name = value?.Trim();
        if (Enum.TryParse(name, ignoreCase: true, out parsed)
            && Enum.IsDefined(parsed)
            && string.Equals(Enum.GetName(parsed), name, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        parsed = default;
        return false;
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
