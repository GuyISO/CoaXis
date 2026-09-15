using System;

namespace CoaXis.Protocol.Viewer;

/// <summary>
/// モデル実体のデータを表す DTO クラス
/// </summary>
public class ModelEntityDto : BaseDto
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid? ParentId { get; init; } = null;
    public string Type { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public float[] Position { get; init; } = new[] { 0f, 0f, 0f };
    public float[] Rotation { get; init; } = new[] { 0f, 0f, 0f, 1f };
    public string Visibility { get; init; } = "Inherit";
    public bool IsCollapsed { get; init; } = false;
    public string IconPath { get; init; } = string.Empty;
    public string ScenePath { get; init; } = string.Empty;
    public bool AlignToAabbCenter { get; init; } = false;
}
