using Godot;
using System.Collections.Generic;

/// <summary>
/// AxisNavigator を操作対象とするネイティブコンテキストメニュー
/// </summary>
/// <remarks>
/// 上下左右前後の6方向への視点切替、ロール回転、投影方式の切替を扱う。
	/// AxisNavigator は特定の ModelEntity/PickResult を対象にしないため、表示メソッドのみを公開する。
/// ネイティブメニュー Rid の生成・解放は基底クラスの <see cref="BaseMenu"/> が担う。
/// </remarks>
public partial class AxisNavigatorMenu : BaseMenu
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

	#region Public Methods

	/// <summary>
	/// 現在のマウス位置にネイティブコンテキストメニューを表示する
	/// </summary>
	public void ShowAtPosition()
	{
		PopupNativeMenu();
	}

	#endregion

	#region Protected API

	/// <inheritdoc/>
	protected override void BuildMenuItems(Rid nativeMenu)
	{
		NativeMenu.Singleton.AddItem(nativeMenu, "Top", Callable.From<Variant>(_ => HandleViewMenuItemPressed(MenuItemId.ViewTop)));
		NativeMenu.Singleton.AddItem(nativeMenu, "Bottom", Callable.From<Variant>(_ => HandleViewMenuItemPressed(MenuItemId.ViewBottom)));
		NativeMenu.Singleton.AddItem(nativeMenu, "Left", Callable.From<Variant>(_ => HandleViewMenuItemPressed(MenuItemId.ViewLeft)));
		NativeMenu.Singleton.AddItem(nativeMenu, "Right", Callable.From<Variant>(_ => HandleViewMenuItemPressed(MenuItemId.ViewRight)));
		NativeMenu.Singleton.AddItem(nativeMenu, "Front", Callable.From<Variant>(_ => HandleViewMenuItemPressed(MenuItemId.ViewFront)));
		NativeMenu.Singleton.AddItem(nativeMenu, "Back", Callable.From<Variant>(_ => HandleViewMenuItemPressed(MenuItemId.ViewBack)));
		NativeMenu.Singleton.AddSeparator(nativeMenu);
		NativeMenu.Singleton.AddItem(nativeMenu, "RollLeft", Callable.From<Variant>(_ => HandleRollMenuItemPressed(-90f)));
		NativeMenu.Singleton.AddItem(nativeMenu, "RollRight", Callable.From<Variant>(_ => HandleRollMenuItemPressed(90f)));
		NativeMenu.Singleton.AddSeparator(nativeMenu);
		NativeMenu.Singleton.AddItem(nativeMenu, "ToggleProjection", Callable.From<Variant>(HandleToggleProjectionMenuItemPressed));
	}

	#endregion

	#region Internal Helpers

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

	// NativeMenuのcallbackはtagに渡した値をVariantとして1引数で受け取る契約のため、未使用でも引数を受け取る
	private void HandleToggleProjectionMenuItemPressed(Variant tag)
	{
		Application.Viewport.Event.ToggleProjectionType();
	}

	#endregion
}
