// TODO: リファクタリング確認後に削除
using Godot;
using System;

/// <summary>
/// モデルの論理状態の変更要求と変更通知を担当するハブ。
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
	/// <summary>
	/// 配置位置の変更を通知する。
	/// </summary>
	/// <param name="entityId">変更された ModelEntity の識別子</param>
	public void NotifyPosition(Guid entityId)
	{
		EmitSignal(SignalName.PositionNotified, entityId.ToString());
	}

	/// <summary>モデルの回転の変更通知。値は ModelEntity.Rotation を参照する。</summary>
	/// <param name="entityId">変更された ModelEntity の識別子</param>
	[Signal] public delegate void RotationNotifiedEventHandler(string entityId);
	/// <summary>
	/// 回転の変更を通知する。
	/// </summary>
	/// <param name="entityId">変更された ModelEntity の識別子</param>
	public void NotifyRotation(Guid entityId)
	{
		EmitSignal(SignalName.RotationNotified, entityId.ToString());
	}

	/// <summary>モデルの表示設定の変更通知。値は ModelEntity.Visibility を参照する。</summary>
	/// <param name="entityId">変更された ModelEntity の識別子</param>
	[Signal] public delegate void VisibilityNotifiedEventHandler(string entityId);
	/// <summary>
	/// 表示設定の変更を通知する。
	/// </summary>
	/// <param name="entityId">変更された ModelEntity の識別子</param>
	public void NotifyVisibility(Guid entityId)
	{
		EmitSignal(SignalName.VisibilityNotified, entityId.ToString());
	}

	/// <summary>モデルツリーの折り畳み状態の変更通知。値は ModelEntity.IsCollapsed を参照する。</summary>
	/// <param name="entityId">変更された ModelEntity の識別子</param>
	[Signal] public delegate void CollapsedEventHandler(string entityId);

	/// <summary>モデルのロード状態の変更通知。値は ModelEntity.Status を参照する。</summary>
	/// <param name="entityId">変更された ModelEntity の識別子</param>
	[Signal] public delegate void StatusNotifiedEventHandler(string entityId);
	/// <summary>
	/// ロード状態の変更を通知する。
	/// </summary>
	/// <param name="entityId">変更された ModelEntity の識別子</param>
	public void NotifyStatus(Guid entityId)
	{
		EmitSignal(SignalName.StatusNotified, entityId.ToString());
	}

	/// <summary>モデルの透明度の変更通知。値は ModelEntityVisualHub.Transparency を参照する。</summary>
	[Signal] public delegate void TransparencyNotifiedEventHandler();
	/// <summary>
	/// 透明度の変更を通知する。
	/// </summary>
	public void NotifyTransparency()
	{
		EmitSignal(SignalName.TransparencyNotified);
	}

	/// <summary>Registryのクリア通知。</summary>
	[Signal] public delegate void RegistryClearedEventHandler();
	/// <summary>
	/// モデルレジストリがクリアされたことを通知する。
	/// </summary>
	public void NotifyRegistryCleared()
	{
		EmitSignal(SignalName.RegistryCleared);
	}

	#endregion

	#region Lifecycle

	#endregion

	#region Events

	#endregion

	#region Methods

	/// <summary>
	/// モデルの表示/非表示を切り替える。
	/// </summary>
	/// <param name="entityId">切替対象の ModelEntity の識別子</param>
	public void ToggleModelVisibility(Guid entityId)
	{
		ModelEntity modelEntity = Application.Model.Entity.Registry.Get(entityId);
		if (modelEntity == null)
		{
			Application.Log.Warn($"ModelEntityStateHub: toggle target not found. entityId='{entityId}'");
			return;
		}

		var command = new SetModelVisibilityCommand(
			[entityId],
			GetNextVisibility(modelEntity.Visibility));
		Application.Command.Execute(command);
	}

	/// <summary>
	/// モデルの折り畳み状態を更新し、変更があった場合に通知する。
	/// </summary>
	/// <param name="entityId">対象 ModelEntity の識別子</param>
	/// <param name="isCollapsed">折り畳む場合は true、展開する場合は false</param>
	public void SetCollapsed(Guid entityId, bool isCollapsed)
	{
		ModelEntity modelEntity = Application.Model.Entity.Registry.Get(entityId);
		if (modelEntity == null)
		{
			Application.Log.Warn($"ModelEntityStateHub: collapse target not found. entityId='{entityId}'");
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

	#region Helpers

	/// <summary>
	/// 現在の表示設定から次の切替先を返す。
	/// </summary>
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