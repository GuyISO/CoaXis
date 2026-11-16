using CoaXis.Protocol.Viewer;
using Godot;
using System.Collections.Generic;

/// <summary>
/// パーティクル等で使うテクスチャの取得とキャッシュを管理するハブ。
/// </summary>
public partial class AssetTextureHub : BaseHub
{
    #region Fields

    /// <summary>ProtocolテクスチャIDとViewerアセットの対応表。</summary>
    private static readonly Dictionary<string, string> ParticleTexturePaths = new(System.StringComparer.OrdinalIgnoreCase)
    {
        [ModelParticleTextureId.Star] = "res://assets/icon/particle/star.png",
        [ModelParticleTextureId.Kira] = "res://assets/icon/particle/kira.png"
    };

    /// <summary>読み込み済みテクスチャのキャッシュ。</summary>
    private readonly Dictionary<string, Texture2D> _textureCache = new(System.StringComparer.OrdinalIgnoreCase);

    #endregion

    #region Properties

    #endregion

    #region Signals

    #endregion

    #region Lifecycle

    public override void _ExitTree()
    {
        _textureCache.Clear();

        base._ExitTree();
    }

    #endregion

    #region Events

    #endregion

    #region Methods

    /// <summary>
    /// パーティクルのテクスチャIDに対応する画像を取得する。
    /// </summary>
    /// <param name="textureId">Protocolで定義されたテクスチャID。空の場合は既定の星画像</param>
    /// <returns>対応するテクスチャ、読み込めない場合は null</returns>
    /// <exception cref="System.ArgumentException">未定義のテクスチャIDの場合</exception>
    public Texture2D GetParticle(string textureId)
    {
        string id = string.IsNullOrWhiteSpace(textureId) ? ModelParticleTextureId.Star : textureId.Trim();
        if (!ModelParticleTextureId.IsValid(id))
        {
            throw new System.ArgumentException($"Unsupported particle texture ID '{textureId}'.", nameof(textureId));
        }

        if (_textureCache.TryGetValue(id, out Texture2D cached))
        {
            return cached;
        }

        if (!ParticleTexturePaths.TryGetValue(id, out string path))
        {
            throw new System.InvalidOperationException($"Particle texture ID '{id}' has no Viewer asset mapping.");
        }

        Texture2D texture = ResourceLoader.Exists(path) ? ResourceLoader.Load<Texture2D>(path) : null;
        if (texture == null)
        {
            Application.Log.Warn($"AssetTextureHub: texture load failed. id='{id}', path='{path}'");
            return null;
        }

        _textureCache[id] = texture;
        return texture;
    }

    #endregion

    #region Helpers

    #endregion
}
