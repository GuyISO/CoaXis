using Godot;

/// <summary>
/// ビューポートカメラの永続化状態
/// </summary>
public partial class ViewportCameraState : RefCounted
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
    public static ViewportCameraState CreateDefault()
    {
        return new ViewportCameraState();
    }

    /// <summary>
    /// 不正値を補正する。
    /// </summary>
    public void Normalize()
    {
        Position ??= new[] { 0f, 0f, 0f };
        Rotation ??= new[] { 0f, 0f, 0f, 1f };
        if (Distance <= 0f) Distance = 5f;
        if (Size <= 0f) Size = 5f;
        if (Fov <= 0f || Fov > 180f) Fov = 35f;
        if (string.IsNullOrWhiteSpace(ProjectionType)) ProjectionType = "Perspective";
    }
}
