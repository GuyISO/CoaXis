using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Node3D と TreeItem の対応を管理するシングルトンクラス、Autoload としてシーンに追加して使用する、Node3Dのヒエラルキーを TreeItem に反映するための登録・登録解除、対応の取得処理を提供する
/// </summary>
/// <remarks>
/// 実際のモデル動的ロード時の登録の流れはModelLoaderでメインシーンに追加、その後にEventHub経由で通知を受けたTreeが更新され、そこからHierarchyBinderに登録される
/// </remarks>
public partial class HierarchyBinder : Node
{
    public static HierarchyBinder I { get; private set; }

    // Node ↔ TreeItem の対応辞書
    private readonly Dictionary<Node3D, TreeItem> _nodeToItem = new();
    private readonly Dictionary<TreeItem, Node3D> _itemToNode = new();

    public override void _Ready()
    {
        I = this;
    }

	/// <summary>
	/// TreeItem から対応する Node3D を取得する
	/// </summary>
	/// <param name="item">対応を取得する TreeItem</param>
	/// <returns>対応する Node3D</returns>
    public static Node3D GetNode(TreeItem item)
		=> I._itemToNode.TryGetValue(item, out var node) ? node : null;

	/// <summary>
	/// Node3D から対応する TreeItem を取得する
	/// </summary>
	/// <param name="node">対応を取得する Node3D</param>
	/// <returns>対応する TreeItem</returns>
    public static TreeItem GetItem(Node3D node)
        => I._nodeToItem.TryGetValue(node, out var item) ? item : null;

	/// <summary>
	/// Node3D を登録する
	/// </summary>
	/// <param name="node">登録する Node3D</param>
	/// <param name="item">対応する TreeItem</param>
	/// <returns>登録に成功した場合は true、すでに登録されている場合は false</returns>
    public static bool Bind(Node3D node, TreeItem item)
    {
        if (I._nodeToItem.ContainsKey(node))
            return false; // すでに登録されている
		
		if (I._itemToNode.ContainsKey(item))
            return false; // すでに登録されている
		
        I._nodeToItem[node] = item;
        I._itemToNode[item] = node;
        
		return true;
    }

	/// <summary>
	/// Node3D を登録解除する
	/// </summary>
	/// <param name="node">登録解除する Node3D</param>
    public static void Unbind(Node3D node)
    {
        if (!I._nodeToItem.TryGetValue(node, out var item))
            return;

        I._itemToNode.Remove(item);
        I._nodeToItem.Remove(node);

        item.Free();
    }

	/// <summary>
	/// TreeItem を登録解除する
	/// </summary>
	/// <param name="item">登録解除する TreeItem</param>
	public static void Unbind(TreeItem item)
	{
		if (!I._itemToNode.TryGetValue(item, out var node))
			return;

		I._nodeToItem.Remove(node);
		I._itemToNode.Remove(item);

		item.Free();
	}
}