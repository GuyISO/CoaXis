using Godot;
using System;

/// <summary>
/// ModelNode の表示状態を変更する Undo/Redo 対応コマンド、バッチで複数モデルの表示状態を変更することも可能
/// </summary>
public sealed partial class SetModelVisibilityCommand : CommandBase
{
    #region Fields

    private readonly Guid[] _modelIds;
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
    /// コンストラクタ、指定されたモデルの表示状態を変更するコマンド
    /// </summary>
    /// <param name="modelIds">表示状態を変更するモデルIDの配列</param>
    /// <param name="nextVisibility">変更後の表示設定</param>
    public SetModelVisibilityCommand(Guid[] modelIds, ModelVisibility nextVisibility)
    {
        if (modelIds == null)
        {
            throw new ArgumentNullException(nameof(modelIds));
        }

        _modelIds = modelIds;
        _previousVisibilities = new ModelVisibility[_modelIds.Length];
        for (int i = 0; i < _modelIds.Length; i++)
        {
            ModelData modelData = ResolveModelData(_modelIds[i]);
            _previousVisibilities[i] = modelData?.Visibility ?? ModelVisibility.Inherit;
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
        for (int i = 0; i < _modelIds.Length; i++)
        {
            ModelData modelData = ResolveModelData(_modelIds[i]);
            if (modelData?.Node == null || !GodotObject.IsInstanceValid(modelData.Node))
            {
                LogSkip("Do", $"model at index {i} is not valid.");
                continue;
            }

            modelData.Visibility = _nextVisibility;
            LogDo($"model='{modelData.Node.Name}', visibility={_nextVisibility}");
        }

        NotifyEffectiveVisibilityStates();
    }

    /// <summary>
    /// 実行したコマンドを元に戻す
    /// </summary>
    public override void Undo()
    {
        for (int i = 0; i < _modelIds.Length; i++)
        {
            ModelData modelData = ResolveModelData(_modelIds[i]);
            if (modelData?.Node == null || !GodotObject.IsInstanceValid(modelData.Node))
            {
                LogSkip("Undo", $"model at index {i} is not valid.");
                continue;
            }

            modelData.Visibility = _previousVisibilities[i];
            LogUndo($"model='{modelData.Node.Name}', visibility={_previousVisibilities[i]}");
        }

        NotifyEffectiveVisibilityStates();
    }

    private static ModelData ResolveModelData(Guid modelId)
    {
        return modelId == Guid.Empty ? null : Application.Model.Registry.GetModelData(modelId);
    }

    private static void NotifyEffectiveVisibilityStates()
    {
        foreach (ModelData modelData in Application.Model.Registry.DataSet.Values)
        {
            if (modelData.Node != null && GodotObject.IsInstanceValid(modelData.Node))
            {
                Application.Model.Event.NotifyModelVisibilityState(
                    modelData.Id,
                    ModelVisibilityResolver.IsVisible(modelData));
            }
        }
    }

    #endregion
}