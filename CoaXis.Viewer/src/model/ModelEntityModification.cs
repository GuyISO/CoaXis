using Godot;
using System;

/// <summary>
/// 検証済みのModelEntity部分更新。null の項目は変更しない。座標はGodot座標系へ変換済み。
/// </summary>
public sealed class ModelEntityModification
{
    /// <summary>変更対象のEntity識別子</summary>
    public Guid Id { get; init; }

    /// <summary>新しい位置</summary>
    public Vector3? Position { get; init; }

    /// <summary>新しい回転</summary>
    public Quaternion? Rotation { get; init; }

    /// <summary>新しい表示設定</summary>
    public ModelVisibility? Visibility { get; init; }

    /// <summary>新しい折りたたみ状態</summary>
    public bool? IsCollapsed { get; init; }

    /// <summary>新しいOverlayTree表示状態</summary>
    public bool? IsPropertyTreeVisible { get; init; }
}
