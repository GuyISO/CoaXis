using Godot;
using System;

public abstract class CommandBase
{
    public abstract string Description { get; }

    protected void LogDo(string detail = "")
    {
        LogWithDescription("Do", detail);
    }

    protected void LogUndo(string detail = "")
    {
        LogWithDescription("Undo", detail);
    }

    protected void LogSkip(string operation, string reason)
    {
        string message = $"{GetType().Name}.{operation} skipped: {reason}";
        LogHub.Warn(message);
    }

    private void LogWithDescription(string operation, string detail)
    {
        string message = string.IsNullOrWhiteSpace(detail)
            ? $"{GetType().Name}.{operation}: {Description}"
            : $"{GetType().Name}.{operation}: {Description}, {detail}";
        LogHub.Debug(message);
    }

    public virtual void Do() { }
    public virtual void Undo() { }
}
