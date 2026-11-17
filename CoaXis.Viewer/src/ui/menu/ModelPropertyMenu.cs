using System;
using Godot;

/// <summary>
/// ModelProperty を操作対象とするネイティブコンテキストメニュー
/// </summary>
/// <remarks>
/// ModelPropertyTree の下階層（プロパティ行）を右クリックした際に表示される。
/// プロパティ値のコピーと、所属する ModelEntity のコンテキストメニューを提供する。
/// ネイティブメニュー Rid の生成・解放は基底クラスの <see cref="BaseMenu"/> が担う。
/// </remarks>
public partial class ModelPropertyMenu : BaseMenu
{
	#region Fields

	private string _targetValue = string.Empty;
	private ModelEntityMenu _modelEntityMenu;
	private int _modelEntitySubmenuItemIndex;

	#endregion

	#region Lifecycle

	public override void _Ready()
	{
		// BuildMenuItemsでEntityメニューのRidを参照するため、base._Ready()より先に子として生成する。
		_modelEntityMenu = new ModelEntityMenu { Name = "ModelEntityMenu" };
		AddChild(_modelEntityMenu);

		base._Ready();
	}

	#endregion

	#region Methods

	/// <summary>
	/// 指定した値と所属Entityを対象にネイティブコンテキストメニューを表示する
	/// </summary>
	/// <param name="value">操作対象とするプロパティ値</param>
	/// <param name="entityId">操作対象プロパティが所属するModelEntityの識別子</param>
	public void Show(string value, Guid entityId = default)
	{
		if (!TryGetNativeMenu(out Rid nativeMenu))
		{
			return;
		}

		_targetValue = value ?? string.Empty;
		_modelEntityMenu.SetTargetEntity(entityId);
		bool hasEntity = entityId != Guid.Empty
			&& Application.Model.Entity.Registry.Get(entityId) != null;
		NativeMenu.Singleton.SetItemDisabled(nativeMenu, _modelEntitySubmenuItemIndex, !hasEntity);
		PopupNativeMenu();
	}

	#endregion

	#region Helpers

	/// <summary>
	/// ModelPropertyMenu のネイティブメニュー項目を構築する
	/// </summary>
	/// <param name="nativeMenu">ネイティブメニューの Rid</param>
	protected override void BuildMenuItems(Rid nativeMenu)
	{
		if (_modelEntityMenu == null || !_modelEntityMenu.TryGetNativeMenu(out Rid modelEntityNativeMenu))
		{
			GD.PushError($"{Name}: ModelEntityMenu native submenu could not be created.");
			return;
		}

		NativeMenu.Singleton.AddItem(nativeMenu, "CopyValueToClipboard", Callable.From<Variant>(HandleCopyValueToClipboardMenuItemPressed));
		_modelEntitySubmenuItemIndex = NativeMenu.Singleton.AddSubmenuItem(
			nativeMenu, "ModelEntity", modelEntityNativeMenu);
	}

	/// <summary>
	/// クリップボードに値をコピーするメニュー項目が選択された際の処理
	/// </summary>
	/// <param name="tag">NativeMenuのcallbackはtagに渡した値をVariantとして1引数で受け取る契約のため、未使用でも引数を受け取る</param>
	private void HandleCopyValueToClipboardMenuItemPressed(Variant tag)
	{
		DisplayServer.ClipboardSet(_targetValue);
	}

	#endregion
}
