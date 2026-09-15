using Godot;

/// <summary>
/// 環境設定をWorldEnvironmentに反映するためのコントローラ
/// </summary>
public partial class EnvironmentController : WorldEnvironment
{
	#region Lifecycle

	public override void _Ready()
	{
		SubscribeApplicationEvents();
		ApplySettings();
	}

	public override void _ExitTree()
	{
		UnsubscribeApplicationEvents();

		base._ExitTree();
	}

	#endregion

	#region Events

	/// <summary>
	/// Applicationイベントの購読を開始する
	/// </summary>
	private void SubscribeApplicationEvents()
	{
		Application.Setting.Event.SettingsNotified += ApplySettings;
	}

	/// <summary>
	/// Applicationイベントの購読を解除する
	/// </summary>
	private void UnsubscribeApplicationEvents()
	{
		Application.Setting.Event.SettingsNotified -= ApplySettings;
	}

	/// <summary>
	/// 設定値を反映する
	/// </summary>
	private void ApplySettings()
	{
		Environment env = Environment;
		if (env == null)
		{
			return;
		}

		ColorSettings c = Application.Setting.Service.Current.Color;
		env.BackgroundColor = Color.FromHtml(c.EnvironmentBackgroundColor);
	}

	#endregion
}