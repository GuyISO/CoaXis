using Godot;
using System;

public abstract class CommandBase
{
    public abstract string Description { get; }
    public virtual void Do() { }
    public virtual void Undo() { }
}
