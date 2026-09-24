using Godot;
using System;

/// <summary>
/// モデルの論理状態変更要求を処理するサービス
/// </summary>
public partial class ModelStateService : Node
{
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

	#region Events

	/// <summary>
	/// Applicationイベントの購読を開始する
	/// </summary>
	private void SubscribeApplicationEvents()
	{
		Application.Model.Event.ToggleModelVisibilityRequested += OnToggleModelVisibilityRequested;
		Application.Model.Event.Collapsed += OnModelCollapsed;
	}

	/// <summary>
	/// Applicationイベントの購読を解除する
	/// </summary>
	private void UnsubscribeApplicationEvents()
	{
		Application.Model.Event.ToggleModelVisibilityRequested -= OnToggleModelVisibilityRequested;
		Application.Model.Event.Collapsed -= OnModelCollapsed;
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

		ModelEntity modelEntity = Application.Model.Registry.GetEntity(parsedEntityId);
		if (modelEntity == null)
		{
			Application.Log.Warn($"ModelStateService: toggle target not found. entityId='{parsedEntityId}'");
			return;
		}

		var command = new SetModelVisibilityCommand(
			[parsedEntityId],
			GetNextVisibility(modelEntity.Visibility));
		Application.Command.Event.Execute(command);
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

		ModelEntity modelEntity = Application.Model.Registry.GetEntity(parsedEntityId);
		if (modelEntity == null)
		{
			Application.Log.Warn($"ModelStateService: collapse target not found. entityId='{parsedEntityId}'");
			return;
		}

		modelEntity.IsCollapsed = isCollapsed;
	}

	#endregion

	#region Internal Helpers

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