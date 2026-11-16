using System;
using System.Collections.Generic;

namespace CoaXis.Protocol.Viewer;

/// <summary>
/// ModelPropertyのパーティクルで指定できるテクスチャID。
/// </summary>
public static class ModelParticleTextureId
{
    /// <summary>星形のテクスチャ。省略時の既定。</summary>
    public const string Star = "Star";

    /// <summary>きらめき形のテクスチャ。</summary>
    public const string Kira = "Kira";

    private static readonly HashSet<string> ValidIds = new(StringComparer.OrdinalIgnoreCase)
    {
        Star,
        Kira
    };

    /// <summary>
    /// 指定されたテクスチャIDがサポート対象かどうかを判定する。
    /// </summary>
    /// <param name="textureId">判定するテクスチャID</param>
    /// <returns>サポート対象の場合は true</returns>
    public static bool IsValid(string textureId)
    {
        return !string.IsNullOrWhiteSpace(textureId) && ValidIds.Contains(textureId);
    }
}
