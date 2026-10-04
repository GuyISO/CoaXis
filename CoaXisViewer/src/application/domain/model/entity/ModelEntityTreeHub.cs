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

	#region Signals

	/// <summary>指定Entityへのツリー上センタリング要求。</summary>
	[Signal] public delegate void CenteringRequestedEventHandler(string entityId);

	#endregion

	#region Lifecycle

	#endregion

	#region Events

	#endregion

	#region Methods

	/// <summary>
	/// 指定したモデルをツリーの中央へ表示する操作をリクエストする。
	/// </summary>
	/// <param name="entityId">ツリーの中央へ表示するモデル実体の識別子</param>
	internal void Centering(Guid entityId)
	{
		EmitSignal(SignalName.CenteringRequested, entityId.ToString());
	}

	#endregion

	#region Helpers

	#endregion
}