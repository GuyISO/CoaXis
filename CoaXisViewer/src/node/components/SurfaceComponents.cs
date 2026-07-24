using Godot;

/// <summary>
/// 面モデル向けの内部構造を表すコンポーネント
/// </summary>
public partial class SurfaceComponents : AnyComponents
{
    #region Properties

    /// <summary>
    /// 面モデル固有の追加ノードの例
    /// </summary>
    public Node3D SurfaceOverlay { get; private set; }

    #endregion

    #region Internal Helpers

    protected override void InitializeDerivedComponents()
    {
        SurfaceOverlay = CreateNode<Node3D>("SurfaceOverlay");
    }

    #endregion
}