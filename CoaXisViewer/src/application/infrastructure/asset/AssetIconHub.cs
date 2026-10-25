using Godot;
using CoaXis.Protocol.Viewer;
using System.Collections.Generic;

/// <summary>
/// アイコンの取得とキャッシュを管理するハブ。
/// </summary>
public partial class AssetIconHub : BaseHub
{
    #region Fields

    /// <summary>継承表示（可視）アイコンのパス。</summary>
    private const string InheritVisibleIconPath = "res://assets/icon/visibility/inherit_visible.svg";
    /// <summary>継承表示（不可視）アイコンのパス。</summary>
    private const string InheritInvisibleIconPath = "res://assets/icon/visibility/inherit_invisible.svg";
    /// <summary>表示アイコンのパス。</summary>
    private const string VisibleIconPath = "res://assets/icon/visibility/visible.svg";
    /// <summary>非表示アイコンのパス。</summary>
    private const string InvisibleIconPath = "res://assets/icon/visibility/invisible.svg";
    /// <summary>Propertyなしアイコンのパス。</summary>
    private const string PropertyNoneIconPath = "res://assets/icon/property/none.svg";
    /// <summary>Propertyあり・OverlayTree非表示アイコンのパス。</summary>
    private const string PropertyHiddenIconPath = "res://assets/icon/property/hidden.svg";
    /// <summary>Propertyあり・OverlayTree表示中アイコンのパス。</summary>
    private const string PropertyShownIconPath = "res://assets/icon/property/shown.svg";

    /// <summary>ProtocolアイコンIDとViewerアセットの対応表。</summary>
    private static readonly Dictionary<string, string> ModelIconPaths = new(System.StringComparer.OrdinalIgnoreCase)
    {
        [ModelIconId.Annotation] = "res://assets/icon/entity/annotation.svg",
        [ModelIconId.AnnotationSet] = "res://assets/icon/entity/annotationset.svg",
        [ModelIconId.Body] = "res://assets/icon/entity/body.svg",
        [ModelIconId.Component] = "res://assets/icon/entity/component.svg",
        [ModelIconId.HybridBody] = "res://assets/icon/entity/hybridbody.svg",
        [ModelIconId.Instance] = "res://assets/icon/entity/instance.svg",
        [ModelIconId.Line] = "res://assets/icon/entity/line.svg",
        [ModelIconId.Point] = "res://assets/icon/entity/point.svg",
        [ModelIconId.Product] = "res://assets/icon/entity/product.svg",
        [ModelIconId.Surface] = "res://assets/icon/entity/surface.svg",
        [ModelIconId.Target] = "res://assets/icon/mono/target.svg"
    };

    /// <summary>読み込み済みアイコンのキャッシュ。</summary>
    private readonly Dictionary<string, Texture2D> _iconCache = new Dictionary<string, Texture2D>();

    #endregion

    #region Properties

    #endregion

    #region Signals

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
    /// モデルの表示設定と実効表示状態に対応するアイコンを取得する。
    /// </summary>
    /// <param name="visibility">モデルが持つ表示設定</param>
    /// <param name="isVisible">実際にモデルが表示されている場合は true</param>
    /// <param name="size">返却アイコンのサイズ</param>
    /// <returns>取得したアイコン、失敗時は null</returns>
    public Texture2D GetVisibility(ModelVisibility visibility, bool isVisible, int size = 24)
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
    /// ModelEntityのPropertyの有無とOverlayTree表示状態に対応するアイコンを取得する。
    /// </summary>
    /// <param name="hasProperty">Propertyを持つ場合は true</param>
    /// <param name="isTreeVisible">OverlayTreeが表示されている場合は true</param>
    /// <param name="size">返却アイコンのサイズ</param>
    /// <returns>取得したアイコン、失敗時は null</returns>
    public Texture2D GetProperty(bool hasProperty, bool isTreeVisible, int size = 24)
    {
        string path = !hasProperty ? PropertyNoneIconPath : isTreeVisible ? PropertyShownIconPath : PropertyHiddenIconPath;
        return GetByPath(path, size);
    }

    /// <summary>
    /// ModelEntityまたはModelPropertyのアイコンIDに対応する画像を取得する。
    /// </summary>
    /// <param name="iconId">Protocolで定義されたアイコンID</param>
    /// <param name="size">返却アイコンのサイズ</param>
    /// <returns>対応するアイコン。Noneの場合は null</returns>
    public Texture2D GetModelIcon(string iconId, int size = 16)
    {
        if (string.IsNullOrEmpty(iconId))
        {
            return null;
        }

        if (!ModelIconId.IsValid(iconId))
        {
            throw new System.ArgumentException($"Unsupported model icon ID '{iconId}'.", nameof(iconId));
        }

        if (!ModelIconPaths.TryGetValue(iconId, out string path))
        {
            throw new System.InvalidOperationException($"Model icon ID '{iconId}' has no Viewer asset mapping.");
        }

        return GetByPath(path, size);
    }

    /// <summary>
    /// 指定パスのアイコンを取得する。
    /// </summary>
    /// <param name="path">アセットパス</param>
    /// <param name="size">返却アイコンのサイズ</param>
    /// <returns>取得したアイコン、失敗時は null</returns>
    public Texture2D GetByPath(string path, int size = 16)
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
    /// 指定パスが有効なアイコンパスかどうかを判定する。
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
    /// アイコンを取得し、指定サイズにリサイズして返す。
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

        if (image.IsCompressed())
        {
            Error decompressError = image.Decompress();
            if (decompressError != Error.Ok)
            {
                Application.Log.Warn($"AssetService: icon decompression failed. path='{path}', error={decompressError}");
                return null;
            }
        }

        image.Resize(size, size, Image.Interpolation.Lanczos);
        Texture2D resized = ImageTexture.CreateFromImage(image);
        _iconCache[key] = resized;
        return resized;
    }

    #endregion
}