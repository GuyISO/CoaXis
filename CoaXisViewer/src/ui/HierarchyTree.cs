using Godot;
using System;

public partial class HierarchyTree : Tree
{

	// 関連ノードのキャッシュ
	private Node3D _models = null; // 3Dモデル用ルートノードのキャッシュ
	// アイコンのキャッシュ
	private Texture2D _visibleIcon;

	public override void _Ready()
	{
		// Tree選択イベントの購読開始
		MultiSelected += OnMultiSelected;
		CellSelected += OnCellSelected;

		// イベントの購読
		ModelEventHub.I.AddModelRequested += OnAddModelRequested;
		ModelEventHub.I.SelectModelRequested += OnSelectModelRequested;
		ModelEventHub.I.DeselectModelRequested += OnDeselectModelRequested;

		// 関連ノードのキャッシュ
		_models = GetNode<Node3D>("/root/Main/Models");


		_visibleIcon = LoadIcon("res://assets/icon/icon.svg", 24);

		// VisibleButton 列を固定幅にする
		SetColumnExpand((int)HierarchyTreeColumn.VisibleButton, false);
		SetColumnCustomMinimumWidth((int)HierarchyTreeColumn.VisibleButton, 24); // 24px など

		// ツリーの初期化
		AddToTree(_models);
	}

	public override void _ExitTree()
	{
		// Tree選択イベントの購読解除
		MultiSelected -= OnMultiSelected;
		CellSelected -= OnCellSelected;

		// イベントの購読解除
		ModelEventHub.I.AddModelRequested -= OnAddModelRequested;
		ModelEventHub.I.SelectModelRequested -= OnSelectModelRequested;
		ModelEventHub.I.DeselectModelRequested -= OnDeselectModelRequested;
	}

	#region Events

	/// <summary>
	/// TreeItem が選択・解除されたときのイベントハンドラ
	/// </summary>
	private void OnMultiSelected(TreeItem item, long column, bool selected)
	{
		if (column != (int)HierarchyTreeColumn.Name)
		{
			// 名前列以外の列が選択された場合はそのセルの選択状態を解除して終了する
			if (selected)
			{
				item.Deselect((int)column);
				return;
			}
		}

		if (selected)
		{
			// 選択されたアイテムに対応する Node3D を選択状態にする
			Selection.Add(HierarchyBinder.GetNode(item));
		}
		else
		{
			// 選択が解除されたアイテムに対応する Node3D を選択解除状態にする
			Selection.Remove(HierarchyBinder.GetNode(item));
		}
	}

	/// <summary>
	/// セルが選択されたときのイベントハンドラ、主にボタンのクリックを検知するために使用する
	/// </summary>
	private void OnCellSelected()
	{
		int column = GetSelectedColumn();
		TreeItem item = GetSelected();
		switch (column)
		{
			case (int)HierarchyTreeColumn.VisibleButton:
				Node3D node = HierarchyBinder.GetNode(item);
				if (node != null)
				{
					node.Visible = !node.Visible; // ノードの表示状態を切り替える
					item.SetIconModulate((int)HierarchyTreeColumn.VisibleButton, node.Visible ? new Color(1, 1, 1) : new Color(1, 1, 1, 0.5f)); // アイコンの見た目も更新する
				}
				break;
			default:
				break;
		}
	}

	private void OnAddModelRequested(Node3D node, Node3D parent)
	{
		AddToTree(node, parent);
	}

	private void OnSelectModelRequested(Node3D node)
	{
		TreeItem item = HierarchyBinder.GetItem(node);
		if (item != null)
		{
			item.Select((int)HierarchyTreeColumn.Name);
		}
	}

	private void OnDeselectModelRequested(Node3D node)
	{
		TreeItem item = HierarchyBinder.GetItem(node);
		if (item != null)
		{
			item.Deselect((int)HierarchyTreeColumn.Name);
		}
	}

	#endregion

	/// <summary>
	/// Node3D を TreeItem に追加する
	/// </summary>
	/// <param name="node">追加する Node3D</param>
	/// <param name="parentTreeItem">親の TreeItem</param>
	private void AddToTree(Node3D node, TreeItem parentTreeItem = null)
	{
		// ツリーにアイテムを追加。親が null の場合はルートアイテムとして追加される便利仕様
		TreeItem item = CreateItem(parentTreeItem);
		item.SetText((int)HierarchyTreeColumn.Name, node.Name);

		// 非表示切り替えのためのアイコンを設定
		item.SetCellMode((int)HierarchyTreeColumn.VisibleButton, TreeItem.TreeCellMode.Icon);
		item.SetIcon((int)HierarchyTreeColumn.VisibleButton, _visibleIcon);
		item.SetIconModulate((int)HierarchyTreeColumn.VisibleButton, node.Visible ? new Color(1, 1, 1) : new Color(1, 1, 1, 0.5f)); // 非表示なら半透明にする
		item.SetEditable((int)HierarchyTreeColumn.VisibleButton, true); // アイコンをクリックして編集可能にする

		// Node3D と TreeItem の対応を登録
		HierarchyBinder.Bind(node, item);

		// 子ノードを再帰的に追加
		foreach (Node3D childNode in node.GetChildren())
		{
			// 純粋なNode3Dのみをツリーに追加する、Node3Dを継承しているStaticBody3DやMeshInstance3Dなどはツリーに表示しない
			if (childNode.GetType() == typeof(Node3D))
			{
				AddToTree(childNode, item);
			}
		}
	}

	private void AddToTree(Node3D node , Node3D parentNode)
	{
		TreeItem parentTreeItem = HierarchyBinder.GetItem(parentNode);
		if (parentTreeItem != null)
		{
			AddToTree(node, parentTreeItem);
		}
	}


	private Texture2D LoadIcon(string path, int size = 16)
	{
		var tex = GD.Load<Texture2D>(path);
		var img = tex.GetImage();
		img.Resize(size, size, Image.Interpolation.Lanczos);
		return ImageTexture.CreateFromImage(img);
	}
}
