using System;
using System.Text.Json.Serialization;

namespace CoaXis.Protocol.Viewer;

/// <summary>
/// 既存モデル属性の変更可能な値を部分更新するDTO。null の項目は変更しない。
/// </summary>
public sealed class ModelPropertyModificationDto
{
    /// <summary>
    /// 変更対象のモデル属性ID。
    /// </summary>
    [JsonRequired]
    public Guid Id { get; init; }

    /// <summary>
    /// 新しい文字列化された値。
    /// </summary>
    public string Value { get; init; }

    /// <summary>
    /// 新しい表示設定（Inherit / Visible / Invisible）。
    /// </summary>
    public string Visibility { get; init; }

    /// <summary>
    /// 新しいパーティクルの色。"#RRGGBB" 形式、空文字の場合は既定色へ戻す。
    /// </summary>
    public string ParticleColor { get; init; }
}
