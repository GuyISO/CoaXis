using Godot;
using System.Collections.Generic;

/// <summary>
/// AxisNavigator を操作対象とするコンテキストメニュー
/// </summary>
/// <remarks>
/// 上下左右前後の6方向への視点切替、ロール回転、投影方式の切替を扱う
/// AxisNavigator は特定の ModelEntity/PickResult を対象にしないため、ShowAtPosition のみを公開する
/// </remarks>
public partial class AxisNavigatorMenu : PopupMenu
{
	/// <summary>
	/// コンテキストメニューの項目を識別する ID
	/// </summary>
	private enum MenuItemId
	{
		ViewTop,
		ViewBottom,
		ViewLeft,
		ViewRight,
		ViewFront,
		ViewBack,
		RollLeft,
		RollRight,
		ToggleProjection,
	}

	// 各視点方向に対応する回転角度(度)。AxisNavigator.ViewLookAt が解釈するノード名の値と一致させている
	private static readonly Dictionary<MenuItemId, Vector3> ViewRotationDegrees = new()
	{
		{ MenuItemId.ViewTop, new Vector3(-90, 0, 0) },
		{ MenuItemId.ViewBottom, new Vector3(90, 180, 0) },
		{ MenuItemId.ViewLeft, new Vector3(0, -90, 0) },
		{ MenuItemId.ViewRight, new Vector3(0, 90, 0) },
		{ MenuItemId.ViewFront, new Vector3(0, 180, 0) },
		{ MenuItemId.ViewBack, new Vector3(0, 0, 0) },
	};

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
	/// 指定した OS 画面座標にコンテキストメニューを表示する
	/// </summary>
	/// <param name="screenPosition">メニューを表示する OS 画面座標</param>
	public void ShowAtPosition(Vector2I screenPosition)
	{
		Position = screenPosition;
		Popup();
	}

	#endregion

	#region Internal Helpers

	private void InitializeItems()
	{
		AddItem("Top", (int)MenuItemId.ViewTop);
		AddItem("Bottom", (int)MenuItemId.ViewBottom);
		AddItem("Left", (int)MenuItemId.ViewLeft);
		AddItem("Right", (int)MenuItemId.ViewRight);
		AddItem("Front", (int)MenuItemId.ViewFront);
		AddItem("Back", (int)MenuItemId.ViewBack);
		AddSeparator();
		AddItem("RollLeft", (int)MenuItemId.RollLeft);
		AddItem("RollRight", (int)MenuItemId.RollRight);
		AddSeparator();
		AddItem("ToggleProjection", (int)MenuItemId.ToggleProjection);
	}

	private void OnIdPressed(long id)
	{
		MenuItemId menuItemId = (MenuItemId)id;
		switch (menuItemId)
		{
			case MenuItemId.RollLeft:
				HandleRollMenuItemPressed(-90f);
				return;
			case MenuItemId.RollRight:
				HandleRollMenuItemPressed(90f);
				return;
			case MenuItemId.ToggleProjection:
				Application.Viewport.Event.ToggleProjectionType();
				return;
		}

		HandleViewMenuItemPressed(menuItemId);
	}

	private void HandleViewMenuItemPressed(MenuItemId menuItemId)
	{
		if (!ViewRotationDegrees.TryGetValue(menuItemId, out Vector3 rotationDegrees))
		{
			return;
		}

		Quaternion rotation = Quaternion.FromEuler(rotationDegrees * (Mathf.Pi / 180f));
		Application.Viewport.Event.MoveRotationTo(rotation, true);
	}

	private void HandleRollMenuItemPressed(float degrees)
	{
		Quaternion rotation = new Quaternion(Vector3.Forward, Mathf.DegToRad(degrees));
		Application.Viewport.Event.Rotate(rotation, SpaceMode.FocalPoint, true);
	}

	#endregion
}

