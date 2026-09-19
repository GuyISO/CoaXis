using Godot;
using System;

/// <summary>
/// PickResult（ビューポート上のレイキャスト結果）を操作対象とするコンテキストメニュー
/// </summary>
/// <remarks>
/// ModelEntityMenu をサブメニューとして内包し、対象モデルへの操作はそちらへ委譲する
/// 本メニュー自身は PickResult 由来の操作（法線方向へのカメラ整列など）のみを扱う
/// </remarks>
public partial class PickResultMenu : PopupMenu
{
	/// <summary>
	/// コンテキストメニューの項目を識別する ID
	/// </summary>
	private enum MenuItemId
	{
		AlignNormal,
	}

	#region Fields

	private PickResult _pickResult;
	private ModelEntityMenu _modelEntityMenu;
	private int _modelEntitySubmenuItemIndex;

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
	/// 指定した PickResult を対象にコンテキストメニューを表示する
	/// </summary>
	/// <param name="pickResult">右クリック位置で PickByRay したピック結果</param>
	/// <param name="screenPosition">メニューを表示する OS 画面座標</param>
	public void ShowForPickResult(PickResult pickResult, Vector2I screenPosition)
	{
		_pickResult = pickResult;
		_modelEntityMenu.SetTargetEntity(pickResult != null ? pickResult.EntityId : Guid.Empty);

		// ヒットしたModelEntityが無い場合はModelEntity向け操作を、法線が取れない場合はAlignNormalを無効化する
		SetItemDisabled(_modelEntitySubmenuItemIndex, pickResult == null || pickResult.EntityId == Guid.Empty);
		SetItemDisabled(GetItemIndex((int)MenuItemId.AlignNormal), pickResult == null || !pickResult.HasHit);

		Position = screenPosition;

		Popup();
	}

	#endregion

	#region Internal Helpers

	private void InitializeItems()
	{
		_modelEntityMenu = new ModelEntityMenu { Name = "ModelEntityMenu" };
		AddChild(_modelEntityMenu);
		AddSubmenuNodeItem("ModelEntity", _modelEntityMenu);
		_modelEntitySubmenuItemIndex = ItemCount - 1;

		AddItem("AlignNormal", (int)MenuItemId.AlignNormal);
	}

	private void OnIdPressed(long id)
	{
		switch ((MenuItemId)id)
		{
			case MenuItemId.AlignNormal:
				HandleAlignNormalMenuItemPressed();
				break;
		}
	}

	private void HandleAlignNormalMenuItemPressed()
	{
		if (_pickResult == null || !_pickResult.HasHit)
		{
			return;
		}

		Application.Viewport.Event.AlignNormalTo(_pickResult.Normal, true);
	}

	#endregion
}
