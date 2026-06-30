using Godot;
using System;
using System.Linq;
using System.Collections.Generic;

/// <summary>
/// 選択管理クラス、選択状態の管理と選択変更イベントの発行を担当する
/// Autoloadに登録してシングルトン参照する
/// </summary>
public partial class Selection : Node
{
    #region Fields

    public static Selection Instance { get; private set; }

    private static bool _isMultiSelectMode = false;
    private HashSet<AnyModel> _models = new HashSet<AnyModel>();

    #endregion

    #region Lifecycle

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _Ready()
    {
        if (ModelEventHub.Instance == null)
        {
            LogHub.Warn("Selection: ModelEventHub is not initialized.");
            return;
        }

        // イベントの購読開始
        ModelEventHub.Instance.SetMultiSelectModeRequested += OnSetMultiSelectModeRequested;
        ModelEventHub.Instance.SelectModelRequested += OnSelectModelRequested;
        ModelEventHub.Instance.SelectModelsRequested += OnSelectModelsRequested;
        ModelEventHub.Instance.ClearSelectionRequested += OnClearSelectionRequested;
    }

    public override void _ExitTree()
    {
        if (ModelEventHub.Instance == null)
        {
            return;
        }

        // イベントの購読解除
        ModelEventHub.Instance.SetMultiSelectModeRequested -= OnSetMultiSelectModeRequested;
        ModelEventHub.Instance.SelectModelRequested -= OnSelectModelRequested;
        ModelEventHub.Instance.SelectModelsRequested -= OnSelectModelsRequested;
        ModelEventHub.Instance.ClearSelectionRequested -= OnClearSelectionRequested;

        Instance = null;
    }

    #endregion

    #region Events

    /// <summary>
    /// マルチ選択モードの有効化/無効化要求を受け取る
    /// </summary>
    /// <param name="enable">有効化する場合はtrue、無効化する場合はfalse</param>
    private void OnSetMultiSelectModeRequested(bool enable)
    {
        _isMultiSelectMode = enable;
    }

    /// <summary>
    /// 指定したモデルの選択要求を受け取る
    /// </summary>
    /// <param name="model">選択するモデル</param>
    private void OnSelectModelRequested(AnyModel model)
    {
        if (_isMultiSelectMode)
        {
            Toggle(model);
        }
        else
        {
            Set(model);
        }
    }

    /// <summary>
    /// 指定したモデル群の選択要求を受け取る
    /// </summary>
    /// <param name="models">選択するモデルの配列</param>
    private void OnSelectModelsRequested(AnyModel[] models)
    {
        if (_isMultiSelectMode)
        {
            Toggle(models);
        }
        else
        {
            Set(models);
        }
    }

    /// <summary>
    /// 選択解除要求を受け取る
    /// </summary>
    private void OnClearSelectionRequested()
    {
        Clear();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 現在の選択モデルのコレクションの複製を取得する
    /// </summary>
    public static IReadOnlyCollection<AnyModel> GetModels => Instance._models.ToList().AsReadOnly();

    /// <summary>
    /// 現在の選択モデルの数を取得する
    /// </summary>
    public static int Count => Instance._models.Count;

    /// <summary>
    /// 指定したモデルが選択されているかどうかを確認する
    /// </summary>
    /// <param name="model">確認するモデル</param>
    /// <returns>モデルが選択されている場合はtrue、それ以外の場合はfalseを返す</returns>
    public static bool Contains(AnyModel model) => Instance._models.Contains(model);

    /// <summary>
    /// 選択モデルの配列を取得する
    /// </summary>
    /// <remarks>
    /// 解放済みモデルやツリー外モデルを除外したスナップショットを返す
    /// </remarks>
    public static AnyModel[] GetModelArray()
    {
        return Instance._models
            .Where(model => model != null && GodotObject.IsInstanceValid(model) && model.IsInsideTree())
            .ToArray();
    }

    /// <summary>
    /// 指定したモデルのみの選択状態にする、既存の選択はすべて解除される
    /// </summary>
    /// <param name="model">選択するモデル</param>
    public static void Set(AnyModel model)
    {
        Clear();
        Add(model);
    }

    /// <summary>
    /// 指定したモデル群のみの選択状態にする、既存の選択はすべて解除される
    /// </summary>
    /// <param name="models">選択するモデルの配列</param>
    public static void Set(AnyModel[] models)
    {
        Clear();
        foreach (var model in models)
        {
            Add(model);
        }
    }

    /// <summary>
    /// 指定したモデルを選択対象に追加する
    /// </summary>
    /// <param name="model">選択するモデル</param>
    /// <returns>モデルが新たに選択された場合はtrue、それ以外の場合はfalseを返す</returns>
    /// <remarks>モデルがすでに選択されている場合は何も起こらない</remarks>
    public static bool Add(AnyModel model)
    {
        if (Instance._models.Add(model))
        {
            ModelEventHub.NotifyModelSelectionState(model, true);
            LogHub.Info($"Selected: {model.Name}");
            return true;
        }
        return false;
    }

    /// <summary>
    /// 指定したモデル群を選択対象に追加する
    /// </summary>
    /// <param name="models">選択するモデルの配列</param>
    public static void Add(AnyModel[] models)
    {
        foreach (var model in models)
        {
            Add(model);
        }
    }

    /// <summary>
    /// 指定したモデルを選択対象から外す
    /// </summary>
    /// <param name="model">選択から外すモデル</param>
    /// <returns>モデルが選択から外された場合はtrue、それ以外の場合はfalseを返す</returns>
    /// <remarks>モデルが選択されていない場合は何も起こらない</remarks>
    public static bool Remove(AnyModel model)
    {
        if (Instance._models.Remove(model))
        {
            ModelEventHub.NotifyModelSelectionState(model, false);
            LogHub.Info($"Deselected: {model.Name}");
            return true;
        }
        return false;
    }

    /// <summary>
    /// 指定したモデル群を選択対象から外す
    /// </summary>
    /// <param name="models">選択対象から外すモデルの配列</param>
    public static void Remove(AnyModel[] models)
    {
        foreach (var model in models)
        {
            Remove(model);
        }
    }

    /// <summary>
    /// 指定したモデルの選択状態を切り替える
    /// </summary>
    /// <param name="model">切り替えるモデル</param>
    public static void Toggle(AnyModel model)
    {
        if (Instance._models.Contains(model))
        {
            Remove(model);
        }
        else
        {
            Add(model);
        }
    }

    /// <summary>
    /// 指定したモデル群の選択状態を切り替える
    /// </summary>
    /// <param name="models">切り替えるモデルの列挙体</param>
    public static void Toggle(AnyModel[] models)
    {
        // 切り替えるモデルがない場合は何もしない
        if (models == null || models.Length == 0)
        {
            return;
        }

        foreach (var model in models)
        {
            Toggle(model);
        }
    }

    /// <summary>
    /// すべての選択を解除する
    /// </summary>
    /// <returns>選択状態が変更された場合はtrue、それ以外の場合はfalseを返す</returns>
    public static bool Clear()
    {
        if (Instance._models.Count == 0)
        {
            return false;
        }

        var modelsToDeselect = Instance._models.ToArray();

        // 先にクリアしてからシグナル発報することで、シグナルハンドラ内で選択状態確認した際の整合性を保つ
        Instance._models.Clear();

        // モデルの選択解除シグナルとハイライト解除は個々に行う
        foreach (var model in modelsToDeselect)
        {
            ModelEventHub.NotifyModelSelectionState(model, false);
            LogHub.Info($"Deselected: {model.Name}");
        }
        return true;
    }

    #endregion
}