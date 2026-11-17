using Godot;
using System;

/// <summary>
/// 環境設定をWorldEnvironmentに反映するためのコントローラ
/// </summary>
public partial class EnvironmentController : WorldEnvironment
{
	#region Lifecycle

	public override void _Ready()
	{
		SubscribeThemeEvents();
		ApplyTheme();
	}

	public override void _ExitTree()
	{
		UnsubscribeThemeEvents();

		base._ExitTree();
	}

	#endregion

	#region Events

	/// <summary>
	/// UIテーマ変更通知の購読を開始する。
	/// </summary>
	private void SubscribeThemeEvents()
	{
		Application.Ui.Theme.ThemeChanged += OnThemeChanged;
	}

	/// <summary>
	/// UIテーマ変更通知の購読を解除する。
	/// </summary>
	private void UnsubscribeThemeEvents()
	{
		Application.Ui.Theme.ThemeChanged -= OnThemeChanged;
	}

	/// <summary>
	/// UIテーマが変更されたとき、Environment背景色を更新する。
	/// </summary>
	private void OnThemeChanged()
	{
		ApplyTheme();
	}

	#endregion

	#region Helpers

	/// <summary>
	/// Theme の背景色と Settings の Glow 設定を WorldEnvironment へ反映する。
	/// </summary>
	private void ApplyTheme()
	{
		Godot.Environment env = Environment;
		if (env == null)
		{
			throw new InvalidOperationException("WorldEnvironment has no Environment resource.");
		}

		env.BackgroundColor = Application.Ui.Theme.EnvironmentBackgroundColor;
		env.GlowEnabled = Application.Setting.Current.Environment.GlowEnabled;
	}

	#endregion
}