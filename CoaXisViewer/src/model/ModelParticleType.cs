/// <summary>
/// ModelProperty が 3D Viewer 上で持つパーティクル表現の種類
/// </summary>
public enum ModelParticleType
{
    /// <summary>パーティクルを表示しない</summary>
    None,

    /// <summary>中心から粒子が噴き出す点状のパーティクル</summary>
    Point,

    /// <summary>箱状の空間で粒子がきらめくパーティクル</summary>
    Box
}
