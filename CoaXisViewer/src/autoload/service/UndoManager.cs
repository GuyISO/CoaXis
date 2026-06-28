using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Undo/Redo 管理クラス、Autoload でシングルトン化される
/// </summary>
public partial class UndoManager : Node
{
    public static UndoManager Instance { get; private set; }

    private readonly Stack<CommandBase> _undoStack = new();
    private readonly Stack<CommandBase> _redoStack = new();

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _ExitTree()
    {
        Instance = null;
    }

    /// <summary>
    /// コマンドを実行し、Undoスタックに積む
    /// </summary>
    public void Execute(CommandBase command)
    {
        if (command == null)
        {
            LogHub.Warn("UndoManager.Execute was called with null command.");
            return;
        }

        LogHub.Debug($"UndoManager Execute: {command.Description}");
        command.Do();
        _undoStack.Push(command);
        _redoStack.Clear();
        LogHub.Debug($"UndoManager Stacks: undo={_undoStack.Count}, redo={_redoStack.Count}");
    }

    /// <summary>
    /// Undo 実行
    /// </summary>
    public void Undo()
    {
        if (_undoStack.Count == 0)
        {
            LogHub.Debug("UndoManager Undo skipped: undo stack is empty.");
            return;
        }

        var cmd = _undoStack.Pop();
        LogHub.Debug($"UndoManager Undo: {cmd.Description}");
        cmd.Undo();
        _redoStack.Push(cmd);
        LogHub.Debug($"UndoManager Stacks: undo={_undoStack.Count}, redo={_redoStack.Count}");
    }

    /// <summary>
    /// Redo 実行
    /// </summary>
    public void Redo()
    {
        if (_redoStack.Count == 0)
        {
            LogHub.Debug("UndoManager Redo skipped: redo stack is empty.");
            return;
        }

        var cmd = _redoStack.Pop();
        LogHub.Debug($"UndoManager Redo: {cmd.Description}");
        cmd.Do();
        _undoStack.Push(cmd);
        LogHub.Debug($"UndoManager Stacks: undo={_undoStack.Count}, redo={_redoStack.Count}");
    }

    /// <summary>
    /// スタックのクリア（シーン切り替え時など）
    /// </summary>
    public void Clear()
    {
        LogHub.Info($"UndoManager Clear: undo={_undoStack.Count}, redo={_redoStack.Count}");
        _undoStack.Clear();
        _redoStack.Clear();
    }
}