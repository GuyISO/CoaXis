using Godot;
using System;

/// <summary>
/// ModelNode の表示状態を変更する Undo/Redo 対応コマンド、バッチで複数モデルの表示状態を変更することも可能
/// </summary>
public sealed partial class SetModelVisibilityCommand : CommandBase
{
    #region Fields

    private readonly Guid[] _entityIds;
    private readonly ModelVisibility[] _previousVisibilities;
    private readonly ModelVisibility _nextVisibility;

    #endregion

    #region Properties

    /// <summary>
    /// コマンドの説明、ログ出力時に使用される
    /// </summary>
    public override string Description => "Set model visibility";

    #endregion

    #region Constructors

    /// <summary>
    /// コンストラクタ、指定されたモデル実体の表示状態を変更するコマンド
    /// </summary>
    /// <param name="entityIds">表示状態を変更する実体IDの配列</param>
    /// <param name="nextVisibility">変更後の表示設定</param>
    public SetModelVisibilityCommand(Guid[] entityIds, ModelVisibility nextVisibility)
    {
        if (entityIds == null)
        {
            throw new ArgumentNullException(nameof(entityIds));
        }

        _entityIds = entityIds;
        _previousVisibilities = new ModelVisibility[_entityIds.Length];
        for (int i = 0; i < _entityIds.Length; i++)
        {
            ModelEntity modelEntity = ResolveModelEntity(_entityIds[i]);
            _previousVisibilities[i] = modelEntity?.Visibility ?? ModelVisibility.Inherit;
        }
        _nextVisibility = nextVisibility;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// コマンドを実行する
    /// </summary>
    public override void Do()
    {
        for (int i = 0; i < _entityIds.Length; i++)
        {
            ModelEntity modelEntity = ResolveModelEntity(_entityIds[i]);
            if (modelEntity?.Node == null || !GodotObject.IsInstanceValid(modelEntity.Node))
            {
                LogSkip("Do", $"model at index {i} is not valid.");
                continue;
            }

            modelEntity.Visibility = _nextVisibility;
            LogDo($"model='{modelEntity.Node.Name}', visibility={_nextVisibility}");
        }

        NotifyEffectiveVisibilityStates();
    }

    /// <summary>
    /// 実行したコマンドを元に戻す
    /// </summary>
    public override void Undo()
    {
        for (int i = 0; i < _entityIds.Length; i++)
        {
            ModelEntity modelEntity = ResolveModelEntity(_entityIds[i]);
            if (modelEntity?.Node == null || !GodotObject.IsInstanceValid(modelEntity.Node))
            {
                LogSkip("Undo", $"model at index {i} is not valid.");
                continue;
            }

            modelEntity.Visibility = _previousVisibilities[i];
            LogUndo($"model='{modelEntity.Node.Name}', visibility={_previousVisibilities[i]}");
        }

        NotifyEffectiveVisibilityStates();
    }

    private static ModelEntity ResolveModelEntity(Guid entityId)
    {
        return entityId == Guid.Empty ? null : Application.Model.Registry.GetEntity(entityId);
    }

    private static void NotifyEffectiveVisibilityStates()
    {
        foreach (ModelEntity modelEntity in Application.Model.Registry.Entities.Values)
        {
            if (modelEntity.Node != null && GodotObject.IsInstanceValid(modelEntity.Node))
            {
                Application.Model.Event.NotifyVisibility(modelEntity.Id, modelEntity.Visibility);
            }
        }
    }

    #endregion
}