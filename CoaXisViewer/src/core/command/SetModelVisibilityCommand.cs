using Godot;
using System;

/// <summary>
/// AnyModel の表示状態を変更する Undo/Redo 対応コマンド、バッチで複数モデルの表示状態を変更することも可能
/// </summary>
public sealed class SetModelVisibilityCommand : CommandBase
{
    #region Fields

    private readonly AnyModel[] _models;
    private readonly bool[] _previousVisibles;
    private readonly bool _nextVisible;

    #endregion

    #region Properties

    /// <summary>
    /// コマンドの説明、ログ出力時に使用される
    /// </summary>
    public override string Description => "Set model visibility";

    #endregion

    #region Constructors

    /// <summary>
    /// コンストラクタ、指定されたモデルの表示状態を変更するコマンド
    /// </summary>
    /// <param name="models">表示状態を変更するモデルの配列</param>
    /// <param name="nextVisible">変更後の表示状態</param>
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

    #endregion

    #region Public Methods

    /// <summary>
    /// コマンドを実行する
    /// </summary>
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
            Application.Model.NotifyModelVisibilityState(_models[i], _nextVisible);
        }
    }

    /// <summary>
    /// 実行したコマンドを元に戻す
    /// </summary>
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
            Application.Model.NotifyModelVisibilityState(_models[i], _previousVisibles[i]);
        }
    }

    #endregion
}