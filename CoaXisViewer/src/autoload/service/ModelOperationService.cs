using Godot;
using System;

/// <summary>
/// モデルの操作を行うサービス、Autoload でシングルトン化される
/// </summary>
public partial class ModelOperationService : Node
{
	#region Properties

	public static ModelOperationService Instance { get; private set; }

	#endregion

	#region Lifecycle

	public override void _EnterTree()
	{
		Instance = this;
	}

	public override void _Ready()
	{
		// イベントの購読開始
		ModelEventHub.Instance.ToggleModelVisibilityRequested += OnToggleModelVisibilityRequested;
	}

	public override void _ExitTree()
	{
		// イベントの購読解除
		ModelEventHub.Instance.ToggleModelVisibilityRequested -= OnToggleModelVisibilityRequested;

		Instance = null;
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
