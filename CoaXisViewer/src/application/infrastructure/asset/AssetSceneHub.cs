using Godot;
using System.Collections.Generic;

/// <summary>
/// シーンの取得とキャッシュを管理するハブ。
/// </summary>
public partial class AssetSceneHub : BaseHub
{
    #region Fields

    /// <summary>計測点ラベルのシーン。</summary>
    private readonly PackedScene _pointerLabel = ResourceLoader.Load<PackedScene>("res://scenes/part/pointer_label.tscn")!;

    /// <summary>読み込み済みシーンのキャッシュ。</summary>
    private readonly Dictionary<string, PackedScene> _cache = new Dictionary<string, PackedScene>();

    #endregion

    #region Properties

    #endregion

    #region Signals

    #endregion

    #region Lifecycle

    public override void _ExitTree()
    {
        _cache.Clear();

        base._ExitTree();
    }

    #endregion

    #region Events

    #endregion

    #region Methods

    /// <summary>
    /// res:// パスのシーンキャッシュを取得する。
    /// </summary>
    /// <param name="path">シーンのリソースパス</param>
    /// <returns>キャッシュ済みシーン。未登録または res:// 外のパスの場合は null</returns>
    internal PackedScene TryGetByPath(string path)
    {
        if (!IsResourcePath(path))
        {
            return null;
        }

        return _cache.TryGetValue(path, out PackedScene packedScene) ? packedScene : null;
    }

    /// <summary>
    /// res:// パスのシーンをアセットキャッシュへ登録する。
    /// </summary>
    /// <param name="path">シーンのリソースパス</param>
    /// <param name="packedScene">キャッシュするシーン</param>
    internal void Cache(string path, PackedScene packedScene)
    {
        if (IsResourcePath(path) && packedScene != null)
        {
            _cache[path] = packedScene;
        }
    }

    /// <summary>
    /// ポインターラベルのシーンを取得する。
    /// </summary>
    /// <returns>ポインターラベルのシーン</returns>
    internal PointerLabel GetPointerLabel()
    {
        return _pointerLabel.Instantiate<PointerLabel>();
    }

    #endregion

    #region Helpers

    /// <summary>リソースパス形式（res://）か判定する。</summary>
    private static bool IsResourcePath(string path)
    {
        return !string.IsNullOrWhiteSpace(path)
            && path.StartsWith("res://", System.StringComparison.Ordinal);
    }

    #endregion
}