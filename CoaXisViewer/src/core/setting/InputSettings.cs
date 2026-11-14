/// <summary>
/// 入力操作の感度に適用する設定値。
/// </summary>
public sealed class InputSettings
{
    #region Properties

    private const float DefaultTranslateSpeed = 8.0f;
    private const float DefaultRotateSpeedDeg = 90.0f;
    private const float DefaultRollSpeedDeg = 120.0f;
    private const float DefaultZoomFactor = 1.0f;
    private const float DefaultPointerRotationSpeedDeg = 90.0f;

    /// <summary>
    /// キーボード平行移動速度（m/s）。
    /// </summary>
    public float TranslateSpeed { get; set; } = DefaultTranslateSpeed;

    /// <summary>
    /// キーボード回転速度（度/秒）。
    /// </summary>
    public float RotateSpeedDeg { get; set; } = DefaultRotateSpeedDeg;

    /// <summary>
    /// キーボードロール回転速度（度/秒）。
    /// </summary>
    public float RollSpeedDeg { get; set; } = DefaultRollSpeedDeg;

    /// <summary>
    /// マウスホイールによるズーム倍率係数。
    /// </summary>
    public float ZoomFactor { get; set; } = DefaultZoomFactor;

    /// <summary>
    /// PointerLabel の回転速度（度/秒）。
    /// </summary>
    public float PointerRotationSpeedDeg { get; set; } = DefaultPointerRotationSpeedDeg;

    /// <summary>
    /// 入力感度設定の不正値を補正する。
    /// </summary>
    public void Normalize()
    {
        if (TranslateSpeed <= 0f) TranslateSpeed = DefaultTranslateSpeed;
        if (RotateSpeedDeg <= 0f) RotateSpeedDeg = DefaultRotateSpeedDeg;
        if (RollSpeedDeg <= 0f) RollSpeedDeg = DefaultRollSpeedDeg;
        if (ZoomFactor <= 0f) ZoomFactor = DefaultZoomFactor;
        if (PointerRotationSpeedDeg < 0f) PointerRotationSpeedDeg = DefaultPointerRotationSpeedDeg;
    }

    #endregion
}
