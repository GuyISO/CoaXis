using Godot;
using System;

/// <summary>
/// モデルの操作を行う Autoload ノード
/// </summary>
public partial class ModelOperationService : Node
{
	#region Lifecycle

	public override void _Ready()
	{
		// イベントの購読開始
		Application.Model.Event.ToggleModelVisibilityRequested += OnToggleModelVisibilityRequested;
	}

	public override void _ExitTree()
	{
		// イベントの購読解除
		Application.Model.Event.ToggleModelVisibilityRequested -= OnToggleModelVisibilityRequested;

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

	#endregion
}
