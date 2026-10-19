using System;
using System.Collections.Generic;

namespace CoaXis.Protocol.Viewer;

/// <summary>
/// ModelProperty に付与するパーティクル表現の種類を表す定数定義
/// </summary>
public static class ModelPropertyParticleType
{
    /// <summary>パーティクルを表示しない</summary>
    public const string None = "None";

    /// <summary>中心から粒子が噴き出す点状のパーティクル</summary>
    public const string Point = "Point";

    /// <summary>箱状の空間で粒子がきらめくパーティクル</summary>
    public const string Box = "Box";

    // 大文字小文字の差異を許容して判定可能にする
    private static readonly HashSet<string> ValidTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        None,
        Point,
        Box
    };

    /// <summary>
    /// 指定された種類名がサポート対象かどうかを判定する
    /// </summary>
    /// <param name="particleType">判定対象の種類名</param>
    /// <returns>サポート対象の場合は true</returns>
    public static bool IsValid(string particleType)
    {
        return !string.IsNullOrWhiteSpace(particleType) && ValidTypes.Contains(particleType);
    }
}
