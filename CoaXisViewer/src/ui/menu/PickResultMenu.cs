using Godot;
using System;

/// <summary>
/// PickResult（ビューポート上のレイキャスト結果）を操作対象とするネイティブコンテキストメニュー
/// </summary>
/// <remarks>
/// ModelEntityMenu をネイティブサブメニューとして内包し、対象モデルへの操作はそちらへ委譲する。
/// 本メニュー自身は PickResult 由来の操作（法線方向へのカメラ整列など）のみを扱う。
/// ネイティブメニュー Rid の生成・解放は基底クラスの <see cref="BaseMenu"/> が担う。
/// </remarks>
public partial class PickResultMenu : BaseMenu
{
	#region Fields

	private PickResult _pickResult;
	private ModelEntityMenu _modelEntityMenu;
	private int _modelEntitySubmenuItemIndex;
	private int _alignNormalMenuItemIndex;

	#endregion

	#region Lifecycle

	public override void _Ready()
	{
		// BuildMenuItemsでModelEntityMenuのネイティブメニューRidを参照するため、base._Ready()より先に子として生成しておく
		_modelEntityMenu = new ModelEntityMenu { Name = "ModelEntityMenu" };
		AddChild(_modelEntityMenu);

		base._Ready();
	}

	#endregion

	#region Public API

	/// <summary>
	/// 指定した PickResult を対象にネイティブコンテキストメニューを表示する
	/// </summary>
	/// <param name="pickResult">右クリック位置で PickByRay したピック結果</param>
	public void ShowForPickResult(PickResult pickResult)
	{
		if (!TryGetNativeMenu(out Rid nativeMenu))
		{
			return;
		}

		_pickResult = pickResult;
		_modelEntityMenu.SetTargetEntity(pickResult != null ? pickResult.EntityId : Guid.Empty);

		// ヒット結果に依存する項目は、無効な対象へ操作を実行できないよう表示のたびに状態を更新する。
		NativeMenu.Singleton.SetItemDisabled(nativeMenu, _modelEntitySubmenuItemIndex, pickResult == null || pickResult.EntityId == Guid.Empty);
		NativeMenu.Singleton.SetItemDisabled(nativeMenu, _alignNormalMenuItemIndex, pickResult == null || !pickResult.HasHit);
		PopupNativeMenu();
	}

	#endregion

	#region Protected API

	/// <inheritdoc/>
	protected override void BuildMenuItems(Rid nativeMenu)
	{
		if (_modelEntityMenu == null || !_modelEntityMenu.TryGetNativeMenu(out Rid modelEntityNativeMenu))
		{
			GD.PushError($"{Name}: ModelEntityMenu native submenu could not be created.");
			return;
		}

		_modelEntitySubmenuItemIndex = NativeMenu.Singleton.AddSubmenuItem(nativeMenu, "ModelEntity", modelEntityNativeMenu);
		_alignNormalMenuItemIndex = NativeMenu.Singleton.AddItem(nativeMenu, "AlignNormal", Callable.From<Variant>(HandleAlignNormalMenuItemPressed));
	}

	#endregion

	#region Internal Helpers

	// NativeMenuのcallbackはtagに渡した値をVariantとして1引数で受け取る契約のため、未使用でも引数を受け取る
	private void HandleAlignNormalMenuItemPressed(Variant tag)
	{
		if (_pickResult == null || !_pickResult.HasHit)
		{
			return;
		}

		Application.Viewport.Event.AlignNormalTo(_pickResult.Normal, true);
	}

	#endregion
}
