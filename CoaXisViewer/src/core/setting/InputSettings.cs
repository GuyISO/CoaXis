/// <summary>
/// 入力操作の感度に適用する設定値。
/// </summary>
public sealed class InputSettings
{
    /// <summary>
    /// キーボード平行移動速度（m/s）。
    /// </summary>
    public float TranslateSpeed { get; set; } = 8.0f;

    /// <summary>
    /// キーボード回転速度（度/秒）。
    /// </summary>
    public float RotateSpeedDeg { get; set; } = 90.0f;

    /// <summary>
    /// キーボードロール回転速度（度/秒）。
    /// </summary>
    public float RollSpeedDeg { get; set; } = 120.0f;

    /// <summary>
    /// マウスホイールによるズーム倍率係数。
    /// </summary>
    public float ZoomFactor { get; set; } = 1.0f;

    /// <summary>
    /// PointerLabel の回転速度（度/秒）。
    /// </summary>
    public float PointerRotationSpeedDeg { get; set; } = 90.0f;

    /// <summary>
    /// 入力感度設定の不正値を補正する。
    /// </summary>
    public void Normalize()
    {
        if (TranslateSpeed <= 0f) TranslateSpeed = 8.0f;
        if (RotateSpeedDeg <= 0f) RotateSpeedDeg = 90.0f;
        if (RollSpeedDeg <= 0f) RollSpeedDeg = 120.0f;
        if (ZoomFactor <= 0f) ZoomFactor = 1.0f;
        if (PointerRotationSpeedDeg < 0f) PointerRotationSpeedDeg = 90.0f;
    }
}
