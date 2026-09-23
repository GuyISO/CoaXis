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

	[Signal] public delegate void AddedEventHandler(string entityId, string parentEntityId);
	/// <summary>
	/// モデルの追加を通知する
	/// </summary>
	/// <param name="entityId">追加する ModelEntity の識別子</param>
	/// <param name="parentEntityId">追加先の親 ModelEntity の識別子。Guid.Empty の場合はルートに追加される</param>
	internal void NotifyAdded(Guid entityId, Guid parentEntityId = default)
	{
		Emit(SignalName.Added, entityId.ToString(), parentEntityId.ToString());
	}

	[Signal] public delegate void PositionNotifiedEventHandler(string entityId, Vector3 position);
	/// <summary>
	/// モデルの配置位置を通知する
	/// </summary>
	/// <param name="entityId">配置位置が変化した ModelEntity の識別子</param>
	/// <param name="position">変更後の配置位置（Godot座標系）</param>
	internal void NotifyPosition(Guid entityId, Vector3 position)
	{
		Emit(SignalName.PositionNotified, entityId.ToString(), position);
	}

	[Signal] public delegate void RotationNotifiedEventHandler(string entityId, Quaternion rotation);
	/// <summary>
	/// モデルの回転を通知する
	/// </summary>
	/// <param name="entityId">回転が変化した ModelEntity の識別子</param>
	/// <param name="rotation">変更後の回転（Godot座標系）</param>
	internal void NotifyRotation(Guid entityId, Quaternion rotation)
	{
		Emit(SignalName.RotationNotified, entityId.ToString(), rotation);
	}

	[Signal] public delegate void VisibilityNotifiedEventHandler(string entityId, ModelVisibility visibility);
	/// <summary>
	/// モデルの表示状態の通知を行う
	/// </summary>
	/// <param name="entityId">表示状態が変化した ModelEntity の識別子</param>
	/// <param name="visibility">変更後のモデル表示設定</param>
	internal void NotifyVisibility(Guid entityId, ModelVisibility visibility)
	{
		Emit(SignalName.VisibilityNotified, entityId.ToString(), (int)visibility);
	}

	[Signal] public delegate void CollapsedEventHandler(string entityId, bool isCollapsed);
	/// <summary>
	/// モデルツリーの折りたたみを通知する
	/// </summary>
	/// <param name="entityId">折りたたまれた ModelEntity の識別子</param>
	/// <param name="isCollapsed">モデルツリーが折りたたまれている場合はtrue、展開されている場合はfalse</param>
	internal void NotifyCollapsed(Guid entityId, bool isCollapsed)
	{
		Emit(SignalName.Collapsed, entityId.ToString(), isCollapsed);
	}

	[Signal] public delegate void StatusNotifiedEventHandler(string entityId, int status);
	/// <summary>
	/// モデルのロード状態が変化したことを通知する
	/// </summary>
	/// <param name="entityId">状態が変化した ModelEntity の識別子</param>
	/// <param name="status">新しい状態</param>
	internal void NotifyStatus(Guid entityId, ModelStatus status)
	{
		Emit(SignalName.StatusNotified, entityId.ToString(), (int)status);
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