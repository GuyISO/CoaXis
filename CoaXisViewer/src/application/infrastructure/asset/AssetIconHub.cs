using Godot;
using System.Collections.Generic;

/// <summary>
/// アイコンの取得とキャッシュを管理するハブ。
/// </summary>
public partial class AssetIconHub : BaseHub
{
    #region Fields

    private const string DefaultEntityIconPath = "res://assets/icon/mono/question.svg";
    private const string InheritVisibleIconPath = "res://assets/icon/visibility/inherit_visible.svg";
    private const string InheritInvisibleIconPath = "res://assets/icon/visibility/inherit_invisible.svg";
    private const string VisibleIconPath = "res://assets/icon/visibility/visible.svg";
    private const string InvisibleIconPath = "res://assets/icon/visibility/invisible.svg";

    private readonly Dictionary<string, Texture2D> _iconCache = new Dictionary<string, Texture2D>();

    #endregion

    #region Properties

    #endregion

    #region Lifecycle

    public override void _ExitTree()
    {
        _iconCache.Clear();

        base._ExitTree();
    }

    #endregion

    #region Events

    #endregion

    #region Methods

    /// <summary>
    /// モデルの表示設定と実効表示状態に対応するアイコンを取得する
    /// </summary>
    /// <param name="visibility">モデルが持つ表示設定</param>
    /// <param name="isVisible">実際にモデルが表示されている場合は true</param>
    /// <param name="size">返却アイコンのサイズ</param>
    /// <returns>取得したアイコン、失敗時は null</returns>
    internal Texture2D GetVisibility(ModelVisibility visibility, bool isVisible, int size = 24)
    {
        string path = visibility switch
        {
            ModelVisibility.Visible => VisibleIconPath,
            ModelVisibility.Invisible => InvisibleIconPath,
            _ => isVisible ? InheritVisibleIconPath : InheritInvisibleIconPath,
        };
        return GetByPath(path, size);
    }

    /// <summary>
    /// モデルアイコン不在時に使う既定アイコンを取得する
    /// </summary>
    /// <param name="size">返却アイコンのサイズ</param>
    /// <returns>取得したアイコン、失敗時は null</returns>
    internal Texture2D GetDefaultEntity(int size = 16)
    {
        return GetByPath(DefaultEntityIconPath, size);
    }

    /// <summary>
    /// 指定パスのアイコンを取得する
    /// </summary>
    /// <param name="path">アセットパス</param>
    /// <param name="size">返却アイコンのサイズ</param>
    /// <returns>取得したアイコン、失敗時は null</returns>
    internal Texture2D GetByPath(string path, int size = 16)
    {
        if (!IsInsideTree())
        {
            Application.Log.Warn($"AssetService is not initialized. path='{path}', size={size}");
            return null;
        }

        if (!IsValidPath(path))
        {
            return null;
        }

        return GetOrCreate(path, size);
    }

    #endregion

    #region Helpers

    /// <summary>
    /// 指定パスが有効なアイコンパスかどうかを判定する
    /// </summary>
    private static bool IsValidPath(string path)
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

    /// <summary>
    /// アイコンを取得し、指定サイズにリサイズして返す
    /// </summary>
    private Texture2D GetOrCreate(string path, int size)
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

    #endregion
}