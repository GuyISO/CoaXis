using Godot;
using System;

public partial class HierarchyTree : Tree
{

	// 関連ノードのキャッシュ
	private RootModel _rootModel = null; // 3Dモデル用ルートノードのキャッシュ
	// アイコンのキャッシュ
	private Texture2D _visibleIcon;

	public override void _Ready()
	{
		// Tree選択イベントの購読開始
		MultiSelected += OnMultiSelected;
		CellSelected += OnCellSelected;

		// イベントの購読
		ModelEventHub.I.AddModelRequested += OnAddModelRequested;
		ModelEventHub.I.ModelSelectionStateNotified += OnModelSelectionStateNotified;

		// 関連ノードのキャッシュ
		_rootModel = GetNode<RootModel>("/root/Main/Models");

		_visibleIcon = LoadIcon("res://assets/icon/icon.svg", 24);

		// VisibleButton 列を固定幅にする
		SetColumnExpand((int)HierarchyTreeColumn.VisibleButton, false);
		SetColumnCustomMinimumWidth((int)HierarchyTreeColumn.VisibleButton, 24); // 24px など

		// ツリーの初期化
		AddToTree(_rootModel);
	}

	public override void _ExitTree()
	{
		// Tree選択イベントの購読解除
		MultiSelected -= OnMultiSelected;
		CellSelected -= OnCellSelected;

		// イベントの購読解除
		ModelEventHub.I.AddModelRequested -= OnAddModelRequested;
		ModelEventHub.I.ModelSelectionStateNotified -= OnModelSelectionStateNotified;
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
			// 選択されたアイテムに対応する AnyModel を選択状態にする
			Selection.Add(ModelBinder.GetModel(item));
		}
		else
		{
			// 選択が解除されたアイテムに対応する AnyModel を選択解除状態にする
			Selection.Remove(ModelBinder.GetModel(item));
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
				AnyModel model = ModelBinder.GetModel(item);
				if (model != null)
				{
					model.Visible = !model.Visible; // ノードの表示状態を切り替える
					item.SetIconModulate((int)HierarchyTreeColumn.VisibleButton, model.Visible ? new Color(1, 1, 1) : new Color(1, 1, 1, 0.5f)); // アイコンの見た目も更新する
				}
				break;
			default:
				break;
		}
	}

	/// <summary>
	/// モデルの追加がリクエストされたときのイベントハンドラ
	/// </summary>
	/// <param name="child">追加する子モデル</param>
	/// <param name="parent">追加先の親モデル</param>
	private void OnAddModelRequested(AnyModel child, AnyModel parent)
	{
		AddToTree(child, parent);
	}

	private void OnModelSelectionStateNotified(AnyModel model, bool isSelected)
	{
		TreeItem item = ModelBinder.GetItem(model);
		if (item != null)
		{
			if (isSelected)
			{
				item.Select((int)HierarchyTreeColumn.Name);
			}
			else
			{
				item.Deselect((int)HierarchyTreeColumn.Name);
			}
		}
	}

	#endregion

	/// <summary>
	/// AnyModel を TreeItem に追加する
	/// </summary>
	/// <param name="node">追加する AnyModel</param>
	/// <param name="parentTreeItem">親の TreeItem</param>
	private void AddToTree(AnyModel model, TreeItem parentTreeItem = null)
	{
		// ツリーにアイテムを追加。親が null の場合はルートアイテムとして追加される便利仕様
		TreeItem item = CreateItem(parentTreeItem);
		item.SetText((int)HierarchyTreeColumn.Name, model.Name);

		// 非表示切り替えのためのアイコンを設定
		item.SetCellMode((int)HierarchyTreeColumn.VisibleButton, TreeItem.TreeCellMode.Icon);
		item.SetIcon((int)HierarchyTreeColumn.VisibleButton, _visibleIcon);
		item.SetIconModulate((int)HierarchyTreeColumn.VisibleButton, model.Visible ? new Color(1, 1, 1) : new Color(1, 1, 1, 0.5f)); // 非表示なら半透明にする
		item.SetEditable((int)HierarchyTreeColumn.VisibleButton, true); // アイコンをクリックして編集可能にする

		// AnyModel と TreeItem の対応を登録
		ModelBinder.Bind(model, item);

		// 子ノードを再帰的に追加
		foreach (AnyModel childModel in model.ChildModels)
		{
			// AnyModel のみをツリーに追加する
			AddToTree(childModel, item);
		}
	}

	private void AddToTree(AnyModel childModel , AnyModel parentModel)
	{
		TreeItem parentTreeItem = ModelBinder.GetItem(parentModel);
		if (parentTreeItem != null)
		{
			AddToTree(childModel, parentTreeItem);
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