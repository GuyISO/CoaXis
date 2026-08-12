/// <summary>
/// カメラ操作に適用する設定値。
/// </summary>
public sealed class CameraSettings
{
    /// <summary>
    /// ズーム倍率変更時の底。exponent 1 あたりの拡大倍率。
    /// </summary>
    public float ZoomBase { get; set; } = 1.005f;

    /// <summary>
    /// ズームの最小値。これ以上近づけないようにするための制限値。
    /// </summary>
    public float MinZoomValue { get; set; } = 0.01f;

    /// <summary>
    /// Fit All In 時に対象が画面にぴったり収まるようにするための余白倍率。
    /// </summary>
    public float FitPadding { get; set; } = 1.1f;

    /// <summary>
    /// Tween を使用する場合のアニメーション時間（秒）。
    /// </summary>
    public float TweenDuration { get; set; } = 0.5f;

    /// <summary>
    /// カメラ設定の不正値を補正する。
    /// </summary>
    public void Normalize()
    {
        if (ZoomBase <= 1.0f) ZoomBase = 1.005f;
        if (MinZoomValue <= 0f) MinZoomValue = 0.01f;
        if (FitPadding < 1.0f) FitPadding = 1.0f;
        if (TweenDuration < 0f) TweenDuration = 0.5f;
    }
}
