using Godot;

/// <summary>
/// ビューポートカメラの永続化状態
/// </summary>
public partial class CameraState : RefCounted
{
    /// <summary>
    /// 注視点のワールド座標。
    /// </summary>
    public float[] Position { get; private set; } = new[] { 0f, 0f, 0f };

    /// <summary>
    /// 注視点の回転クォータニオン。
    /// </summary>
    public float[] Rotation { get; private set; } = new[] { 0f, 0f, 0f, 1f };

    /// <summary>
    /// 透視投影時のカメラ距離。
    /// </summary>
    public float Distance { get; private set; } = 5f;

    /// <summary>
    /// 正投影時のサイズ。
    /// </summary>
    public float Size { get; private set; } = 5f;

    /// <summary>
    /// 視野角（度）。
    /// </summary>
    public float Fov { get; private set; } = 35f;

    /// <summary>
    /// 投影タイプ。"Perspective" / "Orthogonal"。
    /// </summary>
    public string ProjectionType { get; private set; } = "Perspective";

    /// <summary>
    /// 既定値を返す。
    /// </summary>
    public static CameraState CreateDefault()
    {
        return new CameraState();
    }

    /// <summary>
    /// Godot 内部状態からカメラ状態を生成する。
    /// </summary>
    /// <param name="position">注視点の位置（Godot座標系）</param>
    /// <param name="rotation">注視点の回転（Godot座標系）</param>
    /// <param name="distance">透視投影時のカメラ距離</param>
    /// <param name="size">正投影時のサイズ</param>
    /// <param name="fov">視野角（度）</param>
    /// <param name="projectionType">投影タイプ</param>
    /// <returns>生成されたカメラ状態</returns>
    public static CameraState Create(
        Vector3 position,
        Quaternion rotation,
        float distance,
        float size,
        float fov,
        Camera3D.ProjectionType projectionType)
    {
        return new CameraState
        {
            Position = new[] { position.X, position.Y, position.Z },
            Rotation = new[] { rotation.X, rotation.Y, rotation.Z, rotation.W },
            Distance = distance,
            Size = size,
            Fov = fov,
            ProjectionType = projectionType.ToString()
        };
    }

    /// <summary>
    /// 不正値を補正する。
    /// </summary>
    public void Normalize()
    {
        if (Position == null || Position.Length != 3)
        {
            Position = new[] { 0f, 0f, 0f };
        }

        if (Rotation == null || Rotation.Length != 4)
        {
            Rotation = new[] { 0f, 0f, 0f, 1f };
        }

        if (Distance <= 0f) Distance = 5f;
        if (Size <= 0f) Size = 5f;
        if (Fov <= 0f || Fov > 180f) Fov = 35f;
        if (string.IsNullOrWhiteSpace(ProjectionType)) ProjectionType = "Perspective";
    }
}
