using Godot;

/// <summary>
/// モデル関連のイベント集約ハブ
/// </summary>
public partial class ModelEvent : EventBase<ModelEvent>
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

	[Signal] public delegate void ToggleModelVisibilityRequestedEventHandler(ModelNode model);
	/// <summary>
	/// モデルの表示/非表示切替をリクエストする
	/// </summary>
	/// <param name="model">切替対象のモデル</param>
	internal void ToggleModelVisibility(ModelNode model)
	{
		Emit(SignalName.ToggleModelVisibilityRequested, model);
	}

	[Signal] public delegate void AddModelRequestedEventHandler(ModelNode childModel, ModelNode parentModel);
	/// <summary>
	/// モデルの追加をリクエストする
	/// </summary>
	/// <param name="childModel">追加するモデル</param>
	/// <param name="parentModel">追加先の親モデル、nullの場合はルートに追加される</param>
	internal void AddModel(ModelNode childModel, ModelNode parentModel = null)
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

	[Signal] public delegate void RootModelNotifiedEventHandler(RootModelNode rootModel);
	/// <summary>
	/// ルートモデルの通知を行う
	/// </summary>
	/// <param name="rootModel">通知するルートモデル</param>
	internal void NotifyRootModel(RootModelNode rootModel)
	{
		Emit(SignalName.RootModelNotified, rootModel);
	}

	[Signal] public delegate void ModelVisibilityStateNotifiedEventHandler(ModelNode model, bool isVisible);
	/// <summary>
	/// モデルの表示状態の通知を行う
	/// </summary>
	/// <param name="model">表示状態が変化したモデル</param>
	/// <param name="isVisible">モデルが表示されている場合はtrue、非表示の場合はfalse</param>
	internal void NotifyModelVisibilityState(ModelNode model, bool isVisible)
	{
		Emit(SignalName.ModelVisibilityStateNotified, model, isVisible);
	}

	#endregion
}