using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// コマンド履歴を管理するサービス
/// </summary>
public partial class CommandService : Node
{
	#region Fields

	private readonly List<CommandBase> _history = new();
	private int _cursor = 0;

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

	#region Events

	/// <summary>
	/// Applicationイベントの購読を開始する
	/// </summary>
	private void SubscribeApplicationEvents()
	{
		Application.Command.Event.AskStateRequested += OnAskStateRequested;
		Application.Command.Event.ExecuteRequested += OnExecuteRequested;
		Application.Command.Event.UndoRequested += OnUndoRequested;
		Application.Command.Event.RedoRequested += OnRedoRequested;
		Application.Command.Event.ClearRequested += OnClearRequested;
		Application.Command.Event.SetCursorRequested += OnSetCursorRequested;
	}

	/// <summary>
	/// Applicationイベントの購読を解除する
	/// </summary>
	private void UnsubscribeApplicationEvents()
	{
		Application.Command.Event.AskStateRequested -= OnAskStateRequested;
		Application.Command.Event.ExecuteRequested -= OnExecuteRequested;
		Application.Command.Event.UndoRequested -= OnUndoRequested;
		Application.Command.Event.RedoRequested -= OnRedoRequested;
		Application.Command.Event.ClearRequested -= OnClearRequested;
		Application.Command.Event.SetCursorRequested -= OnSetCursorRequested;
	}

	private void OnAskStateRequested()
	{
		NotifyState();
	}

	private void OnExecuteRequested(CommandBase command)
	{
		Execute(command);
	}

	private void OnUndoRequested()
	{
		Undo();
	}

	private void OnRedoRequested()
	{
		Redo();
	}

	private void OnClearRequested()
	{
		Clear();
	}

	private void OnSetCursorRequested(int cursor)
	{
		SetCursor(cursor);
	}

	#endregion

	#region Internal Helpers

	/// <summary>
	/// コマンドを実行し、Undoスタックに積む
	/// </summary>
	private void Execute(CommandBase command)
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
		_cursor++;
		Application.Log.Debug($"CommandService State: history={_history.Count}, cursor={_cursor}");
		NotifyState();
	}

	/// <summary>
	/// Undo 実行
	/// </summary>
	private void Undo()
	{
		if (_cursor <= 0)
		{
			Application.Log.Debug("CommandService Undo skipped: cursor is at initial position.");
			return;
		}

		var cmd = _history[_cursor - 1];
		Application.Log.Debug($"CommandService Undo: {cmd.Description}");
		cmd.Undo();
		_cursor--;
		Application.Log.Debug($"CommandService State: history={_history.Count}, cursor={_cursor}");
		NotifyState();
	}

	/// <summary>
	/// Redo 実行
	/// </summary>
	private void Redo()
	{
		if (_cursor >= _history.Count)
		{
			Application.Log.Debug("CommandService Redo skipped: no command can be redone.");
			return;
		}

		var cmd = _history[_cursor];
		Application.Log.Debug($"CommandService Redo: {cmd.Description}");
		cmd.Do();
		_cursor++;
		Application.Log.Debug($"CommandService State: history={_history.Count}, cursor={_cursor}");
		NotifyState();
	}

	/// <summary>
	/// スタックのクリア（シーン切り替え時など）
	/// </summary>
	private void Clear()
	{
		Application.Log.Info($"CommandService Clear: history={_history.Count}, cursor={_cursor}");
		_history.Clear();
		_cursor = 0;
		NotifyState();
	}

	/// <summary>
	/// カーソル位置を指定してタイムトラベルする
	/// </summary>
	/// <param name="cursor">移動先カーソル</param>
	private void SetCursor(int cursor)
	{
		int clampedCursor = Math.Clamp(cursor, 0, _history.Count);
		if (clampedCursor == _cursor)
		{
			NotifyState();
			return;
		}

		Application.Log.Debug($"CommandService SetCursor: from={_cursor}, to={clampedCursor}");

		while (_cursor > clampedCursor)
		{
			Undo();
		}

		while (_cursor < clampedCursor)
		{
			Redo();
		}
	}

	/// <summary>
	/// 現在カーソルより後ろの履歴を削除する
	/// </summary>
	private void TrimRedoBranch()
	{
		if (_cursor >= _history.Count)
		{
			return;
		}

		int removeStart = _cursor;
		int removeCount = _history.Count - removeStart;
		_history.RemoveRange(removeStart, removeCount);
	}

	/// <summary>
	/// 現在の履歴状態を通知する
	/// </summary>
	private void NotifyState()
	{
		Application.Command.Event.NotifyState(_history.ToArray(), _cursor);
	}

	#endregion
}