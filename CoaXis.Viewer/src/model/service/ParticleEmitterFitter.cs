using Godot;
using CoaXis.Protocol.Viewer;

/// <summary>
/// ModelNode の形状に合わせて、パーティクルの放出範囲・大きさ・描画範囲を算出するサービス。
/// </summary>
public static class ParticleEmitterFitter
{
    #region Fields

    /// <summary>AABB対角線長に対する、各方向への余白の割合。</summary>
    private const float PaddingRatio = -0.05f;

    /// <summary>AABB対角線長に対する、スプライト1枚の一辺の割合。モデルの大きさに関わらず密度感を保つ。</summary>
    private const float SpriteSizeRatio = 0.03f;

    /// <summary>Emerge の移動距離を Disperse に対して抑える割合。</summary>
    private const float EmergeVelocityRatio = 0.2f;

    #endregion

    #region Methods

    /// <summary>
    /// ModelNode の AABB に合わせて ProcessMaterial を更新し、パーティクルの配置と描画範囲を算出する。
    /// </summary>
    /// <remarks>
    /// 薄いモデルでもパーティクルが見えるよう余白を足す。
    /// 計算は ModelNode ローカル空間で行い、ModelNode の回転・スケールに追従させる。
    /// </remarks>
    /// <param name="modelNode">パーティクルが所属する ModelNode</param>
    /// <param name="material">更新対象の ProcessMaterial</param>
    /// <param name="emitterType">パーティクルの発生源</param>
    /// <param name="behaviorType">パーティクルの挙動</param>
    /// <param name="lifetime">パーティクルの寿命（秒）</param>
    /// <param name="position">ModelNode ローカル座標でのパーティクルの配置</param>
    /// <param name="visibilityAabb">パーティクルローカル座標での描画範囲</param>
    /// <returns>配置できた場合は true、AABB が無い・放出点を取得できない場合は false</returns>
    public static bool TryFit(
        ModelNode modelNode,
        ParticleProcessMaterial material,
        ModelParticleEmitterType emitterType,
        ModelParticleBehaviorType behaviorType,
        double lifetime,
        out Vector3 position,
        out Aabb visibilityAabb)
    {
        position = Vector3.Zero;
        visibilityAabb = default;

        if (!WorldAabbUtility.TryGetWorldAabb(new[] { modelNode }, out Aabb worldAabb) ||
            worldAabb.Size == Vector3.Zero)
        {
            return false;
        }

        Aabb localAabb = modelNode.GlobalTransform.AffineInverse() * worldAabb;
        float diagonal = localAabb.Size.Length();
        float margin = diagonal * PaddingRatio;
        Vector3 halfSize = localAabb.Size * 0.5f + Vector3.One * margin;

        // 描画メッシュは共有するため、サイズはメッシュではなくProcessMaterialのスケールで表現する
        float spriteSize = diagonal * SpriteSizeRatio / ParticleVisualResourceFactory.MeshUnitSize;
        material.ScaleMin = spriteSize;
        material.ScaleMax = spriteSize;
        float movementDistance = ApplyOutwardVelocity(material, behaviorType, halfSize, lifetime);

        position = localAabb.GetCenter();
        switch (emitterType)
        {
            case ModelParticleEmitterType.Surface:
                if (!TryApplySurfacePoints(modelNode, material))
                {
                    return false;
                }

                // 放出点は ModelNode ローカル座標のため、原点に置いて AABB をそのまま使う
                position = Vector3.Zero;
                float surfacePadding = margin + movementDistance;
                visibilityAabb = new Aabb(
                    localAabb.Position - Vector3.One * surfacePadding,
                    localAabb.Size + Vector3.One * surfacePadding * 2f);
                break;

            case ModelParticleEmitterType.Box:
                material.EmissionBoxExtents = halfSize;
                Vector3 visibleHalfSize = halfSize + Vector3.One * movementDistance;
                visibilityAabb = new Aabb(-visibleHalfSize, visibleHalfSize * 2f);
                break;

            case ModelParticleEmitterType.Sphere:
                float radius = halfSize.Length();
                material.EmissionSphereRadius = radius;
                float visibleRadius = radius + movementDistance;
                visibilityAabb = CreateCenteredAabb(visibleRadius);
                break;

            default:
                visibilityAabb = CreateCenteredAabb(halfSize.Length());
                break;
        }

        return true;
    }

    #endregion

    #region Helpers

    /// <summary>
    /// 外向き挙動の初速を設定し、寿命内の移動距離を返す。
    /// </summary>
    /// <returns>寿命内の最大移動距離、外向き挙動以外は 0</returns>
    private static float ApplyOutwardVelocity(
        ParticleProcessMaterial material,
        ModelParticleBehaviorType behaviorType,
        Vector3 halfSize,
        double lifetime)
    {
        float velocityRatio = behaviorType switch
        {
            ModelParticleBehaviorType.Disperse => 1f,
            ModelParticleBehaviorType.Emerge => EmergeVelocityRatio,
            _ => 0f,
        };
        if (velocityRatio == 0f)
        {
            return 0f;
        }

        float distance = halfSize.Length() * velocityRatio;
        material.InitialVelocityMax = distance / (float)lifetime;
        material.InitialVelocityMin = material.InitialVelocityMax / 3f;
        return distance;
    }

    /// <summary>
    /// ModelNode 表面のサンプリング点を放出点テクスチャとして設定する。
    /// </summary>
    /// <returns>放出点を設定できた場合は true</returns>
    private static bool TryApplySurfacePoints(ModelNode modelNode, ParticleProcessMaterial material)
    {
        if (!modelNode.TryGetSurfacePoints(out ImageTexture texture, out int count))
        {
            return false;
        }

        material.EmissionPointTexture = texture;
        material.EmissionPointCount = count;
        return true;
    }

    /// <summary>
    /// 原点中心で各方向に指定半径まで広がる AABB を生成する。
    /// </summary>
    private static Aabb CreateCenteredAabb(float radius)
    {
        return new Aabb(Vector3.One * -radius, Vector3.One * radius * 2f);
    }

    #endregion
}
