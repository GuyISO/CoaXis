using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Viewer全体のUIテーマを管理するハブ。
/// </summary>
public partial class UiThemeHub : BaseHub
{
	#region Fields

	/// <summary>ダークテーマのリソースパス。</summary>
	private const string DarkThemePath = "res://assets/theme/coaxis_dark.tres";

	/// <summary>ライトテーマのリソースパス。</summary>
	private const string LightThemePath = "res://assets/theme/coaxis_light.tres";

	/// <summary>ModelStatusに対応する色を保持するTheme内の型名。</summary>
	private const string ModelStatusThemeType = "CoaXisModelStatus";

	/// <summary>CoaXisブランドカラーを保持するTheme内の型名。</summary>
	private const string BrandThemeType = "CoaXisBrand";

	/// <summary>標準文字色を保持するTheme内の型名。</summary>
	private const string ControlThemeType = "Control";

	/// <summary>CoaXisプライマリカラーを保持するTheme内の項目名。</summary>
	private const string PrimaryColorName = "primary";

	/// <summary>Theme内でEnvironment用の色を保持する型名。</summary>
	private const string EnvironmentThemeType = "CoaXisEnvironment";

	/// <summary>Environmentの背景色を保持するTheme内の項目名。</summary>
	private const string EnvironmentBackgroundColorName = "background";

	/// <summary>Treeアイコン用の色を保持するTheme内の型名。</summary>
	private const string TreeIconThemeType = "CoaXisTreeIcon";

	/// <summary>テーマ識別子とThemeリソースパスの対応表。</summary>
	private static readonly Dictionary<string, string> ThemeResourcePaths = new()
	{
		[UiSettings.DarkThemeId] = DarkThemePath,
		[UiSettings.LightThemeId] = LightThemePath,
	};

	/// <summary>読み込んだテーマリソース。</summary>
	private readonly Dictionary<string, Theme> _themes = new();

	#endregion

	#region Properties

	/// <summary>現在適用中のテーマ識別子。</summary>
	public string CurrentThemeId { get; private set; } = UiSettings.DefaultThemeId;

	/// <summary>現在適用中のテーマリソース。</summary>
	public Theme CurrentTheme { get; private set; }

	/// <summary>現在のテーマに設定されたCoaXisプライマリカラー。</summary>
	public Color PrimaryColor => GetThemeColor(PrimaryColorName, BrandThemeType);

	/// <summary>現在のテーマに設定された標準文字色。</summary>
	public Color TextColor => GetThemeColor("font_color", ControlThemeType);

	/// <summary>現在のテーマに設定されたEnvironment背景色。</summary>
	public Color EnvironmentBackgroundColor => GetThemeColor(EnvironmentBackgroundColorName, EnvironmentThemeType);

	/// <summary>Treeアイコンの通常色。</summary>
	public Color TreeIconColor => GetThemeColor("normal", TreeIconThemeType);

	/// <summary>Treeアイコンの無効色。</summary>
	public Color TreeIconMutedColor => GetThemeColor("muted", TreeIconThemeType);

	/// <summary>Treeの非表示アイコンの色。</summary>
	public Color TreeVisibilityHiddenColor => GetThemeColor("visibility_hidden", TreeIconThemeType);

	/// <summary>Treeアイコンのバッジ背景色。</summary>
	public Color TreeIconBadgeBackgroundColor => GetThemeColor("badge_background", TreeIconThemeType);

	/// <summary>
	/// 指定されたモデル状態に対応する色を現在のテーマから取得する。
	/// </summary>
	/// <param name="status">色を取得するモデル状態。</param>
	/// <returns>現在のテーマに定義された状態色。</returns>
	public Color GetModelStatusColor(ModelStatus status)
	{
		return GetThemeColor(status.ToString(), ModelStatusThemeType);
	}

	#endregion

	#region Signals

	/// <summary>UIテーマが切り替わったことを通知する。</summary>
	[Signal] public delegate void ThemeChangedEventHandler();

	#endregion

	#region Lifecycle

