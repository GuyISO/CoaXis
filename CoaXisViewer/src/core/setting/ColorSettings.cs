/// <summary>
/// UI・描画に適用する色設定値。
/// JSON 上は "#RRGGBBAA" 形式の文字列で管理し、読み込み時に Godot の Color へ変換する。
/// </summary>
public sealed class ColorSettings
{
    #region Properties

    private const string DefaultEnvironmentBackgroundColor = "#202124FF";
    private const string DefaultOverlayLineColor = "#4E95D9FF";
    private const string DefaultHierarchySelectedColor = "#4E95D9FF";
    private const string DefaultSelectedMaterialColor = "#4E95D9FF";
    private const string DefaultMeasurementLineColor = "#4E95D9FF";

    /// <summary>
    /// WorldEnvironment の背景色。
    /// </summary>
    public string EnvironmentBackgroundColor { get; set; } = DefaultEnvironmentBackgroundColor;

    /// <summary>
    /// ビューポートオーバーレイ（中心軸・アークボール）の線色。
    /// </summary>
    public string OverlayLineColor { get; set; } = DefaultOverlayLineColor;

    /// <summary>
    /// 階層ツリーの選択行の背景色。
    /// </summary>
    public string HierarchySelectedColor { get; set; } = DefaultHierarchySelectedColor;

    /// <summary>
    /// モデル選択ハイライト用マテリアルの色。
    /// </summary>
    public string SelectedMaterialColor { get; set; } = DefaultSelectedMaterialColor;

    /// <summary>
    /// 測定ラインのマテリアル色。
    /// </summary>
    public string MeasurementLineColor { get; set; } = DefaultMeasurementLineColor;

    #endregion

    #region Methods

    /// <summary>
    /// 色設定の不正値を補正する。
    /// </summary>
    public void Normalize()
    {
        if (string.IsNullOrWhiteSpace(EnvironmentBackgroundColor)) EnvironmentBackgroundColor = DefaultEnvironmentBackgroundColor;
        if (string.IsNullOrWhiteSpace(OverlayLineColor)) OverlayLineColor = DefaultOverlayLineColor;
        if (string.IsNullOrWhiteSpace(HierarchySelectedColor)) HierarchySelectedColor = DefaultHierarchySelectedColor;
        if (string.IsNullOrWhiteSpace(SelectedMaterialColor)) SelectedMaterialColor = DefaultSelectedMaterialColor;
        if (string.IsNullOrWhiteSpace(MeasurementLineColor)) MeasurementLineColor = DefaultMeasurementLineColor;
    }

    #endregion
}
