using Godot;
using System;
using System.Linq;
using System.Collections.Generic;

/// <summary>
/// 選択管理クラスです。選択状態の管理と、選択変更イベントの発行を担当します。
/// Autoloadに登録してシングルトン参照します。
/// </summary>
public partial class Selection : Node
{
    public static Selection I { get; private set; }

	public static bool IsMultiSelectMode { get; set; } = false;

	private HashSet<Node3D> _nodes = new HashSet<Node3D>();

    public override void _Ready()
    {
        I = this;
    }

	#region Public API

	/// <summary>
	/// 現在の選択ノードのコレクションを取得します。外部向けに読み取り専用で提供されます。
	/// </summary>
	public static IReadOnlyCollection<Node3D> Nodes => I._nodes;

	/// <summary>
	/// 現在の選択ノードの数を取得します。
	/// </summary>
	public static int Count => I._nodes.Count;

	/// <summary>
	/// Fit処理に使用可能な選択ノードの配列を取得します。
	/// </summary>
	/// <remarks>
	/// 解放済みノードやツリー外ノードを除外したスナップショットを返します。
	/// </remarks>
	public static Node3D[] GetNodesArray()
	{
		return I._nodes
			.Where(node => node != null && GodotObject.IsInstanceValid(node) && node.IsInsideTree())
			.ToArray();
	}

	/// <summary>
	/// 指定したノードのみの選択状態にします（既存の選択はすべて解除されます）。
	/// </summary>
	/// <param name="node">選択するノードです。</param>
	public static void Set(Node3D node)
	{
		Clear();
		Add(node);
	}

	/// <summary>
	/// 指定したノード群のみの選択状態にします（既存の選択はすべて解除されます）。
	/// </summary>
	/// <param name="nodes">選択するノードの列挙体です。</param>
	public static void Set(IEnumerable<Node3D> nodes)
	{
		Clear();
		foreach (var node in nodes)
		{
			Add(node);
		}
	}

	/// <summary>
	/// 指定したノードを選択状態にします。
	/// </summary> <param name="node">選択するノードです。</param>
	/// <returns>ノードが新たに選択された場合はtrue、それ以外の場合はfalseを返します。</returns>
	/// <remarks>ノードがすでに選択されている場合は何も起こりません。</remarks>
    public static bool Add(Node3D node)
	{
		if (I._nodes.Add(node))
		{
			ModelEventHub.RequestSelectModel(node);
			HighLightNode(node, true);
			LogHub.Info($"Selected: {node.Name}");
			return true;
		}
		return false;
	}

	/// <summary>
	/// 指定したノード群を選択状態にします。
	/// </summary> <param name="nodes">選択するノードの列挙体です。</param>
	public static void Add(IEnumerable<Node3D> nodes)
	{
		foreach (var node in nodes)
		{
			Add(node);
		}
	}

	/// <summary>
	/// 指定したノードを選択から外します。
	/// </summary> <param name="node">選択から外すノードです。</param>
	/// <returns>ノードが選択から外された場合はtrue、それ以外の場合はfalseを返します。</returns>
	/// <remarks>ノードが選択されていない場合は何も起こりません。</remarks>
	public static bool Remove(Node3D node)
	{
		if (I._nodes.Remove(node))
		{
			ModelEventHub.RequestDeselectModel(node);
			HighLightNode(node, false);
			LogHub.Info($"Deselected: {node.Name}");
			return true;
		}
		return false;
	}

	/// <summary>
	/// 指定したノード群を選択から外します。
	/// </summary> <param name="nodes">選択から外すノードの列挙体です。</param>
	public static void Remove(IEnumerable<Node3D> nodes)
	{
		foreach (var node in nodes)
		{
			Remove(node);
		}
	}

	/// <summary>
	/// 指定したノードの選択状態を切り替えます。
	/// </summary> <param name="node">切り替えるノードです。</param>
	public static void Toggle(Node3D node)
	{
		if (I._nodes.Contains(node))
		{
			Remove(node);
		}
		else
		{
			Add(node);
		}
	}

	/// <summary>
	/// 指定したノード群の選択状態を切り替えます。
	/// </summary> <param name="nodes">切り替えるノードの列挙体です。</param>
	public static void Toggle(IEnumerable<Node3D> nodes)
	{
		// 切り替えるノードがない場合は何もしない
		if (!nodes.Any())
		{
			return;
		}

		foreach (var node in nodes)
		{
			Toggle(node);
		}
	}

