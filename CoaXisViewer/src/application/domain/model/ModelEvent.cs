using Godot;
using System;

/// <summary>
/// モデル関連のイベント集約ハブ
/// </summary>
public partial class ModelEvent : EventBase<ModelEvent>
{
	#region --------------------------------------- Action ---------------------------------------

	[Signal] public delegate void ToggleModelVisibilityRequestedEventHandler(string modelId);
	/// <summary>
	/// モデルの表示/非表示切替をリクエストする
	/// </summary>
	/// <param name="modelId">切替対象のモデル識別子</param>
	internal void ToggleModelVisibility(Guid modelId)
	{
		Emit(SignalName.ToggleModelVisibilityRequested, modelId.ToString());
	}

	#endregion

	#region --------------------------------------- Notification ---------------------------------------

	[Signal] public delegate void ModelAddedEventHandler(string modelId, string parentModelId);
	/// <summary>
	/// モデルの追加を通知する
	/// </summary>
	/// <param name="modelId">追加するモデル識別子</param>
	/// <param name="parentModelId">追加先の親モデル識別子。Guid.Empty の場合はルートに追加される</param>
	internal void NotifyModelAdded(Guid modelId, Guid parentModelId = default)
	{
		Emit(SignalName.ModelAdded, modelId.ToString(), parentModelId.ToString());
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

	[Signal] public delegate void ModelStatusNotifiedEventHandler(string modelId, int status);
	/// <summary>
	/// モデルのロード状態が変化したことを通知する
	/// </summary>
	/// <param name="modelId">状態が変化したモデル識別子</param>
	/// <param name="status">新しい状態</param>
	internal void NotifyModelStatusChanged(Guid modelId, ModelStatus status)
	{
		Emit(SignalName.ModelStatusNotified, modelId.ToString(), (int)status);
	}

	[Signal] public delegate void TransparencyNotifiedEventHandler(float transparency);
	/// <summary>
	/// モデルの透明度を通知する
	/// </summary>
	/// <param name="transparency">新しい透明度</param>
	internal void NotifyTransparency(float transparency)
	{
		Emit(SignalName.TransparencyNotified, transparency);
	}

	[Signal] public delegate void RegistryClearedEventHandler();
	/// <summary>
	/// モデルレジストリがクリアされたことを通知する
	/// </summary>
	internal void NotifyRegistryCleared()
	{
		Emit(SignalName.RegistryCleared);
	}

	#endregion
}