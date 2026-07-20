using Godot;

/// <summary>
/// コマンド操作に関するイベント集約ハブ
/// </summary>
public partial class CommandEvent : EventBase<CommandEvent>
{
	#region Action

	[Signal] public delegate void AskStateRequestedEventHandler();
	/// <summary>
	/// コマンド履歴状態の通知をリクエストする
	/// </summary>
	internal void AskState()
	{
		Emit(SignalName.AskStateRequested);
	}

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

	[Signal] public delegate void SetCursorRequestedEventHandler(int cursor);
	/// <summary>
	/// 指定位置までのタイムトラベルをリクエストする
	/// </summary>
	/// <param name="cursor">移動先カーソル位置</param>
	internal void SetCursor(int cursor)
	{
		Emit(SignalName.SetCursorRequested, cursor);
	}

	#endregion

	#region Notification

	[Signal] public delegate void StateNotifiedEventHandler(CommandBase[] history, int cursor);
	/// <summary>
	/// コマンド履歴状態を通知する
	/// </summary>
	/// <param name="history">履歴配列</param>
	/// <param name="cursor">現在カーソル位置（-1 の場合は未実行）</param>
	internal void NotifyState(CommandBase[] history, int cursor)
	{
		Emit(SignalName.StateNotified, history, cursor);
	}

	#endregion
}