using System;
using System.Text.Json;

/// <summary>
/// モデルのデータを表す DTO クラス
/// </summary>
public class ModelDto

{
    #region Properties

    public Guid Id { get; init; }
    public Guid? ParentId { get; init; }
    public string Type { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public float[] Position { get; init; } = new[] { 0f, 0f, 0f };
    public float[] Rotation { get; init; } = new[] { 0f, 0f, 0f, 1f };
    public string IconFilePath { get; init; } = string.Empty;
    public string GlbFilePath { get; init; } = string.Empty;
    public string WrlFilePath { get; init; } = string.Empty;

    #endregion

    #region Constructors

    public ModelDto()
    {
        Id = Guid.NewGuid();
    }

    public ModelDto(
        Guid id,
        Guid? parentId = null,
        string type = "",
        string name = "",
        float[] position = null,
        float[] rotation = null,
        string iconFilePath = "",
        string glbFilePath = "",
        string wrlFilePath = "")
    {
        Id = id;
        ParentId = parentId;
        Type = type ?? string.Empty;
        Name = name ?? string.Empty;
        Position = NormalizePosition(position);
        Rotation = NormalizeRotation(rotation);
        IconFilePath = iconFilePath ?? string.Empty;
        GlbFilePath = glbFilePath ?? string.Empty;
        WrlFilePath = wrlFilePath ?? string.Empty;
    }

    #endregion

    #region Public Methods

    public string ToJson(JsonSerializerOptions options = null)
    {
        return JsonSerializer.Serialize(this, options);
    }

    #endregion

    #region Private Methods

    private static float[] NormalizePosition(float[] position)
    {
        if (position == null || position.Length != 3)
        {
            return new[] { 0f, 0f, 0f };
        }

        return new[] { position[0], position[1], position[2] };
    }

    private static float[] NormalizeRotation(float[] rotation)
    {
        if (rotation == null || rotation.Length != 4)
        {
            return new[] { 0f, 0f, 0f, 1f };
        }

        return new[] { rotation[0], rotation[1], rotation[2], rotation[3] };
    }

    #endregion
}
