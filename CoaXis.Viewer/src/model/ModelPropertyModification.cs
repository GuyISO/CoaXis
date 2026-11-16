using Godot;
using System;

/// <summary>
/// 検証済みのModelProperty部分更新。null の項目は変更しない。
/// </summary>
public sealed class ModelPropertyModification
{
    /// <summary>変更対象のProperty識別子</summary>
    public Guid Id { get; init; }

    /// <summary>新しい値</summary>
    public string Value { get; init; }

    /// <summary>新しい表示設定</summary>
    public ModelVisibility? Visibility { get; init; }

    /// <summary>パーティクルの色を変更するかどうか</summary>
    public bool ChangesParticleColor { get; init; }

    /// <summary>パーティクル色。ChangesParticleColor が true で null の場合は既定色へ戻す</summary>
    public Color? ParticleColor { get; init; }
}
