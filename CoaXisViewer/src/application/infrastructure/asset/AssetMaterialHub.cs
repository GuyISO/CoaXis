using Godot;

/// <summary>
/// マテリアルの取得と指定色の反映を管理するハブ。
/// </summary>
public partial class AssetMaterialHub : BaseHub
{
    #region Fields

    /// <summary>選択強調マテリアルのパス。</summary>
    private const string SelectedMaterialPath = "res://assets/material/selected.tres";

    /// <summary>選択色を HDR 発光として表示する強度。</summary>
    private const float SelectedMaterialEmissionEnergyMultiplier = 2.86f;

    /// <summary>選択強調マテリアル。</summary>
    private Material _selectedMaterial;

    #endregion

    #region Properties

    #endregion

    #region Signals

    #endregion

    #region Lifecycle

    /// <summary>
    /// マテリアル参照を解放する。
    /// </summary>
    public override void _ExitTree()
    {
        _selectedMaterial = null;

        base._ExitTree();
    }

    #endregion

    #region Events

    #endregion

    #region Methods

    /// <summary>
    /// ハイライト表示用のマテリアルを取得する。
    /// </summary>
    /// <param name="primaryColor">選択ハイライトに適用するCoaXisプライマリカラー。</param>
    /// <returns>選択ハイライト用マテリアル。取得失敗時は null</returns>
    public Material GetSelected(Color primaryColor)
    {
        if (_selectedMaterial == null)
        {
            _selectedMaterial = GD.Load<Material>(SelectedMaterialPath);
            if (_selectedMaterial == null)
            {
                Application.Log.Warn($"AssetService: material load failed. path='{SelectedMaterialPath}'");
                return null;
            }
        }

        ApplySelectedMaterialColor(primaryColor);

        return _selectedMaterial;
    }

    /// <summary>
    /// キャッシュ済みの選択マテリアルへ色を反映する。
    /// </summary>
    /// <param name="primaryColor">適用するCoaXisプライマリカラー。</param>
    public void UpdateSelectedColor(Color primaryColor)
    {
        ApplySelectedMaterialColor(primaryColor);
    }

    #endregion

    #region Helpers

    /// <summary>
    /// 選択中のマテリアルに設定される色を適用する。
    /// </summary>
    private void ApplySelectedMaterialColor(Color selectedColor)
    {
        if (_selectedMaterial is not StandardMaterial3D standardMaterial)
        {
            return;
        }

        standardMaterial.AlbedoColor = selectedColor;
        standardMaterial.Emission = selectedColor;
        standardMaterial.EmissionEnergyMultiplier = SelectedMaterialEmissionEnergyMultiplier;
    }

    #endregion
}