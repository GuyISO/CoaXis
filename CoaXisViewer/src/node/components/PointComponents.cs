using Godot;

/// <summary>
/// 点モデル向けの内部構造を表すコンポーネント
/// </summary>
public partial class PointComponents : AnyComponents
{
    #region Properties

    /// <summary>
    /// 点モデル固有の追加ノードの例
    /// </summary>
    public Marker3D Anchor { get; private set; }

    #endregion

    #region Internal Helpers

    protected override void InitializeDerivedComponents()
    {
        Anchor = CreateNode<Marker3D>("Anchor");
    }

    #endregion
}