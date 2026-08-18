using System;
using System.Text.Json;

/// <summary>
/// ベース DTO クラス
/// </summary>
public abstract class BaseDto
{
    public string ToJson(JsonSerializerOptions options = null)
    {
        return JsonSerializer.Serialize(this, options);
    }
}

/// <summary>
/// モデルのデータを表す DTO クラス
/// </summary>
public class ModelDto : BaseDto
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid? ParentId { get; init; } = null;
    public string Type { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public float[] Position { get; init; } = new[] { 0f, 0f, 0f };
    public float[] Rotation { get; init; } = new[] { 0f, 0f, 0f, 1f };
    public string Visibility { get; init; } = "Inherit";
    public string IconFilePath { get; init; } = string.Empty;
    public string GlbFilePath { get; init; } = string.Empty;
    public string WrlFilePath { get; init; } = string.Empty;
}
