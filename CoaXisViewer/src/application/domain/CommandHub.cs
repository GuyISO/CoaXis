using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// コマンド履歴と実行状態を管理するハブ。
/// </summary>
public partial class CommandHub : BaseHub
{
	#region Fields

	private readonly List<BaseCommand> _history = new();

	#endregion

	#region Properties

	/// <summary>
	/// コマンド履歴の配列を取得する
	/// </summary>
	internal BaseCommand[] History => _history.ToArray();

	/// <summary>
	/// 現在のカーソル位置
	/// </summary>
	internal int Cursor { get; private set; } = 0;

	#endregion

	#region Lifecycle

	#endregion

	#region Signals

	[Signal] public delegate void ExecutedEventHandler();
	/// <summary>
	/// コマンドの実行を通知する
	/// </summary>
	internal void NotifyExecuted()
	{
		EmitSignal(SignalName.Executed);
	}

	#endregion

	#region Events

	#endregion

	#region Methods

	/// <summary>
	/// コマンドを実行し、Undoスタックに積む
	/// </summary>
	internal void Execute(BaseCommand command)
	{
		if (command == null)
		{
			Application.Log.Warn("CommandService.Execute was called with null command.");
			return;
		}

		TrimRedoBranch();

		Application.Log.Debug($"CommandService Execute: {command.Description}");
		command.Do();
		_history.Add(command);
		Cursor++;
		Application.Log.Debug($"CommandService State: history={_history.Count}, cursor={Cursor}");
		NotifyExecuted();
	}

	/// <summary>
	/// Undo 実行
	/// </summary>
	internal void Undo()
	{
		if (Cursor <= 0)
		{
			Application.Log.Debug("CommandService Undo skipped: cursor is at initial position.");
			return;
		}

		var cmd = _history[Cursor - 1];
		Application.Log.Debug($"CommandService Undo: {cmd.Description}");
		cmd.Undo();
		Cursor--;
		Application.Log.Debug($"CommandService State: history={_history.Count}, cursor={Cursor}");
		NotifyExecuted();
	}

	/// <summary>
	/// Redo 実行
	/// </summary>
	internal void Redo()
	{
		if (Cursor >= _history.Count)
		{
			Application.Log.Debug("CommandService Redo skipped: no command can be redone.");
			return;
		}

		var cmd = _history[Cursor];
		Application.Log.Debug($"CommandService Redo: {cmd.Description}");
		cmd.Do();
		Cursor++;
		Application.Log.Debug($"CommandService State: history={_history.Count}, cursor={Cursor}");
		NotifyExecuted();
	}

	/// <summary>
	/// スタックのクリア（シーン切り替え時など）
	/// </summary>
	internal void Clear()
	{
		Application.Log.Info($"CommandService Clear: history={_history.Count}, cursor={Cursor}");
		_history.Clear();
		Cursor = 0;
		NotifyExecuted();
	}

	/// <summary>
	/// カーソル位置を指定してタイムトラベルする
	/// </summary>
	/// <param name="cursor">移動先カーソル</param>
	internal void SetCursor(int cursor)
	{
		int clampedCursor = Math.Clamp(cursor, 0, _history.Count);
		if (clampedCursor == Cursor)
		{
			return;
		}

		Application.Log.Debug($"CommandService SetCursor: from={Cursor}, to={clampedCursor}");

		while (Cursor > clampedCursor)
		{
			Undo();
		}

		while (Cursor < clampedCursor)
		{
			Redo();
		}
	}

	#endregion

	#region Helpers

	/// <summary>
	/// 現在カーソルより後ろの履歴を削除する。
	/// </summary>
	private void TrimRedoBranch()
	{
		if (Cursor >= _history.Count)
		{
			return;
		}

		int removeStart = Cursor;
		int removeCount = _history.Count - removeStart;
		_history.RemoveRange(removeStart, removeCount);
	}

	#endregion
}