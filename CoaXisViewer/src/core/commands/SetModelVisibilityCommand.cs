using Godot;
using System;

/// <summary>
/// AnyModel の表示状態を変更する Undo/Redo 対応コマンド、バッチで複数モデルの表示状態を変更することも可能
/// </summary>
public sealed class SetModelVisibilityCommand : CommandBase
{
    private readonly AnyModel[] _models;
    private readonly bool[] _previousVisibles;
    private readonly bool _nextVisible;

    public override string Description => "Set model visibility";

    public SetModelVisibilityCommand(AnyModel[] models, bool nextVisible)
    {
        if (models == null)
        {
            throw new ArgumentNullException(nameof(models));
        }

        _models = models;
        _previousVisibles = new bool[_models.Length];
        for (int i = 0; i < _models.Length; i++)
        {
            _previousVisibles[i] = _models[i].Visible;
        }
        _nextVisible = nextVisible;
    }

    public override void Do()
    {
        for (int i = 0; i < _models.Length; i++)
        {
            if (!GodotObject.IsInstanceValid(_models[i]))
            {
                LogSkip("Do", $"model at index {i} is not valid.");
                continue;
            }

            LogDo($"model='{_models[i].Name}', visible={_nextVisible}");
            ModelEventHub.NotifyModelVisibilityState(_models[i], _nextVisible);
        }
    }

    public override void Undo()
    {
        for (int i = 0; i < _models.Length; i++)
        {
            if (!GodotObject.IsInstanceValid(_models[i]))
            {
                LogSkip("Undo", $"model at index {i} is not valid.");
                continue;
            }

            LogUndo($"model='{_models[i].Name}', visible={_previousVisibles[i]}");
            ModelEventHub.NotifyModelVisibilityState(_models[i], _previousVisibles[i]);
        }
    }
}