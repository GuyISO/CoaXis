// TODO: リファクタリング確認後に削除
using Godot;
using System;

/// <summary>
/// モデルの論理状態と変更要求を管理するハブ。
/// </summary>
public partial class ModelEntityStateHub : BaseHub
{
	#region Fields

	#endregion

	#region Properties

	#endregion

	#region Lifecycle

	public override void _Ready()
	{
		SubscribeApplicationEvents();
	}

	public override void _ExitTree()
	{
		UnsubscribeApplicationEvents();

		base._ExitTree();
	}

	#endregion

	#region Actions

	[Signal] public delegate void ToggleModelVisibilityRequestedEventHandler(string entityId);
	/// <summary>
	/// モデルの表示/非表示切替をリクエストする
	/// </summary>
	/// <param name="entityId">切替対象の ModelEntity の識別子</param>
	internal void ToggleModelVisibility(Guid entityId)
	{
		EmitSignal(SignalName.ToggleModelVisibilityRequested, entityId.ToString());
	}

	#endregion

	#region Notifications

	[Signal] public delegate void PositionNotifiedEventHandler(string entityId, Vector3 position);
	/// <summary>
	/// モデルの配置位置を通知する
	/// </summary>
	/// <param name="entityId">配置位置が変化した ModelEntity の識別子</param>
	/// <param name="position">変更後の配置位置（Godot座標系）</param>
	internal void NotifyPosition(Guid entityId, Vector3 position)
	{
		EmitSignal(SignalName.PositionNotified, entityId.ToString(), position);
	}

	[Signal] public delegate void RotationNotifiedEventHandler(string entityId, Quaternion rotation);
	/// <summary>
	/// モデルの回転を通知する
	/// </summary>
	/// <param name="entityId">回転が変化した ModelEntity の識別子</param>
	/// <param name="rotation">変更後の回転（Godot座標系）</param>
	internal void NotifyRotation(Guid entityId, Quaternion rotation)
	{
		EmitSignal(SignalName.RotationNotified, entityId.ToString(), rotation);
	}

	[Signal] public delegate void VisibilityNotifiedEventHandler(string entityId, ModelVisibility visibility);
	/// <summary>
	/// モデルの表示状態の通知を行う
	/// </summary>
	/// <param name="entityId">表示状態が変化した ModelEntity の識別子</param>
	/// <param name="visibility">変更後のモデル表示設定</param>
	internal void NotifyVisibility(Guid entityId, ModelVisibility visibility)
	{
		EmitSignal(SignalName.VisibilityNotified, entityId.ToString(), (int)visibility);
	}

	[Signal] public delegate void CollapsedEventHandler(string entityId, bool isCollapsed);
	/// <summary>
	/// モデルツリーの折りたたみを通知する
	/// </summary>
	/// <param name="entityId">折りたたまれた ModelEntity の識別子</param>
	/// <param name="isCollapsed">モデルツリーが折りたたまれている場合はtrue、展開されている場合はfalse</param>
	internal void NotifyCollapsed(Guid entityId, bool isCollapsed)
	{
		EmitSignal(SignalName.Collapsed, entityId.ToString(), isCollapsed);
	}

	[Signal] public delegate void StatusNotifiedEventHandler(string entityId, int status);
	/// <summary>
	/// モデルのロード状態が変化したことを通知する
	/// </summary>
	/// <param name="entityId">状態が変化した ModelEntity の識別子</param>
	/// <param name="status">新しい状態</param>
	internal void NotifyStatus(Guid entityId, ModelStatus status)
	{
		EmitSignal(SignalName.StatusNotified, entityId.ToString(), (int)status);
	}

	[Signal] public delegate void TransparencyNotifiedEventHandler(float transparency);
	/// <summary>
	/// モデルの透明度を通知する
	/// </summary>
	/// <param name="transparency">新しい透明度</param>
	internal void NotifyTransparency(float transparency)
	{
		EmitSignal(SignalName.TransparencyNotified, transparency);
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
    
	#region Events

	/// <summary>
	/// Applicationイベントの購読を開始する
	/// </summary>
	private void SubscribeApplicationEvents()
	{
		Application.Model.Entity.State.ToggleModelVisibilityRequested += OnToggleModelVisibilityRequested;
		Application.Model.Entity.State.Collapsed += OnModelCollapsed;
	}

	/// <summary>
	/// Applicationイベントの購読を解除する
	/// </summary>
	private void UnsubscribeApplicationEvents()
	{
		Application.Model.Entity.State.ToggleModelVisibilityRequested -= OnToggleModelVisibilityRequested;
		Application.Model.Entity.State.Collapsed -= OnModelCollapsed;
	}

	/// <summary>
	/// モデルの表示状態切替がリクエストされたときに呼び出されるイベントハンドラ
	/// </summary>
	/// <param name="entityId">表示状態を切り替えるModelEntityの識別子</param>
	private void OnToggleModelVisibilityRequested(string entityId)
	{
		if (!Guid.TryParse(entityId, out Guid parsedEntityId) || parsedEntityId == Guid.Empty)
		{
			Application.Log.Warn($"ModelStateService: invalid entityId for toggle request. entityId='{entityId}'");
			return;
		}

		ModelEntity modelEntity = Application.Model.Entity.Registry.GetEntity(parsedEntityId);
		if (modelEntity == null)
		{
			Application.Log.Warn($"ModelStateService: toggle target not found. entityId='{parsedEntityId}'");
			return;
		}

		var command = new SetModelVisibilityCommand(
			[parsedEntityId],
			GetNextVisibility(modelEntity.Visibility));
		Application.Command.Execute(command);
	}

	/// <summary>
	/// モデルの折り畳み状態が通知されたときに呼び出されるイベントハンドラ
	/// </summary>
	/// <param name="entityId">折り畳み状態が変更されたModelEntityの識別子</param>
	/// <param name="isCollapsed">モデルが折り畳まれている場合はtrue、展開されている場合はfalse</param>
	private void OnModelCollapsed(string entityId, bool isCollapsed)
	{
		if (!Guid.TryParse(entityId, out Guid parsedEntityId) || parsedEntityId == Guid.Empty)
		{
			Application.Log.Warn($"ModelStateService: invalid entityId for collapse notification. entityId='{entityId}'");
			return;
		}

		ModelEntity modelEntity = Application.Model.Entity.Registry.GetEntity(parsedEntityId);
		if (modelEntity == null)
		{
			Application.Log.Warn($"ModelStateService: collapse target not found. entityId='{parsedEntityId}'");
			return;
		}

		modelEntity.IsCollapsed = isCollapsed;
	}

	#endregion

	#region Methods

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