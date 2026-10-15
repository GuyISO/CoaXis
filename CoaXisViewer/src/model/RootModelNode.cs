using Godot;
using System;

/// <summary>
/// モデルのルートノードで、シーンルートに配置されるモデル
/// </summary>
public partial class RootModelNode : ModelNode
{
    #region Properties

    /// <summary>
    /// ルート ModelEntity に対応するノードは常に親モデルを持たない
    /// </summary>
    public override ModelNode ParentModel => null;

    #endregion

    #region Lifecycle

    public RootModelNode(Guid entityId)
        : base(entityId)
    {
    }

    public override void _Ready()
    {
        base._Ready();
    }

    public override void _ExitTree()
    {
        base._ExitTree();
    }

    #endregion
}
