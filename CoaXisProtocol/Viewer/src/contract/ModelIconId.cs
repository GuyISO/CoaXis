using System;
using System.Collections.Generic;

namespace CoaXis.Protocol.Viewer;

/// <summary>
/// ModelEntityおよびModelPropertyで指定できるアイコンID。
/// </summary>
public static class ModelIconId
{
    /// <summary>注釈アイコン。</summary>
    public const string Annotation = "Annotation";

    /// <summary>注釈セットアイコン。</summary>
    public const string AnnotationSet = "AnnotationSet";

    /// <summary>ボディアイコン。</summary>
    public const string Body = "Body";

    /// <summary>コンポーネントアイコン。</summary>
    public const string Component = "Component";

    /// <summary>ハイブリッドボディアイコン。</summary>
    public const string HybridBody = "HybridBody";

    /// <summary>インスタンスアイコン。</summary>
    public const string Instance = "Instance";

    /// <summary>線アイコン。</summary>
    public const string Line = "Line";

    /// <summary>点アイコン。</summary>
    public const string Point = "Point";

    /// <summary>プロダクトアイコン。</summary>
    public const string Product = "Product";

    /// <summary>面アイコン。</summary>
    public const string Surface = "Surface";

    /// <summary>ターゲットアイコン。</summary>
    public const string Target = "Target";

    private static readonly HashSet<string> ValidIds = new(StringComparer.OrdinalIgnoreCase)
    {
        Annotation,
        AnnotationSet,
        Body,
        Component,
        HybridBody,
        Instance,
        Line,
        Point,
        Product,
        Surface,
        Target
    };

    /// <summary>
    /// 指定されたアイコンIDがサポート対象かどうかを判定する。
    /// </summary>
    /// <param name="iconId">判定するアイコンID</param>
    /// <returns>サポート対象の場合は true</returns>
    public static bool IsValid(string iconId)
    {
        return !string.IsNullOrWhiteSpace(iconId) && ValidIds.Contains(iconId);
    }
}
