using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Undo/Redo 管理クラス、インスタンス化せずに静的メソッドで利用する
/// </summary>
public static class UndoService
{
    #region Fields

    private static readonly Stack<CommandBase> _undoStack = new();
    private static readonly Stack<CommandBase> _redoStack = new();

    #endregion

    #region Public Methods

    /// <summary>
    /// コマンドを実行し、Undoスタックに積む
    /// </summary>
    public static void Execute(CommandBase command)
    {
        if (command == null)
        {
            LogHub.Warn("UndoService.Execute was called with null command.");
            return;
        }

        LogHub.Debug($"UndoService Execute: {command.Description}");
        command.Do();
        _undoStack.Push(command);
        _redoStack.Clear();
        LogHub.Debug($"UndoService Stacks: undo={_undoStack.Count}, redo={_redoStack.Count}");
    }

    /// <summary>
    /// Undo 実行
    /// </summary>
    public static void Undo()
    {
        if (_undoStack.Count == 0)
        {
            LogHub.Debug("UndoService Undo skipped: undo stack is empty.");
            return;
        }

        var cmd = _undoStack.Pop();
        LogHub.Debug($"UndoService Undo: {cmd.Description}");
        cmd.Undo();
        _redoStack.Push(cmd);
        LogHub.Debug($"UndoService Stacks: undo={_undoStack.Count}, redo={_redoStack.Count}");
    }

    /// <summary>
    /// Redo 実行
    /// </summary>
    public static void Redo()
    {
        if (_redoStack.Count == 0)
        {
            LogHub.Debug("UndoService Redo skipped: redo stack is empty.");
            return;
        }

        var cmd = _redoStack.Pop();
        LogHub.Debug($"UndoService Redo: {cmd.Description}");
        cmd.Do();
        _undoStack.Push(cmd);
        LogHub.Debug($"UndoService Stacks: undo={_undoStack.Count}, redo={_redoStack.Count}");
    }

    /// <summary>
    /// スタックのクリア（シーン切り替え時など）
    /// </summary>
    public static void Clear()
    {
        LogHub.Info($"UndoService Clear: undo={_undoStack.Count}, redo={_redoStack.Count}");
        _undoStack.Clear();
        _redoStack.Clear();
    }

    #endregion
}