using Godot;
using System;
using System.Linq;
using System.Collections.Generic;

/// <summary>
/// 選択管理クラス、選択状態の管理と選択変更イベントの発行を担当する Autoload ノード
/// </summary>
public partial class SelectionService : Node
{
    #region Fields

    private SelectionMode _mode = SelectionMode.Set;

    // 選択状態の管理対象となるモデルのコレクション、HashSet を使用して重複を防ぐ
    private HashSet<AnyModel> _models = new HashSet<AnyModel>();

    #endregion

    #region Lifecycle

    public override void _Ready()
    {
        SubscribeApplicationEvents();
    }

    public override void _ExitTree()
    {
        UnsubscribeApplicationEvents();

        base._ExitTree();
    }

    #endregion

    #region Events

    /// <summary>
    /// Applicationイベントの購読を開始する
    /// </summary>
    private void SubscribeApplicationEvents()
    {
        Application.Selection.Event.SetModeRequested += OnSetModeRequested;
        Application.Selection.Event.ClearRequested += OnClearRequested;
        Application.Pick.Event.ResultNotified += OnPickResultNotified;
        Application.Pick.Event.ResultsNotified += OnPickResultsNotified;
    }

    /// <summary>
    /// Applicationイベントの購読を解除する
    /// </summary>
    private void UnsubscribeApplicationEvents()
    {
        Application.Selection.Event.SetModeRequested -= OnSetModeRequested;
        Application.Selection.Event.ClearRequested -= OnClearRequested;
        Application.Pick.Event.ResultNotified -= OnPickResultNotified;
        Application.Pick.Event.ResultsNotified -= OnPickResultsNotified;
    }

    /// <summary>
    /// マルチ選択モードの有効化/無効化要求を受け取る
    /// </summary>
    /// <param name="enable">有効化する場合はtrue、無効化する場合はfalse</param>
    private void OnSetModeRequested(SelectionMode mode)
    {
        if (_mode != mode)
        {
            _mode = mode;
            Application.Log.Debug($"SelectionService: Selection mode changed to {_mode}.");
            Application.Selection.Event.NotifyMode(_mode);
        }   
    }

    /// <summary>
    /// 選択解除要求を受け取る
    /// </summary>
    private void OnClearRequested()
    {
        Clear();
    }

    /// <summary>
    /// ピック結果の通知を受け取る
    /// </summary>
    /// <param name="pickResult">通知されたピック結果</param>
    private void OnPickResultNotified(PickResult pickResult)
    {
        if (Application.Pick.Service.HandlingMode != PickHandlingMode.Selection)
        {
            return; // 選択操作モードでない場合は無視
        }

        // ピック結果が null またはモデルが null の場合、Setモードの場合は選択をクリアする、Hitしているかは選択においては関係ない
        if (pickResult == null || pickResult.Model == null)
        {
            if (_mode == SelectionMode.Set)
            {
                Clear(); // Setモードの場合、ピック結果がない場合は選択をクリアする
            }
            return;
        }

        AnyModel model = pickResult.Model;
        switch (_mode)
        {
            case SelectionMode.Set:
                Set(model);
                break;
            case SelectionMode.Add:
                Add(model);
                break;
            case SelectionMode.Remove:
                Remove(model);
                break;
            case SelectionMode.Toggle:
                Toggle(model);
                break;
            default:
                Application.Log.Warn($"SelectionService: Unknown selection mode {_mode}.");
                break;
        }
    }

    /// <summary>
    /// ピック結果の通知を受け取る
    /// </summary>
    /// <param name="pickResults">ピック結果の配列</param>
    private void OnPickResultsNotified(PickResult[] pickResults)
    {
        if (Application.Pick.Service.HandlingMode != PickHandlingMode.Selection)
        {
            return; // 選択操作モードでない場合は無視
        }

        if (pickResults == null || pickResults.Length == 0)
        {
            if (_mode == SelectionMode.Set)
            {
                Clear(); // Setモードの場合、ピック結果がない場合は選択をクリアする
            }
            return;
        }

        AnyModel[] models = pickResults.Select(result => result.Model).ToArray();
        switch (_mode)
        {
            case SelectionMode.Set:
                Set(models);
                break;
            case SelectionMode.Add:
                Add(models);
                break;
            case SelectionMode.Remove:
                Remove(models);
                break;
            case SelectionMode.Toggle:
                Toggle(models);
                break;
            default:
                Application.Log.Warn($"SelectionService: Unknown selection mode {_mode}.");
                break;
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 現在の選択モードを取得する
    /// </summary>
    internal SelectionMode Mode => _mode;

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
            Application.Selection.Event.NotifyModelState(model, true);
            Application.Log.Info($"Selected: {model.Name}");
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
            Application.Selection.Event.NotifyModelState(model, false);
            Application.Log.Info($"Deselected: {model.Name}");
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
            Application.Selection.Event.NotifyModelState(model, false);
            Application.Log.Info($"Deselected: {model.Name}");
        }
        return true;
    }

    #endregion
}