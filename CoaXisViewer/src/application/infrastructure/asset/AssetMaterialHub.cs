using Godot;

/// <summary>
/// マテリアルの取得と設定反映を管理するハブ。
/// </summary>
public partial class AssetMaterialHub : BaseHub
{
    #region Fields

    private const string SelectedMaterialPath = "res://assets/material/selected.tres";

    private Material _selectedMaterial;

    #endregion

    #region Properties

    #endregion

    #region Lifecycle

    public override void _Ready()
    {
        SubscribeApplicationEvents();
    }

    public override void _ExitTree()
    {
        UnsubscribeApplicationEvents();
        _selectedMaterial = null;

        base._ExitTree();
    }

    #endregion

    #region Signals

    #endregion

    #region Events

    // TODO: Settingから管理を委譲したら削除できるはず

    /// <summary>
    /// Application イベントの購読を開始する。
    /// </summary>
    private void SubscribeApplicationEvents()
    {
        Application.Setting.SettingsNotified += ApplySettings;
    }

    /// <summary>
    /// Application イベントの購読を解除する。
    /// </summary>
    private void UnsubscribeApplicationEvents()
    {
        Application.Setting.SettingsNotified -= ApplySettings;
    }

    /// <summary>
    /// 設定値を反映する。
    /// </summary>
    private void ApplySettings()
    {
        ApplySelectedMaterialColor();
    }

    #endregion

    #region Methods

    /// <summary>
    /// ハイライト表示用のマテリアルを取得する
    /// </summary>
    /// <returns>選択ハイライト用マテリアル。取得失敗時は null</returns>
    internal Material GetSelected()
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
    /// 選択中のマテリアルに設定される色を適用する
    /// </summary>
    private void ApplySelectedMaterialColor()
    {
        if (_selectedMaterial is not StandardMaterial3D standardMaterial)
        {
            return;
        }

        standardMaterial.AlbedoColor = Color.FromHtml(Application.Setting.Current.Color.SelectedMaterialColor);
    }

    #endregion
}