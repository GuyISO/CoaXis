using Godot;

/// <summary>
/// 環境設定をWorldEnvironmentに反映するためのコントローラ
/// </summary>
public partial class EnvironmentController : WorldEnvironment
{
	#region Lifecycle

	public override void _Ready()
	{
		ApplySettings();
	}

	#endregion

	#region Helpers

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

		ColorSettings c = Application.Setting.Current.Color;
		env.BackgroundColor = Color.FromHtml(c.EnvironmentBackgroundColor);
	}

	#endregion
}