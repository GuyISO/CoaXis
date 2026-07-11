using Godot;
using System;
using System.Linq;
using System.Collections.Generic;

/// <summary>
/// 選択管理クラス、選択状態の管理と選択変更イベントの発行を担当する Autoload ノード
/// </summary>
public partial class Selection : Node
{
    #region Fields

    private bool _isInitialized = false;
    private bool _isMultiSelectionMode = false;
    private PickHandlingMode _currentPickHandlingMode;
    private HashSet<AnyModel> _models = new HashSet<AnyModel>();

    #endregion

    #region Lifecycle

    public override void _Ready()
    {
        // イベントの購読開始
        Application.Model.SetMultiSelectionModeRequested += OnSetMultiSelectionModeRequested;
        Application.Model.ClearSelectionRequested += OnClearSelectionRequested;
        Application.Pick.PickHandlingModeNotified += OnPickHandlingModeNotified;
        Application.Pick.PickResultNotified += OnPickResultNotified;
        Application.Pick.PickResultsNotified += OnPickResultsNotified;
    }

    public override void _ExitTree()
    {
        // イベントの購読解除
        Application.Model.SetMultiSelectionModeRequested -= OnSetMultiSelectionModeRequested;
        Application.Model.ClearSelectionRequested -= OnClearSelectionRequested;
        Application.Pick.PickHandlingModeNotified -= OnPickHandlingModeNotified;
        Application.Pick.PickResultNotified -= OnPickResultNotified;
        Application.Pick.PickResultsNotified -= OnPickResultsNotified;

        base._ExitTree();
    }

    public override void _Process(double delta)
    {
        if (!_isInitialized)
        {
            Application.Pick.RequestNotifyPickHandlingMode();
        }
    }

    #endregion

    #region Events

    /// <summary>
    /// マルチ選択モードの有効化/無効化要求を受け取る
    /// </summary>
    /// <param name="enable">有効化する場合はtrue、無効化する場合はfalse</param>
    private void OnSetMultiSelectionModeRequested(bool enable)
    {
        _isMultiSelectionMode = enable;
    }

    /// <summary>
    /// 選択解除要求を受け取る
    /// </summary>
    private void OnClearSelectionRequested()
    {
        Clear();
    }

    /// <summary>
    /// 選択操作モードの通知を受け取る
    /// </summary>
    /// <param name="mode">通知された選択操作モード</param>
    private void OnPickHandlingModeNotified(PickHandlingMode mode)
    {
        // 初回通知を受け取った時点で初期化済みとする
        _isInitialized = true;
        _currentPickHandlingMode = mode;
    }

    /// <summary>
    /// ピック結果の通知を受け取る
    /// </summary>
    /// <param name="pickResult">通知されたピック結果</param>
    private void OnPickResultNotified(PickResult pickResult)
    {
        if (_currentPickHandlingMode != PickHandlingMode.Selection)
        {
            return; // 選択操作モードでない場合は無視
        }

        // ピック結果が空なら選択状態クリア
        if (pickResult == null || pickResult.Model == null)
        {
            Clear();
            return;
        }

        if (_isMultiSelectionMode)
        {
            Toggle(pickResult.Model);
        }
        else
        {
            Set(pickResult.Model);
        }
    }

    /// <summary>
    /// ピック結果の通知を受け取る
    /// </summary>
    /// <param name="pickResults">ピック結果の配列</param>
    private void OnPickResultsNotified(PickResult[] pickResults)
    {
        if (_currentPickHandlingMode != PickHandlingMode.Selection)
        {
            return; // 選択操作モードでない場合は無視
        }

        // ピック結果が空なら選択状態クリア
        if (pickResults == null || pickResults.Length == 0)
        {
            Clear();
            return;
        }

        var models = pickResults.Select(pr => pr.Model).Where(m => m != null).ToArray();
        if (_isMultiSelectionMode)
        {
            Toggle(models);
        }
        else
        {
            Set(models);
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 現在の選択モデルのコレクションの複製を取得する
    /// </summary>
    internal IReadOnlyCollection<AnyModel> GetModels => _models.ToList().AsReadOnly();

    /// <summary>
    /// 現在の選択モデルの数を取得する
    /// </summary>
    internal int Count => _models.Count;

    /// <summary>
    /// 指定したモデルが選択されているかどうかを確認する
    /// </summary>
    /// <param name="model">確認するモデル</param>
    /// <returns>モデルが選択されている場合はtrue、それ以外の場合はfalseを返す</returns>
    internal bool Contains(AnyModel model) => _models.Contains(model);

    /// <summary>
    /// 選択モデルの配列を取得する
    /// </summary>
    /// <remarks>
    /// 解放済みモデルやツリー外モデルを除外したスナップショットを返す
    /// </remarks>
    internal AnyModel[] GetModelArray()
    {
        return _models
            .Where(model => model != null && GodotObject.IsInstanceValid(model) && model.IsInsideTree())
            .ToArray();
    }

    /// <summary>
    /// 指定したモデルのみの選択状態にする、既存の選択はすべて解除される
    /// </summary>
    /// <param name="model">選択するモデル</param>
    internal void Set(AnyModel model)
    {
        Clear();
        Add(model);
    }

    /// <summary>
    /// 指定したモデル群のみの選択状態にする、既存の選択はすべて解除される
    /// </summary>
    /// <param name="models">選択するモデルの配列</param>
    internal void Set(AnyModel[] models)
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
    internal bool Add(AnyModel model)
    {
        if (_models.Add(model))
        {
            Application.Model.NotifyModelSelectionState(model, true);
            Application.Logger.Info($"Selected: {model.Name}");
            return true;
        }
        return false;
    }

    /// <summary>
    /// 指定したモデル群を選択対象に追加する
    /// </summary>
    /// <param name="models">選択するモデルの配列</param>
    internal void Add(AnyModel[] models)
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
    internal bool Remove(AnyModel model)
    {
        if (_models.Remove(model))
        {
            Application.Model.NotifyModelSelectionState(model, false);
            Application.Logger.Info($"Deselected: {model.Name}");
            return true;
        }
        return false;
    }

    /// <summary>
    /// 指定したモデル群を選択対象から外す
    /// </summary>
    /// <param name="models">選択対象から外すモデルの配列</param>
    internal void Remove(AnyModel[] models)
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
    internal void Toggle(AnyModel model)
    {
        if (_models.Contains(model))
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
    internal void Toggle(AnyModel[] models)
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
    internal bool Clear()
    {
        if (_models.Count == 0)
        {
            return false;
        }

        var modelsToDeselect = _models.ToArray();

        // 先にクリアしてからシグナル発報することで、シグナルハンドラ内で選択状態確認した際の整合性を保つ
        _models.Clear();

        // モデルの選択解除シグナルとハイライト解除は個々に行う
        foreach (var model in modelsToDeselect)
        {
            Application.Model.NotifyModelSelectionState(model, false);
            Application.Logger.Info($"Deselected: {model.Name}");
        }
        return true;
    }

    #endregion
}