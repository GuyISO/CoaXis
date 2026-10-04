// TODO: リファクタリング確認後に削除
using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// ModelEntity ツリー操作の要求を管理するハブ。
/// </summary>
public partial class ModelEntityTreeHub : BaseHub
{
	#region Fields

	#endregion

	#region Properties

	#endregion

	#region Lifecycle

	#endregion

	#region Events

	[Signal] public delegate void CenteringRequestedEventHandler(string entityId);
	/// <summary>
	/// 指定したモデルをツリーの中央へ表示する操作をリクエストする
	/// </summary>
	/// <param name="entityId">ツリーの中央へ表示するモデル実体の識別子</param>
	internal void Centering(Guid entityId)
	{
		EmitSignal(SignalName.CenteringRequested, entityId.ToString());
	}

	#endregion

	#region Methods

	#endregion

	#region Helpers

	#endregion

}