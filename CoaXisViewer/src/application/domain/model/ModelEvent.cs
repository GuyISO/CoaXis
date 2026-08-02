using Godot;
using System;

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

	[Signal] public delegate void ToggleModelVisibilityRequestedEventHandler(string modelId);
	/// <summary>
	/// モデルの表示/非表示切替をリクエストする
	/// </summary>
	/// <param name="modelId">切替対象のモデル識別子</param>
	internal void ToggleModelVisibility(Guid modelId)
	{
		Emit(SignalName.ToggleModelVisibilityRequested, modelId.ToString());
	}

	[Signal] public delegate void AddModelRequestedEventHandler(string childModelId, string parentModelId);
	/// <summary>
	/// モデルの追加をリクエストする
	/// </summary>
	/// <param name="childModelId">追加するモデル識別子</param>
	/// <param name="parentModelId">追加先の親モデル識別子。Guid.Empty の場合はルートに追加される</param>
	internal void AddModel(Guid childModelId, Guid parentModelId = default)
	{
		Emit(SignalName.AddModelRequested, childModelId.ToString(), parentModelId.ToString());
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

	[Signal] public delegate void ModelVisibilityStateNotifiedEventHandler(string modelId, bool isVisible);
	/// <summary>
	/// モデルの表示状態の通知を行う
	/// </summary>
	/// <param name="modelId">表示状態が変化したモデル識別子</param>
	/// <param name="isVisible">モデルが表示されている場合はtrue、非表示の場合はfalse</param>
	internal void NotifyModelVisibilityState(Guid modelId, bool isVisible)
	{
		Emit(SignalName.ModelVisibilityStateNotified, modelId.ToString(), isVisible);
	}

	#endregion
}