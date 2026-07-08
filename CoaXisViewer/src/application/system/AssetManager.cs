using Godot;
using System.Collections.Generic;

/// <summary>
/// プロジェクト内アセットの取得とキャッシュを一元化する Autoload ノード
/// </summary>
public partial class AssetManager : Node
{
    #region Fields

    private const string VisibleIconPath = "res://assets/icon/visible.svg";
    private const string InvisibleIconPath = "res://assets/icon/invisible.svg";

    private readonly Dictionary<string, Texture2D> _iconCache = new Dictionary<string, Texture2D>();

    #endregion

    #region Lifecycle

    public override void _ExitTree()
    {
        _iconCache.Clear();
        base._ExitTree();
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
    /// 指定パスのアイコンを取得する
    /// </summary>
    /// <param name="path">アセットパス</param>
    /// <param name="size">返却アイコンのサイズ</param>
    /// <returns>取得したアイコン、失敗時は null</returns>
    internal Texture2D GetIcon(string path, int size = 16)
    {
        if (!IsInsideTree())
        {
            Warn($"AssetManager is not initialized. path='{path}', size={size}");
            return null;
        }

        return GetOrCreateIcon(path, size);
    }

    #endregion

    #region Internal Helpers

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
            Warn($"AssetManager: icon load failed. path='{path}'");
            return null;
        }

        Image image = source.GetImage();
        if (image == null)
        {
            Warn($"AssetManager: icon image is null. path='{path}'");
            _iconCache[key] = source;
            return source;
        }

        image.Resize(size, size, Image.Interpolation.Lanczos);
        Texture2D resized = ImageTexture.CreateFromImage(image);
        _iconCache[key] = resized;
        return resized;
    }

    private void Warn(string message)
    {
        Application.System.Log.Warn(message);
    }

    #endregion
}