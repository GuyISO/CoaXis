// TODO: リファクタリング確認後に削除
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
	private int _cursor = 0;

	#endregion

	#region Properties

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

	#region Actions

	[Signal] public delegate void AskStateRequestedEventHandler();
	/// <summary>
	/// コマンド履歴状態の通知をリクエストする
	/// </summary>
	internal void AskState()
	{
		EmitSignal(SignalName.AskStateRequested);
	}

	#endregion

	#region Notifications

	[Signal] public delegate void StateNotifiedEventHandler(BaseCommand[] history, int cursor);
	/// <summary>
	/// コマンド履歴状態を通知する
	/// </summary>
	/// <param name="history">履歴配列</param>
	/// <param name="cursor">現在カーソル位置（-1 の場合は未実行）</param>
	internal void NotifyState(BaseCommand[] history, int cursor)
	{
		EmitSignal(SignalName.StateNotified, history, cursor);
	}

	#endregion

	#region Events

	/// <summary>
	/// Applicationイベントの購読を開始する
	/// </summary>
	private void SubscribeApplicationEvents()
	{
		Application.Command.AskStateRequested += OnAskStateRequested;
	}

	/// <summary>
	/// Applicationイベントの購読を解除する
	/// </summary>
	private void UnsubscribeApplicationEvents()
	{
		Application.Command.AskStateRequested -= OnAskStateRequested;
	}

	private void OnAskStateRequested()
	{
		NotifyState();
	}

	#endregion

	#region Methods

	/// <summary>
	/// コマンドを実行し、Undoスタックに積む
	/// </summary>
	public void Execute(BaseCommand command)
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
	public void Undo()
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
	public void Redo()
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
	public void Clear()
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
	public void SetCursor(int cursor)
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

	#endregion

	#region Helpers

	/// <summary>
	/// 現在カーソルより後ろの履歴を削除する。
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
		Application.Command.NotifyState(_history.ToArray(), _cursor);
	}

	#endregion
}