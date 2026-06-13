using Godot;

/// <summary>
/// シーン管理用のシングルトンクラスです。AutoLoadノードとしてシーンツリーに配置します。
/// </summary>
public partial class SceneManager : Node
{
	#region Fields

	private Window _cameraWindow;

	#endregion

	#region Properties

	/// <summary>
	/// シングルトン参照です。
	/// </summary>
	public static SceneManager I { get; private set; }

	#endregion

	#region Lifecycle

	/// <summary>
	/// シーンツリー参加時にシングルトン参照を確立します。
	/// </summary>
	public override void _EnterTree()
	{
		// AutoLoad をデフォルト参照として維持するため、未設定時のみ I を確立する。
		if (I == null)
		{
			I = this;
		}
	}

	#endregion

	#region Public API

	/// <summary>
	/// 指定されたコマンド名に対応する処理を実行します。
	/// </summary>
	/// <param name="commandName">実行するコマンドの名前</param>
	public void ExecuteCommand(string commandName)
	{
		// コマンドの実行ロジックをここに実装
		ShowCameraWindow();
	}

	public void ShowCameraWindow()
	{

		if (_cameraWindow == null)
		{
			var packed = ResourceLoader.Load<PackedScene>("res://scenes/ui/CameraWindow.tscn");
			_cameraWindow = packed.Instantiate<Window>();

			// OSウィンドウとして追加
			GetTree().Root.AddChild(_cameraWindow);

			// Xボタンで閉じたときの処理
			_cameraWindow.CloseRequested += () =>
			{
				_cameraWindow.Hide();
			};
		}

		_cameraWindow.Show();
		// _cameraWindow.MoveToFront();

	}

	#endregion

}