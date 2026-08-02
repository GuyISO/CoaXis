using Godot;
using System;

/// <summary>
/// ModelNode の表示状態を変更する Undo/Redo 対応コマンド、バッチで複数モデルの表示状態を変更することも可能
/// </summary>
public sealed partial class SetModelVisibilityCommand : CommandBase
{
    #region Fields

    private readonly Guid[] _modelIds;
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
    /// <param name="modelIds">表示状態を変更するモデルIDの配列</param>
    /// <param name="nextVisible">変更後の表示状態</param>
    public SetModelVisibilityCommand(Guid[] modelIds, bool nextVisible)
    {
        if (modelIds == null)
        {
            throw new ArgumentNullException(nameof(modelIds));
        }

        _modelIds = modelIds;
        _previousVisibles = new bool[_modelIds.Length];
        for (int i = 0; i < _modelIds.Length; i++)
        {
            ModelNode modelNode = ResolveModelNode(_modelIds[i]);
            _previousVisibles[i] = modelNode != null && modelNode.Visible;
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
        for (int i = 0; i < _modelIds.Length; i++)
        {
            ModelNode modelNode = ResolveModelNode(_modelIds[i]);
            if (modelNode == null || !GodotObject.IsInstanceValid(modelNode))
            {
                LogSkip("Do", $"model at index {i} is not valid.");
                continue;
            }

            LogDo($"model='{modelNode.Name}', visible={_nextVisible}");
            Application.Model.Event.NotifyModelVisibilityState(_modelIds[i], _nextVisible);
        }
    }

    /// <summary>
    /// 実行したコマンドを元に戻す
    /// </summary>
    public override void Undo()
    {
        for (int i = 0; i < _modelIds.Length; i++)
        {
            ModelNode modelNode = ResolveModelNode(_modelIds[i]);
            if (modelNode == null || !GodotObject.IsInstanceValid(modelNode))
            {
                LogSkip("Undo", $"model at index {i} is not valid.");
                continue;
            }

            LogUndo($"model='{modelNode.Name}', visible={_previousVisibles[i]}");
            Application.Model.Event.NotifyModelVisibilityState(_modelIds[i], _previousVisibles[i]);
        }
    }

    private static ModelNode ResolveModelNode(Guid modelId)
    {
        if (modelId == Guid.Empty)
        {
            return null;
        }

        RootModelNode rootModelNode = Application.Model.Service?.Root;
        if (rootModelNode == null || !GodotObject.IsInstanceValid(rootModelNode))
        {
            return null;
        }

        return ResolveModelNodeRecursive(rootModelNode, modelId);
    }

    private static ModelNode ResolveModelNodeRecursive(ModelNode modelNode, Guid modelId)
    {
        if (modelNode == null)
        {
            return null;
        }

        if (modelNode.ModelId == modelId)
        {
            return modelNode;
        }

        foreach (ModelNode childModelNode in modelNode.ChildModels)
        {
            ModelNode found = ResolveModelNodeRecursive(childModelNode, modelId);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    #endregion
}