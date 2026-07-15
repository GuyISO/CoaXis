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
		SubscribeApplicationEvents();
	}

	public override void _ExitTree()
	{
		UnsubscribeApplicationEvents();

		base._ExitTree();
	}

	#endregion

	#region Internal Helpers

	/// <summary>
	/// Applicationイベントの購読を開始する
	/// </summary>
	private void SubscribeApplicationEvents()
	{
		Application.Model.Event.ToggleModelVisibilityRequested += OnToggleModelVisibilityRequested;
	}

	/// <summary>
	/// Applicationイベントの購読を解除する
	/// </summary>
	private void UnsubscribeApplicationEvents()
	{
		Application.Model.Event.ToggleModelVisibilityRequested -= OnToggleModelVisibilityRequested;
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
