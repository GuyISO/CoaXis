using Godot;
using CoaXis.Protocol.Viewer;
using System.Collections.Generic;

/// <summary>
/// ModelParticle の描画リソースを生成・共有するファクトリ。
/// </summary>
public static class ParticleVisualResourceFactory
{
    #region Fields

    /// <summary>テクスチャ未指定時の既定テクスチャID。</summary>
    private const string DefaultTextureId = ModelParticleTextureId.Star;

    /// <summary>
    /// 描画メッシュの一辺。Godot は粒子のスケールを最小0.001に固定するため、
    /// メッシュを小さくしてスケール側へ大きな値を持たせ、極小サイズまで縮小できるようにする。
    /// </summary>
    public const float MeshUnitSize = 0.001f;

    /// <summary>テクスチャIDごとの描画メッシュキャッシュ。</summary>
    private static readonly Dictionary<string, QuadMesh> MeshCache = new(System.StringComparer.OrdinalIgnoreCase);

    /// <summary>種類ごとのフェード用スケール曲線キャッシュ。</summary>
    private static readonly Dictionary<ModelParticleBehaviorType, CurveTexture> ScaleCurveCache = new();

    #endregion

    #region Methods

    /// <summary>
    /// 指定テクスチャIDを描画用の正規化キーへ変換する。
    /// </summary>
    /// <param name="requestedId">要求されたテクスチャID</param>
    /// <returns>空白時は既定値へフォールバックしたキー</returns>
    public static string NormalizeTextureId(string requestedId)
    {
        return string.IsNullOrWhiteSpace(requestedId) ? DefaultTextureId : requestedId;
    }

    /// <summary>
    /// テクスチャIDに対応する共有メッシュを取得する。未生成の場合のみ作成する。
    /// </summary>
    /// <param name="textureId">描画用に正規化済みのテクスチャID</param>
    /// <returns>共有メッシュ</returns>
    public static QuadMesh GetOrCreateMesh(string textureId)
    {
        if (MeshCache.TryGetValue(textureId, out QuadMesh cached))
        {
            return cached;
        }

        QuadMesh mesh = CreateMesh(Application.Asset.Texture.GetParticle(textureId));
        MeshCache[textureId] = mesh;
        return mesh;
    }

    /// <summary>
    /// 挙動に対応する共有のスケール曲線を取得する。未生成の場合のみ作成する。
    /// </summary>
    /// <param name="behaviorType">パーティクルの挙動</param>
    /// <returns>共有スケール曲線</returns>
    public static CurveTexture GetScaleCurve(ModelParticleBehaviorType behaviorType)
    {
        if (ScaleCurveCache.TryGetValue(behaviorType, out CurveTexture cached) &&
            GodotObject.IsInstanceValid(cached) &&
            GodotObject.IsInstanceValid(cached.Curve))
        {
            return cached;
        }

        var curve = new Curve();
        if (behaviorType is ModelParticleBehaviorType.Disperse or ModelParticleBehaviorType.Emerge)
        {
            // 発生時にサイズ1、消滅時に0へ線形に遷移させる。Linear モードは端点の接線が0になるため、傾き-1を明示する
            curve.AddPoint(new Vector2(0f, 1f), 0f, -1f);
            curve.AddPoint(new Vector2(1f, 0f), -1f, 0f);
        }
        else
        {
            // 発生時にサイズ0、中央で最大1、消滅時に0へ線形に遷移させる
            curve.AddPoint(new Vector2(0f, 0f));
            curve.AddPoint(new Vector2(0.5f, 1f));
            curve.AddPoint(new Vector2(1f, 0f));
        }

        var texture = new CurveTexture { Curve = curve };
        ScaleCurveCache[behaviorType] = texture;
        return texture;
    }

    #endregion

    #region Helpers

    /// <summary>
    /// テクスチャを貼った 1x1 の Billboard 用メッシュを生成する。大きさはProcessMaterialのスケールで決める。
    /// </summary>
    private static QuadMesh CreateMesh(Texture2D texture)
    {
        var material = new StandardMaterial3D
        {
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            VertexColorUseAsAlbedo = true,
            AlbedoTexture = texture,
            BillboardMode = BaseMaterial3D.BillboardModeEnum.Enabled,
            BillboardKeepScale = true,
        };
        return new QuadMesh { Size = Vector2.One * MeshUnitSize, Material = material };
    }

    #endregion
}
