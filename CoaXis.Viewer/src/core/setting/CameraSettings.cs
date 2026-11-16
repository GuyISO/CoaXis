/// <summary>
/// カメラ操作に適用する設定値。
/// </summary>
public sealed class CameraSettings
{
    #region Properties

    private const float DefaultZoomBase = 1.005f;
    private const float DefaultMinZoomValue = 0.01f;
    private const float DefaultFitPadding = 1.1f;
    private const float MinimumFitPadding = 1.0f;
    private const float DefaultTweenDuration = 0.5f;

    /// <summary>
    /// ズーム倍率変更時の底。exponent 1 あたりの拡大倍率。
    /// </summary>
    public float ZoomBase { get; set; } = DefaultZoomBase;

    /// <summary>
    /// ズームの最小値。これ以上近づけないようにするための制限値。
    /// </summary>
    public float MinZoomValue { get; set; } = DefaultMinZoomValue;

    /// <summary>
    /// Fit All In 時に対象が画面にぴったり収まるようにするための余白倍率。
    /// </summary>
    public float FitPadding { get; set; } = DefaultFitPadding;

    /// <summary>
    /// Tween を使用する場合のアニメーション時間（秒）。
    /// </summary>
    public float TweenDuration { get; set; } = DefaultTweenDuration;

    #endregion
    
    #region Methods

    /// <summary>
    /// カメラ設定の不正値を補正する。
    /// </summary>
    public void Normalize()
    {
        if (ZoomBase <= 1.0f) ZoomBase = DefaultZoomBase;
        if (MinZoomValue <= 0f) MinZoomValue = DefaultMinZoomValue;
        if (FitPadding < MinimumFitPadding) FitPadding = MinimumFitPadding;
        if (TweenDuration < 0f) TweenDuration = DefaultTweenDuration;
    }

    #endregion
}
