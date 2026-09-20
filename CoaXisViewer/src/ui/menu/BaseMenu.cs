using Godot;

/// <summary>
/// OSネイティブメニュー(NativeMenu)を用いたコンテキストメニューの基底クラス
/// </summary>
/// <remarks>
/// ネイティブメニュー Rid の生成・解放、表示可否判定、Popup 呼び出しといった各Menuに共通する処理をここへ集約する。
/// 派生クラスは <see cref="BuildMenuItems(Rid)"/> でメニュー項目の定義のみを行えばよい。
/// </remarks>
public abstract partial class BaseMenu : Node
{
	#region Fields

	private Rid _nativeMenu;
	private bool _hasNativeMenu;

	#endregion

	#region Lifecycle

	public override void _Ready()
	{
		EnsureNativeMenu();
	}

	public override void _ExitTree()
	{
		if (_hasNativeMenu)
		{
			NativeMenu.Singleton.FreeMenu(_nativeMenu);
			_hasNativeMenu = false;
			_nativeMenu = default;
		}

		base._ExitTree();
	}

	#endregion

	#region Protected API

	/// <summary>
	/// 派生クラス固有のメニュー項目を構築する
	/// </summary>
	/// <param name="nativeMenu">項目追加対象のネイティブメニュー Rid</param>
	/// <remarks>NativeMenu.Singleton.CreateMenu 直後に一度だけ呼び出される。</remarks>
	protected abstract void BuildMenuItems(Rid nativeMenu);

	/// <summary>
	/// 現在のマウス位置にネイティブメニューを表示する
	/// </summary>
	protected void PopupNativeMenu()
	{
		if (!EnsureNativeMenu())
		{
			return;
		}

		// NativeMenu.PopupはOS画面座標を要求するため、メイン/フローティングウィンドウを問わず実マウス座標を使う。
		NativeMenu.Singleton.Popup(_nativeMenu, DisplayServer.MouseGetPosition());
	}

	#endregion

	#region Internal Helpers

	/// <summary>
	/// 生成済みのネイティブメニュー Rid を取得する、未生成の場合は生成を試みる
	/// </summary>
	/// <param name="nativeMenu">取得できた場合はネイティブメニューの Rid</param>
	/// <returns>取得できた場合は true</returns>
	/// <remarks>PickResultMenu が ModelEntityMenu をサブメニューとして接続する場合など、子メニューの Rid を必要とする場面で使う。</remarks>
	internal bool TryGetNativeMenu(out Rid nativeMenu)
	{
		if (!EnsureNativeMenu())
		{
			nativeMenu = default;
			return false;
		}

		nativeMenu = _nativeMenu;
		return true;
	}

	private bool EnsureNativeMenu()
	{
		if (_hasNativeMenu)
		{
			return true;
		}

		if (!NativeMenu.Singleton.HasFeature(NativeMenu.Feature.PopupMenu))
		{
			GD.PushError($"{Name}: NativeMenu.Feature.PopupMenu is not supported by the current display server.");
			return false;
		}

		_nativeMenu = NativeMenu.Singleton.CreateMenu();
		BuildMenuItems(_nativeMenu);
		_hasNativeMenu = true;
		return true;
	}

	#endregion
}
