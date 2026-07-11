using Godot;

/// <summary>
/// モデル関連のイベント集約ハブ
/// </summary>
public partial class ModelEventHub : EventHubBase<ModelEventHub>
{
	#region --------------------------------------- Action ---------------------------------------

	[Signal] public delegate void AskRootModelRequestedEventHandler();
	/// <summary>
	/// ルートモデルに対して通知をリクエストする
	/// </summary>
	internal void AskRootModel()
	{
		Emit(SignalName.AskRootModelRequested);
	}

	[Signal] public delegate void SetMultiSelectionModeRequestedEventHandler(bool enable);
	/// <summary>
	/// 複数選択モードの設定をリクエストする
	/// </summary>
	/// <param name="enable">複数選択モードを有効にする場合はtrue、無効にする場合はfalse</param>
	internal void SetMultiSelectionMode(bool enable)
	{
		Emit(SignalName.SetMultiSelectionModeRequested, enable);
	}

	[Signal] public delegate void ClearSelectionRequestedEventHandler();
	/// <summary>
	/// 選択のクリアをリクエストする
	/// </summary>
	internal void ClearSelection()
	{
		Emit(SignalName.ClearSelectionRequested);
	}

	[Signal] public delegate void ToggleModelVisibilityRequestedEventHandler(AnyModel model);
	/// <summary>
	/// モデルの表示/非表示切替をリクエストする
	/// </summary>
	/// <param name="model">切替対象のモデル</param>
	internal void ToggleModelVisibility(AnyModel model)
	{
		Emit(SignalName.ToggleModelVisibilityRequested, model);
	}

	[Signal] public delegate void AddModelRequestedEventHandler(AnyModel childModel, AnyModel parentModel);
	/// <summary>
	/// モデルの追加をリクエストする
	/// </summary>
	/// <param name="childModel">追加するモデル</param>
	/// <param name="parentModel">追加先の親モデル、nullの場合はルートに追加される</param>
	internal void AddModel(AnyModel childModel, AnyModel parentModel = null)
	{
		Emit(SignalName.AddModelRequested, childModel, parentModel);
	}

	[Signal] public delegate void LoadModelRequestedEventHandler(string path);
	/// <summary>
	/// モデルのロードをリクエストする
	/// </summary>
	/// <param name="path">ロードするモデルのパス</param>
	internal void LoadModel(string path)
	{
		Emit(SignalName.LoadModelRequested, path);
	}

	#endregion

	#region --------------------------------------- Notification ---------------------------------------

	[Signal] public delegate void RootModelNotifiedEventHandler(RootModel rootModel);
	/// <summary>
	/// ルートモデルの通知を行う
	/// </summary>
	/// <param name="rootModel">通知するルートモデル</param>
	internal void NotifyRootModel(RootModel rootModel)
	{
		Emit(SignalName.RootModelNotified, rootModel);
	}

	[Signal] public delegate void ModelSelectionStateNotifiedEventHandler(AnyModel model, bool isSelected);
	/// <summary>
	/// モデルの選択状態の通知を行う
	/// </summary>
	/// <param name="model">選択状態が変化したモデル</param>
	/// <param name="isSelected">モデルが選択されている場合はtrue、選択されていない場合はfalse</param>
	internal void NotifyModelSelectionState(AnyModel model, bool isSelected)
	{
		Emit(SignalName.ModelSelectionStateNotified, model, isSelected);
	}

	[Signal] public delegate void ModelVisibilityStateNotifiedEventHandler(AnyModel model, bool isVisible);
	/// <summary>
	/// モデルの表示状態の通知を行う
	/// </summary>
	/// <param name="model">表示状態が変化したモデル</param>
	/// <param name="isVisible">モデルが表示されている場合はtrue、非表示の場合はfalse</param>
	internal void NotifyModelVisibilityState(AnyModel model, bool isVisible)
	{
		Emit(SignalName.ModelVisibilityStateNotified, model, isVisible);
	}

	#endregion
}