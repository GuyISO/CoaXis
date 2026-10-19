using System;

namespace CoaXis.Protocol.Viewer;

/// <summary>
/// モデルの属性情報（プロパティ）を表す DTO クラス
/// </summary>
public class ModelPropertyDto
{
    /// <summary>
    /// プロパティの一意識別子
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// 親プロパティまたは所属先モデルの識別子。親が存在しない場合は null
    /// </summary>
    public Guid? ParentId { get; init; } = null;

    /// <summary>
    /// プロパティの種類識別（元の Parameter 名など）
    /// </summary>
    public string PropertyType { get; init; } = string.Empty;

    /// <summary>
    /// 値の型
    /// </summary>
    public string ValueType { get; init; } = string.Empty;

    /// <summary>
    /// 文字列化された値
    /// </summary>
    public string Value { get; init; } = string.Empty;

    /// <summary>
    /// 表示設定（Inherit / Visible / Invisible）。Inherit は親プロパティ、所属モデルの順に継承する
    /// </summary>
    public string Visibility { get; init; } = "Inherit";

    /// <summary>
    /// ツリー表示に使うアイコン画像のパス。空の場合はアイコンなし
    /// </summary>
    public string IconPath { get; init; } = string.Empty;

    /// <summary>
    /// 3D Viewer 上のパーティクル表現の種類（None / Point / Box）
    /// </summary>
    public string ParticleType { get; init; } = ModelPropertyParticleType.None;

    /// <summary>
    /// パーティクルのテクスチャ画像パス（res://...）。空の場合は既定の星画像
    /// </summary>
    public string ParticleTexturePath { get; init; } = string.Empty;

    /// <summary>
    /// パーティクルの色。"#RRGGBB" 形式、空の場合は既定色
    /// </summary>
    public string ParticleColor { get; init; } = string.Empty;
}
