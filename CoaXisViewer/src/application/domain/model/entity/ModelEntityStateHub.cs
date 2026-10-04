// TODO: リファクタリング確認後に削除
using Godot;
using System;

/// <summary>
/// モデルの論理状態の変更要求と変更通知を担当するハブ。
/// 通知は対象の entityId のみを運び、値は Registry 経由で ModelEntity から参照する。
/// </summary>
public partial class ModelEntityStateHub : BaseHub
{
	#region Actions

	/// <summary>
	/// モデルの表示/非表示を切り替える
	/// </summary>
	/// <param name="entityId">切替対象の ModelEntity の識別子</param>
	internal void ToggleModelVisibility(Guid entityId)
	{
		ModelEntity modelEntity = Application.Model.Entity.Registry.GetEntity(entityId);
		if (modelEntity == null)
		{
			Application.Log.Warn($"ModelStateService: toggle target not found. entityId='{entityId}'");
			return;
		}

		var command = new SetModelVisibilityCommand(
			[entityId],
			GetNextVisibility(modelEntity.Visibility));
		Application.Command.Execute(command);
	}

	/// <summary>
	/// モデルの折り畳み状態を更新し、変更があった場合に通知する
	/// </summary>
	/// <param name="entityId">対象 ModelEntity の識別子</param>
	/// <param name="isCollapsed">折り畳む場合は true、展開する場合は false</param>
	internal void SetCollapsed(Guid entityId, bool isCollapsed)
	{
		ModelEntity modelEntity = Application.Model.Entity.Registry.GetEntity(entityId);
		if (modelEntity == null)
		{
			Application.Log.Warn($"ModelStateService: collapse target not found. entityId='{entityId}'");
			return;
		}

		if (modelEntity.IsCollapsed == isCollapsed)
		{
			return;
		}

		modelEntity.IsCollapsed = isCollapsed;
		EmitSignal(SignalName.Collapsed, entityId.ToString());
	}

	#endregion

	#region Notifications

	/// <summary>モデルの配置位置の変更通知。値は ModelEntity.Position を参照する。</summary>
	/// <param name="entityId">変更された ModelEntity の識別子</param>
	[Signal] public delegate void PositionNotifiedEventHandler(string entityId);
	internal void NotifyPosition(Guid entityId)
	{
		EmitSignal(SignalName.PositionNotified, entityId.ToString());
	}

	/// <summary>モデルの回転の変更通知。値は ModelEntity.Rotation を参照する。</summary>
	/// <param name="entityId">変更された ModelEntity の識別子</param>
	[Signal] public delegate void RotationNotifiedEventHandler(string entityId);
	internal void NotifyRotation(Guid entityId)
	{
		EmitSignal(SignalName.RotationNotified, entityId.ToString());
	}

	/// <summary>モデルの表示設定の変更通知。値は ModelEntity.Visibility を参照する。</summary>
	/// <param name="entityId">変更された ModelEntity の識別子</param>
	[Signal] public delegate void VisibilityNotifiedEventHandler(string entityId);
	internal void NotifyVisibility(Guid entityId)
	{
		EmitSignal(SignalName.VisibilityNotified, entityId.ToString());
	}

	/// <summary>モデルツリーの折り畳み状態の変更通知。値は ModelEntity.IsCollapsed を参照する。</summary>
	/// <param name="entityId">変更された ModelEntity の識別子</param>
	[Signal] public delegate void CollapsedEventHandler(string entityId);

	/// <summary>モデルのロード状態の変更通知。値は ModelEntity.Status を参照する。</summary>
	/// <param name="entityId">変更された ModelEntity の識別子</param>
	[Signal] public delegate void StatusNotifiedEventHandler(string entityId);
	internal void NotifyStatus(Guid entityId)
	{
		EmitSignal(SignalName.StatusNotified, entityId.ToString());
	}

	/// <summary>モデルの透明度の変更通知。値は ModelEntityVisualHub.Transparency を参照する。</summary>
	[Signal] public delegate void TransparencyNotifiedEventHandler();
	internal void NotifyTransparency()
	{
		EmitSignal(SignalName.TransparencyNotified);
	}

	[Signal] public delegate void RegistryClearedEventHandler();
	/// <summary>
	/// モデルレジストリがクリアされたことを通知する
	/// </summary>
	internal void NotifyRegistryCleared()
	{
		EmitSignal(SignalName.RegistryCleared);
	}

	#endregion

	#region Helpers

	private static ModelVisibility GetNextVisibility(ModelVisibility visibility)
	{
		return visibility switch
		{
			ModelVisibility.Inherit => ModelVisibility.Visible,
			ModelVisibility.Visible => ModelVisibility.Invisible,
			_ => ModelVisibility.Inherit,
		};
	}

	#endregion
}