	/// <summary>
	/// テーマリソースを読み込み、保存済みのテーマを適用する。
	/// </summary>
	public override void _Ready()
	{
		foreach (KeyValuePair<string, string> themeResource in ThemeResourcePaths)
		{
			_themes.Add(themeResource.Key, LoadTheme(themeResource.Value));
		}

		string themeId = Application.Setting.Current.Ui.ThemeId;
		if (!_themes.ContainsKey(themeId))
		{
			Application.Log.Warn(
				$"Settings: unknown theme identifier '{themeId}'. Falling back to '{UiSettings.DefaultThemeId}'.");
			themeId = UiSettings.DefaultThemeId;
			Application.Setting.SetTheme(themeId);
		}

		ApplyTheme(themeId);
	}

	#endregion

	#region Events

	#endregion

	#region Methods

	/// <summary>
	/// 指定テーマを適用して設定を保存する。
	/// </summary>
	/// <param name="themeId">適用するテーマの識別子。</param>
	/// <returns>設定を保存できた場合は true。</returns>
	public bool SetTheme(string themeId)
	{
		if (!_themes.ContainsKey(themeId))
		{
			throw new ArgumentException($"Unknown theme identifier: '{themeId}'.", nameof(themeId));
		}

		bool saved = Application.Setting.SetTheme(themeId);
		ApplyTheme(themeId);
		return saved;
	}

	/// <summary>
	/// 指定されたウィンドウに現在のテーマを適用する。
	/// </summary>
	/// <param name="window">テーマ適用先のウィンドウ。</param>
	public void ApplyTo(Window window)
	{
		if (window == null)
		{
			throw new ArgumentNullException(nameof(window));
		}

		window.Theme = CurrentTheme;
	}

	#endregion

	#region Helpers

	/// <summary>
	/// 現在のThemeから必須色を取得する。
	/// </summary>
	/// <param name="colorName">Theme内の色名。</param>
	/// <param name="themeType">色を定義したTheme型名。</param>
	/// <returns>Themeに定義された色。</returns>
	private Color GetThemeColor(string colorName, string themeType)
	{
		if (CurrentTheme == null)
		{
			throw new InvalidOperationException("The current UI theme has not been loaded.");
		}

		if (!CurrentTheme.HasColor(colorName, themeType))
		{
			throw new InvalidOperationException($"Theme color '{colorName}' is missing from '{themeType}'.");
		}

		return CurrentTheme.GetColor(colorName, themeType);
	}

	/// <summary>
	/// 選択テーマをViewport、メインウィンドウ、UIサブウィンドウへ適用する。
	/// </summary>
	/// <param name="themeId">適用するテーマの識別子。</param>
	private void ApplyTheme(string themeId)
	{
		CurrentThemeId = themeId;
		CurrentTheme = _themes[themeId];
		RenderingServer.SetDefaultClearColor(EnvironmentBackgroundColor);
		Window rootWindow = GetTree().Root;
		rootWindow.Theme = CurrentTheme;
		ApplyThemeToUiRoots(rootWindow);
		EmitSignal(SignalName.ThemeChanged);
	}

	/// <summary>
	/// SceneTree内のウィンドウと、親Controlを持たないUIルートへ現在のテーマを適用する。
	/// </summary>
	/// <param name="node">走査対象のノード。</param>
	private void ApplyThemeToUiRoots(Node node)
	{
		foreach (Node child in node.GetChildren())
		{
			if (child is Window window)
			{
				window.Theme = CurrentTheme;
			}
			else if (child is Control control && child.GetParent() is not Control)
			{
				control.Theme = CurrentTheme;
			}

			ApplyThemeToUiRoots(child);
		}
	}

	/// <summary>
	/// 指定パスのテーマを読み込む。
	/// </summary>
	/// <param name="path">テーマリソースのパス。</param>
	/// <returns>読み込んだテーマ。</returns>
	private static Theme LoadTheme(string path)
	{
		Theme theme = GD.Load<Theme>(path);
		if (theme == null)
		{
			throw new InvalidOperationException($"Theme resource could not be loaded: '{path}'.");
		}

		return theme;
	}

	#endregion
}
