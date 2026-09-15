using System;
using System.Collections.Generic;

namespace CoaXis.Protocol.Viewer;

/// <summary>
/// ModelProperty で扱う値の型を表す定数定義
/// </summary>
public static class ModelPropertyValueType
{
    public const string Int = "int";
    public const string Float = "float";
    public const string String = "string";
    public const string Bool = "bool";
    public const string DateTime = "datetime";
    public const string Vector3 = "Vector3";
    public const string Quaternion = "Quaternion";
    public const string Color = "Color";
    public const string Transform = "Transform";

    // サポート対象の型名を網羅したセット（大文字小文字の差異を許容して判定可能にする）
    private static readonly HashSet<string> ValidTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        Int,
        Float,
        String,
        Bool,
        DateTime,
        Vector3,
        Quaternion,
        Color,
        Transform
    };

    /// <summary>
    /// 指定された型名がサポート対象かどうかを判定する
    /// </summary>
    /// <param name="valueType">判定対象の型名</param>
    /// <returns>サポート対象の場合は true</returns>
    public static bool IsValid(string valueType)
    {
        // 空文字や未定義の型を事前に弾き、不正なプロパティ型の混入を防ぐ
        return !string.IsNullOrWhiteSpace(valueType) && ValidTypes.Contains(valueType);
    }
}
