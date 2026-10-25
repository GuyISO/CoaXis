namespace CoaXis.Protocol.Viewer;

/// <summary>
/// ModelProperty に指定するパーティクル挙動を表す
/// </summary>
public enum ModelParticleBehaviorType
{
    /// <summary>挙動を適用しない</summary>
    None,

    /// <summary>粒子を発生源から外向きに広げながら縮小させる</summary>
    Disperse,

    /// <summary>粒子を出現後に拡大・縮小させる</summary>
    Sparkle,

    /// <summary>粒子を発生源から弱く外向きに広げながら縮小させる</summary>
    Emerge
}
