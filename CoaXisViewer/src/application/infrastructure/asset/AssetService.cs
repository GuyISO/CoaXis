using Godot;
using System.Collections.Generic;

/// <summary>
/// プロジェクト内アセットの取得とキャッシュを一元化する Autoload ノード
/// </summary>
public partial class AssetService : Node
{
    #region Fields

    private const string VisibleIconPath = "res://assets/icon/visible.svg";
    private const string InvisibleIconPath = "res://assets/icon/invisible.svg";
    private const string SelectedMaterialPath = "res://assets/materials/selected.tres";

    private readonly Dictionary<string, Texture2D> _iconCache = new Dictionary<string, Texture2D>();
    private Material _selectedMaterial;

    #endregion

    #region Lifecycle

    public override void _Ready()
    {
        SubscribeApplicationEvents();
    }

    public override void _ExitTree()
    {
        UnsubscribeApplicationEvents();
        _iconCache.Clear();
        _selectedMaterial = null;

        base._ExitTree();
    }

    #endregion

    #region Events

    /// <summary>
    /// Applicationイベントの購読を開始する
    /// </summary>
    private void SubscribeApplicationEvents()
    {
        Application.Setting.Event.SettingsNotified += ApplySettings;
    }

    /// <summary>
    /// Applicationイベントの購読を解除する
    /// </summary>
    private void UnsubscribeApplicationEvents()
    {
        Application.Setting.Event.SettingsNotified -= ApplySettings;
    }

    /// <summary>
    /// 設定値を反映する
    /// </summary>
    private void ApplySettings()
    {
        ApplySelectedMaterialColor();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// モデル表示状態用のアイコンを取得する
    /// </summary>
    /// <param name="isVisible">表示状態なら true、非表示状態なら false</param>
    /// <param name="size">返却アイコンのサイズ</param>
    /// <returns>取得したアイコン、失敗時は null</returns>
    internal Texture2D GetVisibilityIcon(bool isVisible, int size = 24)
    {
        string path = isVisible ? VisibleIconPath : InvisibleIconPath;
        return GetIcon(path, size);
    }

    /// <summary>
    /// ハイライト表示用のマテリアルを取得する
    /// </summary>
    /// <returns>選択ハイライト用マテリアル。取得失敗時は null</returns>
    internal Material GetSelectedMaterial()
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

    /// <summary>
    /// 指定パスのアイコンを取得する
    /// </summary>
    /// <param name="path">アセットパス</param>
    /// <param name="size">返却アイコンのサイズ</param>
    /// <returns>取得したアイコン、失敗時は null</returns>
    internal Texture2D GetIcon(string path, int size = 16)
    {
        if (!IsInsideTree())
        {
            Application.Log.Warn($"AssetService is not initialized. path='{path}', size={size}");
            return null;
        }

        if (!IsValidIconPath(path))
        {
            return null;
        }

        return GetOrCreateIcon(path, size);
    }

    #endregion

    #region Internal Helpers

    private static bool IsValidIconPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        // Godot の仮想ルートだけでは実リソースを指さないためロード不可
        if (path == "res://")
        {
            return false;
        }

        return true;
    }

    private Texture2D GetOrCreateIcon(string path, int size)
    {
        string key = $"{path}|{size}";
        if (_iconCache.TryGetValue(key, out Texture2D cachedIcon))
        {
            return cachedIcon;
        }

        Texture2D source = GD.Load<Texture2D>(path);
        if (source == null)
        {
            Application.Log.Warn($"AssetService: icon load failed. path='{path}'");
            return null;
        }

        Image image = source.GetImage();
        if (image == null)
        {
            Application.Log.Warn($"AssetService: icon image is null. path='{path}'");
            _iconCache[key] = source;
            return source;
        }

        image.Resize(size, size, Image.Interpolation.Lanczos);
        Texture2D resized = ImageTexture.CreateFromImage(image);
        _iconCache[key] = resized;
        return resized;
    }

    private void ApplySelectedMaterialColor()
    {
        if (_selectedMaterial is not StandardMaterial3D standardMaterial)
        {
            return;
        }

        standardMaterial.AlbedoColor = Color.FromHtml(Application.Setting.Service.Current.Color.SelectedMaterialColor);
    }

    #endregion
}
