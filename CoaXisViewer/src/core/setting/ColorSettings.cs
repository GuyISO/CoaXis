/// <summary>
/// UI・描画に適用する色設定値。
/// JSON 上は "#RRGGBBAA" 形式の文字列で管理し、読み込み時に Godot の Color へ変換する。
/// </summary>
public sealed class ColorSettings
{
    #region Properties

    /// <summary>
    /// WorldEnvironment の背景色。
    /// </summary>
    public string EnvironmentBackgroundColor { get; set; } = "#202124FF";

    /// <summary>
    /// ビューポートオーバーレイ（中心軸・アークボール）の線色。
    /// </summary>
    public string OverlayLineColor { get; set; } = "#4E95D9FF";

    /// <summary>
    /// 階層ツリーの選択行の背景色。
    /// </summary>
    public string HierarchySelectedColor { get; set; } = "#4E95D9FF";

    /// <summary>
    /// モデル選択ハイライト用マテリアルの色。
    /// </summary>
    public string SelectedMaterialColor { get; set; } = "#4E95D9FF";

    /// <summary>
    /// 測定ラインのマテリアル色。
    /// </summary>
    public string MeasurementLineColor { get; set; } = "#4E95D9FF";

    #endregion

    #region Methods

    /// <summary>
    /// 色設定の不正値を補正する。
    /// </summary>
    public void Normalize()
    {
        if (string.IsNullOrWhiteSpace(EnvironmentBackgroundColor)) EnvironmentBackgroundColor = "#202124FF";
        if (string.IsNullOrWhiteSpace(OverlayLineColor)) OverlayLineColor = "#4E95D9FF";
        if (string.IsNullOrWhiteSpace(HierarchySelectedColor)) HierarchySelectedColor = "#4E95D9FF";
        if (string.IsNullOrWhiteSpace(SelectedMaterialColor)) SelectedMaterialColor = "#4E95D9FF";
        if (string.IsNullOrWhiteSpace(MeasurementLineColor)) MeasurementLineColor = "#4E95D9FF";
    }

    #endregion
}