	/// <summary>
	/// すべての選択を解除します。
	/// </summary>
	/// <returns>選択状態が変更された場合はtrue、それ以外の場合はfalseを返します。</returns>
	public static bool Clear()
	{
		if (I._nodes.Count == 0)
		{
			return false;
		}

		var nodesToDeselect = I._nodes.ToArray();
		
		// 先にクリアしてからシグナル発報することで、シグナルハンドラ内で選択状態確認した際の整合性を保つ
		I._nodes.Clear();

		// ノードの選択解除シグナルとハイライト解除は個々に行う
		foreach (var node in nodesToDeselect)
		{
			ModelEventHub.RequestDeselectModel(node);
			HighLightNode(node, false);
			LogHub.Info($"Deselected: {node.Name}");
		}
		return true;
	}

	/// <summary>
	/// 指定したノードの選択状態を切り替えます。
	/// </summary>
	/// <param name="node">切り替えるノードです。</param>
	/// <param name="enable">選択状態を有効にする場合はtrue、無効にする場合はfalseです。</param>
	private static void HighLightNode(Node3D node, bool enable = true)
	{
		var node3Ds = GetNode3DsRecursively(node);
		foreach (var node3D in node3Ds)
		{
			HighlightMesh(node3D, enable);
		}
	}

	/// <summary>
	/// 指定したノードのハイライト状態を切り替えます。
	/// </summary>
	/// <param name="node">切り替えるノードです。</param>
	/// <param name="enable">ハイライトを有効にする場合はtrue、ハイライトを解除する場合はfalseです。</param>
	private static void HighlightMesh(Node3D node, bool enable = true)
	{
		if (enable)
		{
			// 選択状態の適用は子孫のMeshInstance3Dすべてにとりあえず適用すればよい
			var meshInstances = GetMeshInstancesRecursively(node);
			foreach (var mesh in meshInstances)			{
				mesh.MaterialOverride = ResourceLoader.Load<StandardMaterial3D>("res://assets/materials/selected.tres");
			}
		}
		else
		{
			// 選択状態の解除は、祖先のNode3Dに選択状態のものがいなければ子孫のMeshInstance3Dすべてから選択状態を解除する判定が必要
			if (!HasSelectedAncestor(node))
			{
				var meshInstances = GetMeshInstancesRecursively(node);
				foreach (var mesh in meshInstances)
				{
					mesh.MaterialOverride = null;
				}
			}
		}
	}

	/// <summary>
	/// 指定したノードとその子孫から純粋なNode3Dを再帰的に取得します。
	/// </summary>
	/// <param name="node">取得対象のノードです。</param>
	/// <returns>取得したNode3Dのリストです。</returns>
	private static List<Node3D> GetNode3DsRecursively(Node node)
	{
		var node3Ds = new List<Node3D>();

		if (node.GetType() == typeof(Node3D))
		{
			node3Ds.Add((Node3D)node);
		}

		foreach (Node child in node.GetChildren())
		{
			node3Ds.AddRange(GetNode3DsRecursively(child));
		}

		return node3Ds;
	}

	/// <summary>
	/// 指定したノードとその子孫からMeshInstance3Dを再帰的に取得します。
	/// </summary>
	/// <param name="node">取得対象のノードです。</param>
	private static List<MeshInstance3D> GetMeshInstancesRecursively(Node node)
	{
		var meshInstances = new List<MeshInstance3D>();

		if (node is MeshInstance3D meshInstance)
		{
			meshInstances.Add(meshInstance);
		}

		foreach (Node child in node.GetChildren())
		{
			meshInstances.AddRange(GetMeshInstancesRecursively(child));
		}

		return meshInstances;
	}

	/// <summary>
	/// 指定したノードの祖先に選択状態のノードが存在するかどうかを判定します。
	/// </summary>
	/// <param name="node3D">判定対象のノードです。</param>
	private static bool HasSelectedAncestor(Node node3D)
	{
		var node = node3D;
		while (node != null)
		{
			if (I._nodes.Contains(node))
			{
				return true;
			}
			node = node.GetParent() as Node3D;
		}
		return false;
	}

	#endregion
}