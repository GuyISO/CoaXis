using Godot;

/// <summary>
/// マテリアルの取得と設定反映を管理するハブ。
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

    public override void _ExitTree()
    {
        _selectedMaterial = null;

        base._ExitTree();
    }

    #endregion

    #region Methods

    /// <summary>
    /// ハイライト表示用のマテリアルを取得する。
    /// </summary>
    /// <returns>選択ハイライト用マテリアル。取得失敗時は null</returns>
    public Material GetSelected()
    {
        if (_selectedMaterial != null)
        {
            return _selectedMaterial;
        }

        _selectedMaterial = GD.Load<Material>(SelectedMaterialPath);
        if (_selectedMaterial == null)
        {
            Application.Log.Warn($"AssetService: material load failed. path='{SelectedMaterialPath}'");
            return null;
        }

        ApplySelectedMaterialColor();

        return _selectedMaterial;
    }

    #endregion

    #region Helpers

    /// <summary>
    /// 選択中のマテリアルに設定される色を適用する。
    /// </summary>
    private void ApplySelectedMaterialColor()
    {
        if (_selectedMaterial is not StandardMaterial3D standardMaterial)
        {
            return;
        }

        Color selectedColor = Color.FromHtml(Application.Setting.Current.Color.SelectedMaterialColor);
        standardMaterial.AlbedoColor = selectedColor;
        standardMaterial.Emission = selectedColor;
        standardMaterial.EmissionEnergyMultiplier = SelectedMaterialEmissionEnergyMultiplier;
    }

    #endregion
}