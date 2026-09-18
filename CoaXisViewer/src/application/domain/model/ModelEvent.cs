using Godot;
using System;

/// <summary>
/// モデル関連のイベント集約ハブ
/// </summary>
public partial class ModelEvent : EventBase<ModelEvent>
{
	#region --------------------------------------- Action ---------------------------------------

	[Signal] public delegate void ToggleModelVisibilityRequestedEventHandler(string entityId);
	/// <summary>
	/// モデルの表示/非表示切替をリクエストする
	/// </summary>
	/// <param name="entityId">切替対象の ModelEntity の識別子</param>
	internal void ToggleModelVisibility(Guid entityId)
	{
		Emit(SignalName.ToggleModelVisibilityRequested, entityId.ToString());
	}

	[Signal] public delegate void TreeCenteringRequestedEventHandler(string entityId);
	/// <summary>
	/// 指定したモデルをツリーの中央へ表示する操作をリクエストする
	/// </summary>
	/// <param name="entityId">ツリーの中央へ表示するモデル実体の識別子</param>
	internal void TreeCentering(Guid entityId)
	{
		Emit(SignalName.TreeCenteringRequested, entityId.ToString());
	}

	#endregion

	#region --------------------------------------- Notification ---------------------------------------

	[Signal] public delegate void ModelAddedEventHandler(string entityId, string parentEntityId);
	/// <summary>
	/// モデルの追加を通知する
	/// </summary>
	/// <param name="entityId">追加する ModelEntity の識別子</param>
	/// <param name="parentEntityId">追加先の親 ModelEntity の識別子。Guid.Empty の場合はルートに追加される</param>
	internal void NotifyModelAdded(Guid entityId, Guid parentEntityId = default)
	{
		Emit(SignalName.ModelAdded, entityId.ToString(), parentEntityId.ToString());
	}

	[Signal] public delegate void ModelVisibilityStateNotifiedEventHandler(string entityId, bool isVisible);
	/// <summary>
	/// モデルの表示状態の通知を行う
	/// </summary>
	/// <param name="entityId">表示状態が変化した ModelEntity の識別子</param>
	/// <param name="isVisible">モデルが表示されている場合はtrue、非表示の場合はfalse</param>
	internal void NotifyModelVisibilityState(Guid entityId, bool isVisible)
	{
		Emit(SignalName.ModelVisibilityStateNotified, entityId.ToString(), isVisible);
	}

	[Signal] public delegate void ModelStatusNotifiedEventHandler(string entityId, int status);
	/// <summary>
	/// モデルのロード状態が変化したことを通知する
	/// </summary>
	/// <param name="entityId">状態が変化した ModelEntity の識別子</param>
	/// <param name="status">新しい状態</param>
	internal void NotifyModelStatusChanged(Guid entityId, ModelStatus status)
	{
		Emit(SignalName.ModelStatusNotified, entityId.ToString(), (int)status);
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