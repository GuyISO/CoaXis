using Godot;

/// <summary>
/// ModelProperty を操作対象とするネイティブコンテキストメニュー
/// </summary>
/// <remarks>
/// ModelPropertyTree の下階層（プロパティ行）を右クリックした際に表示される。
/// メニュー項目は今後拡張予定のため、現時点では値のクリップボードコピーのみを扱う。
/// ネイティブメニュー Rid の生成・解放は基底クラスの <see cref="BaseMenu"/> が担う。
/// </remarks>
public partial class ModelPropertyMenu : BaseMenu
{
	#region Fields

	private string _targetValue = string.Empty;

	#endregion

	#region Public API

	/// <summary>
	/// 指定した値を対象にネイティブコンテキストメニューを表示する
	/// </summary>
	/// <param name="value">操作対象とするプロパティ値</param>
	/// <param name="screenPosition">メニューを表示する OS 画面座標</param>
	public void ShowForValue(string value, Vector2I screenPosition)
	{
		_targetValue = value ?? string.Empty;
		PopupNativeMenu(screenPosition);
	}

	#endregion

	#region Protected API

	/// <inheritdoc/>
	protected override void BuildMenuItems(Rid nativeMenu)
	{
		NativeMenu.Singleton.AddItem(nativeMenu, "CopyValueToClipboard", Callable.From<Variant>(HandleCopyValueToClipboardMenuItemPressed));
	}

	#endregion

	#region Internal Helpers

	// NativeMenuのcallbackはtagに渡した値をVariantとして1引数で受け取る契約のため、未使用でも引数を受け取る
	private void HandleCopyValueToClipboardMenuItemPressed(Variant tag)
	{
		DisplayServer.ClipboardSet(_targetValue);
	}

	#endregion
}
