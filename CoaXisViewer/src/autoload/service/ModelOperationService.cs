using Godot;
using System;

/// <summary>
/// モデルの操作を行うサービス、Autoload でシングルトン化される
/// </summary>
public partial class ModelOperationService : AutoloadNodeBase<ModelOperationService>
{
	#region Lifecycle

	public override void _Ready()
	{
		// イベントの購読開始
		ModelEventHub.Instance.ToggleModelVisibilityRequested += OnToggleModelVisibilityRequested;
		PickEventHub.Instance.PickHandlingModeNotified += OnPickHandlingModeNotified;
	}

	public override void _ExitTree()
	{
		// イベントの購読解除
		ModelEventHub.Instance.ToggleModelVisibilityRequested -= OnToggleModelVisibilityRequested;
		PickEventHub.Instance.PickHandlingModeNotified -= OnPickHandlingModeNotified;

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
	/// 選択操作モードが通知されたときに呼び出されるイベントハンドラ
	/// </summary>
	/// <param name="mode">通知された選択操作モード</param>
	private void OnPickHandlingModeNotified(PickHandlingMode mode)
	{
		// モードに応じた処理をここに追加
	}

	#endregion
}
