using Godot;

/// <summary>
/// AutoLoad 登録ノードの共通シングルトン基底クラス
/// </summary>
public abstract partial class AutoloadNodeBase<TNode> : Node where TNode : AutoloadNodeBase<TNode>
{
	public static TNode Instance { get; private set; }

	public override void _EnterTree()
	{
		Instance = (TNode)this;
	}

	public override void _ExitTree()
	{
		if (ReferenceEquals(Instance, this))
		{
			Instance = null;
		}
	}
}