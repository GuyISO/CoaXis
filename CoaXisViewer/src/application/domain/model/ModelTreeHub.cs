using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// モデルツリーの状態をModelNodeへ反映するAutoloadノード
/// </summary>
public partial class ModelTreeHub : Node
{

	#region --------------------------------------- Action ---------------------------------------

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

}