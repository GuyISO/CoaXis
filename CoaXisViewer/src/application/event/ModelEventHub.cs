using Godot;

/// <summary>
/// モデル関連のイベント集約ハブ
/// </summary>
public partial class ModelEventHub : EventHubBase<ModelEventHub>
{
	#region --------------------------------------- Request ---------------------------------------

	[Signal] public delegate void NotifyRootModelRequestedEventHandler();
	/// <summary>
	/// ルートモデルに対して通知をリクエストする
	/// </summary>
	internal static void RequestNotifyRootModel()
	{
		TryEmitSignal(SignalName.NotifyRootModelRequested);
	}

	[Signal] public delegate void SetMultiSelectionModeRequestedEventHandler(bool enable);
	/// <summary>
	/// 複数選択モードの設定をリクエストする
	/// </summary>
	/// <param name="enable">複数選択モードを有効にする場合はtrue、無効にする場合はfalse</param>
	internal static void RequestSetMultiSelectionMode(bool enable)
	{
		TryEmitSignal(SignalName.SetMultiSelectionModeRequested, enable);
	}

	[Signal] public delegate void ClearSelectionRequestedEventHandler();
	/// <summary>
	/// 選択のクリアをリクエストする
	/// </summary>
	internal static void RequestClearSelection()
	{
		TryEmitSignal(SignalName.ClearSelectionRequested);
	}

	[Signal] public delegate void ToggleModelVisibilityRequestedEventHandler(AnyModel model);
	/// <summary>
	/// モデルの表示/非表示切替をリクエストする
	/// </summary>
	/// <param name="model">切替対象のモデル</param>
	internal static void RequestToggleModelVisibility(AnyModel model)
	{
		TryEmitSignal(SignalName.ToggleModelVisibilityRequested, model);
	}

	[Signal] public delegate void AddModelRequestedEventHandler(AnyModel childModel, AnyModel parentModel);
	/// <summary>
	/// モデルの追加をリクエストする
	/// </summary>
	/// <param name="childModel">追加するモデル</param>
	/// <param name="parentModel">追加先の親モデル、nullの場合はルートに追加される</param>
	internal static void RequestAddModel(AnyModel childModel, AnyModel parentModel = null)
	{
		TryEmitSignal(SignalName.AddModelRequested, childModel, parentModel);
	}

	[Signal] public delegate void LoadModelRequestedEventHandler(string path);
	/// <summary>
	/// モデルのロードをリクエストする
	/// </summary>
	/// <param name="path">ロードするモデルのパス</param>
	internal static void RequestLoadModel(string path)
	{
		TryEmitSignal(SignalName.LoadModelRequested, path);
	}

	#endregion

	#region --------------------------------------- Notification ---------------------------------------

	[Signal] public delegate void RootModelNotifiedEventHandler(RootModel rootModel);
	/// <summary>
	/// ルートモデルの通知を行う
	/// </summary>
	/// <param name="rootModel">通知するルートモデル</param>
	internal static void NotifyRootModel(RootModel rootModel)
	{
		TryEmitSignal(SignalName.RootModelNotified, rootModel);
	}

	[Signal] public delegate void ModelSelectionStateNotifiedEventHandler(AnyModel model, bool isSelected);
	/// <summary>
	/// モデルの選択状態の通知を行う
	/// </summary>
	/// <param name="model">選択状態が変化したモデル</param>
	/// <param name="isSelected">モデルが選択されている場合はtrue、選択されていない場合はfalse</param>
	internal static void NotifyModelSelectionState(AnyModel model, bool isSelected)
	{
		TryEmitSignal(SignalName.ModelSelectionStateNotified, model, isSelected);
	}

	[Signal] public delegate void ModelVisibilityStateNotifiedEventHandler(AnyModel model, bool isVisible);
	/// <summary>
	/// モデルの表示状態の通知を行う
	/// </summary>
	/// <param name="model">表示状態が変化したモデル</param>
	/// <param name="isVisible">モデルが表示されている場合はtrue、非表示の場合はfalse</param>
	internal static void NotifyModelVisibilityState(AnyModel model, bool isVisible)
	{
		TryEmitSignal(SignalName.ModelVisibilityStateNotified, model, isVisible);
	}

	#endregion
}