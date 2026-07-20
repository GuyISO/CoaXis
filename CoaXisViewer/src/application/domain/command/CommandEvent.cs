using Godot;

/// <summary>
/// コマンド操作に関するイベント集約ハブ
/// </summary>
public partial class CommandEvent : EventBase<CommandEvent>
{
	#region Action

	[Signal] public delegate void ExecuteRequestedEventHandler(CommandBase command);
	/// <summary>
	/// コマンドの実行をリクエストする
	/// </summary>
	/// <param name="command">実行するコマンド</param>
	internal void Execute(CommandBase command)
	{
		Emit(SignalName.ExecuteRequested, command);
	}

	[Signal] public delegate void UndoRequestedEventHandler();
	/// <summary>
	/// 直近のコマンドの Undo をリクエストする
	/// </summary>
	internal void Undo()
	{
		Emit(SignalName.UndoRequested);
	}

	[Signal] public delegate void RedoRequestedEventHandler();
	/// <summary>
	/// 直近の Undo を Redo することをリクエストする
	/// </summary>
	internal void Redo()
	{
		Emit(SignalName.RedoRequested);
	}

	[Signal] public delegate void ClearRequestedEventHandler();
	/// <summary>
	/// コマンド履歴のクリアをリクエストする
	/// </summary>
	internal void Clear()
	{
		Emit(SignalName.ClearRequested);
	}

	#endregion
}