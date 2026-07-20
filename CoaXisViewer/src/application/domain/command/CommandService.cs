using Godot;
using System.Collections.Generic;

/// <summary>
/// コマンド履歴を管理するサービス
/// </summary>
public partial class CommandService : Node
{
	#region Fields

	private readonly Stack<CommandBase> _undoStack = new();
	private readonly Stack<CommandBase> _redoStack = new();

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
		Application.Command.Event.ExecuteRequested += OnExecuteRequested;
		Application.Command.Event.UndoRequested += OnUndoRequested;
		Application.Command.Event.RedoRequested += OnRedoRequested;
		Application.Command.Event.ClearRequested += OnClearRequested;
	}

	/// <summary>
	/// Applicationイベントの購読を解除する
	/// </summary>
	private void UnsubscribeApplicationEvents()
	{
		Application.Command.Event.ExecuteRequested -= OnExecuteRequested;
		Application.Command.Event.UndoRequested -= OnUndoRequested;
		Application.Command.Event.RedoRequested -= OnRedoRequested;
		Application.Command.Event.ClearRequested -= OnClearRequested;
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

		Application.Log.Debug($"CommandService Execute: {command.Description}");
		command.Do();
		_undoStack.Push(command);
		_redoStack.Clear();
		Application.Log.Debug($"CommandService Stacks: undo={_undoStack.Count}, redo={_redoStack.Count}");
	}

	/// <summary>
	/// Undo 実行
	/// </summary>
	private void Undo()
	{
		if (_undoStack.Count == 0)
		{
			Application.Log.Debug("CommandService Undo skipped: undo stack is empty.");
			return;
		}

		var cmd = _undoStack.Pop();
		Application.Log.Debug($"CommandService Undo: {cmd.Description}");
		cmd.Undo();
		_redoStack.Push(cmd);
		Application.Log.Debug($"CommandService Stacks: undo={_undoStack.Count}, redo={_redoStack.Count}");
	}

	/// <summary>
	/// Redo 実行
	/// </summary>
	private void Redo()
	{
		if (_redoStack.Count == 0)
		{
			Application.Log.Debug("CommandService Redo skipped: redo stack is empty.");
			return;
		}

		var cmd = _redoStack.Pop();
		Application.Log.Debug($"CommandService Redo: {cmd.Description}");
		cmd.Do();
		_undoStack.Push(cmd);
		Application.Log.Debug($"CommandService Stacks: undo={_undoStack.Count}, redo={_redoStack.Count}");
	}

	/// <summary>
	/// スタックのクリア（シーン切り替え時など）
	/// </summary>
	private void Clear()
	{
		Application.Log.Info($"CommandService Clear: undo={_undoStack.Count}, redo={_redoStack.Count}");
		_undoStack.Clear();
		_redoStack.Clear();
	}

	#endregion
}