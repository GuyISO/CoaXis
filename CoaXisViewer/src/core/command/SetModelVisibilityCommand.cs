// TODO: リファクタリング確認後に削除
using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// ModelNode の表示状態を変更する Undo/Redo 対応コマンド、バッチで複数モデルの表示状態を変更することも可能
/// </summary>
public sealed partial class SetModelVisibilityCommand : BaseCommand
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

    #region public Methods

    /// <summary>
    /// コマンドを実行する
    /// </summary>
    public override void Do()
    {
        var changedEntityIds = new HashSet<Guid>();
        for (int i = 0; i < _entityIds.Length; i++)
        {
            ModelEntity modelEntity = ResolveModelEntity(_entityIds[i]);
            if (modelEntity?.Node == null || !GodotObject.IsInstanceValid(modelEntity.Node))
            {
                LogSkip("Do", $"model at index {i} is not valid.");
                continue;
            }

            if (modelEntity.Visibility == _nextVisibility)
            {
                continue;
            }

            modelEntity.Visibility = _nextVisibility;
            changedEntityIds.Add(modelEntity.Id);
            LogDo($"model='{modelEntity.Node.Name}', visibility={_nextVisibility}");
        }

        NotifyAffectedVisibilityStates(changedEntityIds);
    }

    /// <summary>
    /// 実行したコマンドを元に戻す
    /// </summary>
    public override void Undo()
    {
        var changedEntityIds = new HashSet<Guid>();
        for (int i = 0; i < _entityIds.Length; i++)
        {
            ModelEntity modelEntity = ResolveModelEntity(_entityIds[i]);
            if (modelEntity?.Node == null || !GodotObject.IsInstanceValid(modelEntity.Node))
            {
                LogSkip("Undo", $"model at index {i} is not valid.");
                continue;
            }

            if (modelEntity.Visibility == _previousVisibilities[i])
            {
                continue;
            }

            modelEntity.Visibility = _previousVisibilities[i];
            changedEntityIds.Add(modelEntity.Id);
            LogUndo($"model='{modelEntity.Node.Name}', visibility={_previousVisibilities[i]}");
        }

        NotifyAffectedVisibilityStates(changedEntityIds);
    }

    private static ModelEntity ResolveModelEntity(Guid entityId)
    {
        return entityId == Guid.Empty ? null : Application.Model.Entity.Registry.Get(entityId);
    }

    private static void NotifyAffectedVisibilityStates(HashSet<Guid> changedEntityIds)
    {
        var affectedEntityIds = new HashSet<Guid>();
        foreach (Guid entityId in changedEntityIds)
        {
            foreach (ModelEntity descendant in Application.Model.Entity.Registry.GetDescendants(entityId))
            {
                if (descendant != null && !changedEntityIds.Contains(descendant.Id))
                {
                    affectedEntityIds.Add(descendant.Id);
                }
            }
        }

        foreach (Guid entityId in affectedEntityIds)
        {
            ModelEntity modelEntity = ResolveModelEntity(entityId);
            modelEntity?.NotifyVisibilityChanged();
        }
    }

    #endregion
}