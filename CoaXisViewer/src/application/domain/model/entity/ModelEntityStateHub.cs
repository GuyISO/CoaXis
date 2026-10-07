using Godot;
using System;

/// <summary>
/// ModelEntityが更新した論理状態の変更通知のみを担当するハブ。
/// 通知は対象の entityId のみを運び、値は Registry 経由で ModelEntity から参照する。
/// </summary>
public partial class ModelEntityStateHub : BaseHub
{
	#region Fields

	#endregion

	#region Properties

	#endregion

	#region Signals

	/// <summary>モデルの配置位置の変更通知。値は ModelEntity.Position を参照する。</summary>
	/// <param name="entityId">変更された ModelEntity の識別子</param>
	[Signal] public delegate void PositionNotifiedEventHandler(string entityId);

	/// <summary>モデルの回転の変更通知。値は ModelEntity.Rotation を参照する。</summary>
	/// <param name="entityId">変更された ModelEntity の識別子</param>
	[Signal] public delegate void RotationNotifiedEventHandler(string entityId);

	/// <summary>モデルの表示設定の変更通知。値は ModelEntity.Visibility を参照する。</summary>
	/// <param name="entityId">変更された ModelEntity の識別子</param>
	[Signal] public delegate void VisibilityNotifiedEventHandler(string entityId);

	/// <summary>モデルツリーの折り畳み状態の変更通知。値は ModelEntity.IsCollapsed を参照する。</summary>
	/// <param name="entityId">変更された ModelEntity の識別子</param>
	[Signal] public delegate void CollapsedEventHandler(string entityId);

	/// <summary>モデルのロード状態の変更通知。値は ModelEntity.Status を参照する。</summary>
	/// <param name="entityId">変更された ModelEntity の識別子</param>
	[Signal] public delegate void StatusNotifiedEventHandler(string entityId);

	#endregion

	#region Lifecycle

	#endregion

	#region Events

	#endregion

	#region Methods

	/// <summary>
	/// 配置位置の変更を通知する。Fieldの変更に対して状態の反映はModelEntity自身が行うが、変更を周知するためにModelEntity自身から呼び出して使用する。
	/// </summary>
	/// <param name="entityId">変更された ModelEntity の識別子</param>
	public void NotifyPosition(Guid entityId)
	{
		EmitSignal(SignalName.PositionNotified, entityId.ToString());
	}

	/// <summary>
	/// 回転の変更を通知する。Fieldの変更に対して状態の反映はModelEntity自身が行うが、変更を周知するためにModelEntity自身から呼び出して使用する。
	/// </summary>
	/// <param name="entityId">変更された ModelEntity の識別子</param>
	public void NotifyRotation(Guid entityId)
	{
		EmitSignal(SignalName.RotationNotified, entityId.ToString());
	}

	/// <summary>
	/// 表示設定の変更を通知する。Fieldの変更に対して状態の反映はModelEntity自身が行うが、変更を周知するためにModelEntity自身から呼び出して使用する。
	/// </summary>
	/// <param name="entityId">変更された ModelEntity の識別子</param>
	public void NotifyVisibility(Guid entityId)
	{
		EmitSignal(SignalName.VisibilityNotified, entityId.ToString());
	}

	/// <summary>
	/// ツリーの折り畳み状態の変更を通知する。Fieldの変更に対して状態の反映はModelEntity自身が行うが、変更を周知するためにModelEntity自身から呼び出して使用する。
	/// </summary>
	/// <param name="entityId">変更された ModelEntity の識別子</param>
	public void NotifyCollapsed(Guid entityId)
	{
		EmitSignal(SignalName.Collapsed, entityId.ToString());
	}

	/// <summary>
	/// ロード状態の変更を通知する。Fieldの変更に対して状態の反映はModelEntity自身が行うが、変更を周知するためにModelEntity自身から呼び出して使用する。
	/// </summary>
	/// <param name="entityId">変更された ModelEntity の識別子</param>
	public void NotifyStatus(Guid entityId)
	{
		EmitSignal(SignalName.StatusNotified, entityId.ToString());
	}

	#endregion

	#region Helpers

	#endregion
}