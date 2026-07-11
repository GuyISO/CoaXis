using Godot;
using System;

/// <summary>
/// モデルの操作を行う Autoload ノード
/// </summary>
public partial class ModelOperationService : Node
{
	#region Fields

	// 選択操作モードの現在値を保持するフィールド、初期値は選択操作とする
	private static PickHandlingMode _currentPickHandlingMode = PickHandlingMode.Selection;

	#endregion

	#region Lifecycle

	public override void _Ready()
	{
		// イベントの購読開始
		Application.Model.ToggleModelVisibilityRequested += OnToggleModelVisibilityRequested;
		Application.Pick.NotifyPickHandlingModeRequested += OnNotifyPickHandlingModeRequested;
		Application.Pick.PickHandlingModeNotified += OnPickHandlingModeNotified;
	}

	public override void _ExitTree()
	{
		// イベントの購読解除
		Application.Model.ToggleModelVisibilityRequested -= OnToggleModelVisibilityRequested;
		Application.Pick.NotifyPickHandlingModeRequested -= OnNotifyPickHandlingModeRequested;
		Application.Pick.PickHandlingModeNotified -= OnPickHandlingModeNotified;

		base._ExitTree();
	}

	#endregion

	#region Events

	/// <summary>
	/// モデルの表示状態切替がリクエストされたときに呼び出されるイベントハンドラ
	/// </summary>
	/// <param name="model">表示状態を切り替えるモデル</param>
	private void OnToggleModelVisibilityRequested(AnyModel model)
	{
		var command = new SetModelVisibilityCommand([model], !model.Visible);
		UndoService.Execute(command);
	}

	/// <summary>
	/// 選択操作モードの通知がリクエストされたときに呼び出されるイベントハンドラ
	/// </summary>
	private void OnNotifyPickHandlingModeRequested()
	{
		Application.Pick.NotifyPickHandlingMode(_currentPickHandlingMode);
	}

	/// <summary>
	/// 選択操作モードが通知されたときに呼び出されるイベントハンドラ
	/// </summary>
	/// <param name="mode">通知された選択操作モード</param>
	private void OnPickHandlingModeNotified(PickHandlingMode mode)
	{
		_currentPickHandlingMode = mode;
	}

	#endregion
}
