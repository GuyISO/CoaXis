using Godot;
using System;

/// <summary>
/// モデルのルートノードで、シーンルートに配置されるモデル
/// </summary>
public partial class RootModelNode : ModelNode
{
    #region Properties

    /// <summary>
    /// RootModel は常に親モデルを持たない
    /// </summary>
    public override ModelNode ParentModel => null;

    #endregion

    #region Lifecycle

    public RootModelNode(Guid modelId)
        : base(modelId)
    {
    }

    public override void _Ready()
    {
        base._Ready();

        SubscribeApplicationEvents();
    }

    public override void _ExitTree()
    {
        UnsubscribeApplicationEvents();

        base._ExitTree();
    }

    #endregion

    #region Events

    /// <summary>
    /// Applicationイベントの購読を開始する
    /// </summary>
    private void SubscribeApplicationEvents()
    {
    }

    /// <summary>
    /// Applicationイベントの購読を解除する
    /// </summary>
    private void UnsubscribeApplicationEvents()
    {
    }

    #endregion

    #region Internal Helpers

    protected override ModelComponents CreateComponents()
    {
        return new RootComponents();
    }

    #endregion
}
