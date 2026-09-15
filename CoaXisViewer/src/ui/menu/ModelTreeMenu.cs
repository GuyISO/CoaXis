using Godot;
using System;

/// <summary>
/// モデルツリーのコンテキストメニューを管理する UI コンポーネント
/// </summary>
public partial class ModelTreeMenu : PopupMenu
{
	/// <summary>
	/// コンテキストメニューの項目を識別する ID
	/// </summary>
	public enum ContextMenuItemId
	{
		Add = 0,
		Delete = 1,
		Rename = 2,
		Fit = 3,
		Emphasize = 4,
		Spin = 5,
	}

	#region Fields

	private TreeItem _targetItem;

	#endregion

	#region Lifecycle

	public override void _Ready()
	{
		InitializeItems();
		IdPressed += OnIdPressed;
	}

	public override void _ExitTree()
	{
		IdPressed -= OnIdPressed;

		base._ExitTree();
	}

	#endregion

	#region Public Methods

	/// <summary>
	/// 指定した TreeItem を対象にコンテキストメニューを表示する
	/// </summary>
	/// <param name="targetItem">右クリックされた TreeItem</param>
	/// <param name="screenPosition">メニューを表示する OS 画面座標</param>
	public void ShowForItem(TreeItem targetItem, Vector2I screenPosition)
	{
		_targetItem = targetItem;
		Position = screenPosition;

		// すでに表示中なら再オープンせず、右クリック位置の更新だけで追従させる。
		if (!Visible)
		{
			Popup();
		}
	}

	#endregion

	#region Internal Helpers

	private void InitializeItems()
	{
		AddItem("追加", (int)ContextMenuItemId.Add);
		AddItem("削除", (int)ContextMenuItemId.Delete);
		AddSeparator();
		AddItem("名前変更", (int)ContextMenuItemId.Rename);
		AddSeparator();
		AddItem("フィット", (int)ContextMenuItemId.Fit);
		AddItem("強調表示", (int)ContextMenuItemId.Emphasize);
		AddItem("回転アピール", (int)ContextMenuItemId.Spin);
	}

	private void OnIdPressed(long id)
	{
		switch ((ContextMenuItemId)id)
		{
			case ContextMenuItemId.Add:
				GD.Print("追加");
				break;
			case ContextMenuItemId.Delete:
				GD.Print("削除");
				break;
			case ContextMenuItemId.Rename:
				GD.Print("名前変更");
				break;
			case ContextMenuItemId.Fit:
				HandleFitMenuItemPressed();
				break;
			case ContextMenuItemId.Emphasize:
				HandleEmphasizeMenuItemPressed();
				break;
			case ContextMenuItemId.Spin:
				HandleSpinMenuItemPressed();
				break;
		}
	}

	private void HandleFitMenuItemPressed()
	{
		ModelNode modelNode = GetModelNode();
		if (modelNode == null)
		{
			return;
		}

		Node3D[] fitTargetNodes = new Node3D[] { modelNode };
		Application.Viewport.Event.Fit(fitTargetNodes, true);
	}

	private void HandleEmphasizeMenuItemPressed()
	{
		GetModelNode()?.Emphasize();
	}

	private void HandleSpinMenuItemPressed()
	{
		GetModelNode()?.SpinAppeal();
	}

	private ModelNode GetModelNode()
	{
		Guid entityId = TryGetEntityId(_targetItem);
		if (entityId == Guid.Empty)
		{
			return null;
		}

		return Application.Model.Registry.GetEntity(entityId)?.Node;
	}

	private static Guid TryGetEntityId(TreeItem item)
	{
		if (item == null)
		{
			return Guid.Empty;
		}

		Variant entityIdVariant = item.GetMeta("EntityId", Variant.CreateFrom(string.Empty));
		string entityIdText = entityIdVariant.AsString();
		return Guid.TryParse(entityIdText, out Guid entityId) ? entityId : Guid.Empty;
	}

	#endregion
}
