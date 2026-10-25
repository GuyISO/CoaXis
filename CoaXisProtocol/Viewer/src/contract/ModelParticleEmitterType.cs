namespace CoaXis.Protocol.Viewer;

/// <summary>
/// ModelProperty に指定するパーティクル発生源を表す
/// </summary>
public enum ModelParticleEmitterType
{
    /// <summary>パーティクルを表示しない</summary>
    None,

    /// <summary>一点から粒子を放出する</summary>
    Point,

    /// <summary>箱状の空間から粒子を放出する</summary>
    Box,

    /// <summary>球状の空間から粒子を放出する</summary>
    Sphere,

    /// <summary>所属Entityのメッシュ表面から粒子を放出する</summary>
    Surface
}
