using Godot;

/// <summary>
/// Viewerのライトテーマとダークテーマを切り替えるボタン。
/// </summary>
public partial class ToggleThemeButton : Button
{
	#region Fields

	[Export] private Texture2D _lightIcon;
	[Export] private Texture2D _darkIcon;

	#endregion

	#region Lifecycle

	/// <summary>
	/// 保存済みテーマをボタン状態へ反映して切替イベントを購読する。
	/// </summary>
	public override void _Ready()
	{
		ToggleMode = true;
		Toggled += OnToggled;
		Application.Ui.Theme.ThemeChanged += OnThemeChanged;
		OnThemeChanged();
	}

	/// <summary>
	/// 切替イベントの購読を解除する。
	/// </summary>
	public override void _ExitTree()
	{
		Toggled -= OnToggled;
		Application.Ui.Theme.ThemeChanged -= OnThemeChanged;

		base._ExitTree();
	}

	#endregion

	#region Events

	/// <summary>
	/// ボタン状態に応じてテーマを切り替える。
	/// </summary>
	/// <param name="useLightTheme">ライトテーマを使用する場合は true。</param>
	private void OnToggled(bool useLightTheme)
	{
		Application.Ui.Theme.SetTheme(
			useLightTheme ? UiSettings.LightThemeId : UiSettings.DarkThemeId);
	}

	/// <summary>
	/// 現在のテーマをボタン状態と表示に反映する。
	/// </summary>
	private void OnThemeChanged()
	{
		bool useLightTheme = Application.Ui.Theme.CurrentThemeId == UiSettings.LightThemeId;
		SetPressedNoSignal(useLightTheme);
		UpdateDisplay(useLightTheme);
	}

	#endregion

	#region Helpers

	/// <summary>
	/// 現在のテーマをボタンに表示する。
	/// </summary>
	/// <param name="useLightTheme">ライトテーマを使用する場合は true。</param>
	private void UpdateDisplay(bool useLightTheme)
	{
		Icon = useLightTheme ? _lightIcon : _darkIcon;
		TooltipText = useLightTheme
			? "ライトテーマを使用中です。クリックでダークテーマに切り替えます"
			: "ダークテーマを使用中です。クリックでライトテーマに切り替えます";
	}

	#endregion
}
