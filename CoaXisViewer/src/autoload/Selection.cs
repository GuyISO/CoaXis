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

	[Signal] public delegate void RequestSetMultiSelectModeEventHandler(bool enable);
	[Signal] public delegate void NotifySelectedEventHandler(Node3D node);
	[Signal] public delegate void NotifyDeselectedEventHandler(Node3D node);


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
			I.EmitSignal(SignalName.NotifySelected, node);
			LogHub.I.Info($"Selected: {node.Name}");
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
			I.EmitSignal(SignalName.NotifyDeselected, node);
			LogHub.I.Info($"Deselected: {node.Name}");
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
		if (nodes.Count() == 0)
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
		foreach (var node in nodesToDeselect)
		{
			I.EmitSignal(SignalName.NotifyDeselected, node);
			LogHub.I.Info($"Deselected: {node.Name}");
		}
		I._nodes.Clear();
		return true;
	}

	#endregion
}