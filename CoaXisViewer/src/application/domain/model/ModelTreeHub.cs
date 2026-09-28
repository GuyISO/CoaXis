using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// モデルツリーの状態を ModelNode へ反映する Autoload ノード。
/// </summary>
public partial class ModelTreeHub : BaseHub
{
	#region Fields

	#endregion

	#region Properties

	#endregion

	#region Lifecycle

	#endregion

	#region Actions

	[Signal] public delegate void TreeCenteringRequestedEventHandler(string entityId);
	/// <summary>
	/// 指定したモデルをツリーの中央へ表示する操作をリクエストする
	/// </summary>
	/// <param name="entityId">ツリーの中央へ表示するモデル実体の識別子</param>
	internal void TreeCentering(Guid entityId)
	{
		EmitSignal(SignalName.TreeCenteringRequested, entityId.ToString());
	}

	#endregion

	#region Notifications

	#endregion

	#region Events

	#endregion

	#region Methods

	#endregion

	#region Helpers

	#endregion

}