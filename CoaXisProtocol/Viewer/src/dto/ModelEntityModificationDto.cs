using System;
using System.Text.Json.Serialization;

namespace CoaXis.Protocol.Viewer;

/// <summary>
/// 既存モデル実体の変更可能な値を部分更新するDTO。null の項目は変更しない。
/// </summary>
public sealed class ModelEntityModificationDto
{
    /// <summary>
    /// 変更対象のモデル実体ID。
    /// </summary>
    [JsonRequired]
    public Guid Id { get; init; }

    /// <summary>
    /// 新しい位置（CATIA座標系・mm）。
    /// </summary>
    public float[] Position { get; init; }

    /// <summary>
    /// 新しい回転（CATIA座標系のクォータニオン x, y, z, w）。
    /// </summary>
    public float[] Rotation { get; init; }

    /// <summary>
    /// 新しい表示設定（Inherit / Visible / Invisible）。
    /// </summary>
    public string Visibility { get; init; }

    /// <summary>
    /// ツリー表示で折りたたまれているかどうか。
    /// </summary>
    public bool? IsCollapsed { get; init; }

    /// <summary>
    /// Viewport上にPropertyのOverlayTreeを表示するかどうか。
    /// </summary>
    public bool? IsPropertyTreeVisible { get; init; }
}
