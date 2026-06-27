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

    public override void _Ready()
    {
        Instance = this;
    }

    /// <summary>
    /// コマンドを実行し、Undoスタックに積む
    /// </summary>
    public void Execute(CommandBase command)
    {
        command.Do();
        _undoStack.Push(command);
        _redoStack.Clear();
    }

    /// <summary>
    /// Undo 実行
    /// </summary>
    public void Undo()
    {
        if (_undoStack.Count == 0)
            return;

        var cmd = _undoStack.Pop();
        cmd.Undo();
        _redoStack.Push(cmd);
    }

    /// <summary>
    /// Redo 実行
    /// </summary>
    public void Redo()
    {
        if (_redoStack.Count == 0)
            return;

        var cmd = _redoStack.Pop();
        cmd.Do();
        _undoStack.Push(cmd);
    }

    /// <summary>
    /// スタックのクリア（シーン切り替え時など）
    /// </summary>
    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
    }
}