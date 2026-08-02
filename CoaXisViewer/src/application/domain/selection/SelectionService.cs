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

    // 選択状態の管理対象となるモデルIDのコレクション、HashSet を使用して重複を防ぐ
    private readonly HashSet<Guid> _modelIds = new();

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
        if (pickResult == null || pickResult.ModelId == Guid.Empty)
        {
            if (_mode == SelectionMode.Set)
            {
                Clear(); // Setモードの場合、ピック結果がない場合は選択をクリアする
            }
            return;
        }

        Guid modelId = pickResult.ModelId;
        switch (_mode)
        {
            case SelectionMode.Set:
                Set(modelId);
                break;
            case SelectionMode.Add:
                Add(modelId);
                break;
            case SelectionMode.Remove:
                Remove(modelId);
                break;
            case SelectionMode.Toggle:
                Toggle(modelId);
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

        Guid[] modelIds = pickResults
            .Select(result => result.ModelId)
            .Where(modelId => modelId != Guid.Empty)
            .Distinct()
            .ToArray();

        if (modelIds.Length == 0)
        {
            if (_mode == SelectionMode.Set)
            {
                Clear();
            }
            return;
        }

        switch (_mode)
        {
            case SelectionMode.Set:
                Set(modelIds);
                break;
            case SelectionMode.Add:
                Add(modelIds);
                break;
            case SelectionMode.Remove:
                Remove(modelIds);
                break;
            case SelectionMode.Toggle:
                Toggle(modelIds);
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
    /// 現在の選択モデルIDのコレクションの複製を取得する
    /// </summary>
    internal IReadOnlyCollection<Guid> GetModelIds => _modelIds.ToList().AsReadOnly();

    /// <summary>
    /// 現在の選択モデルの数を取得する
    /// </summary>
    internal int Count => _modelIds.Count;

    /// <summary>
    /// 指定したモデルIDが選択されているかどうかを確認する
    /// </summary>
    /// <param name="modelId">確認するモデルID</param>
    /// <returns>モデルが選択されている場合はtrue、それ以外の場合はfalseを返す</returns>
    internal bool Contains(Guid modelId) => modelId != Guid.Empty && _modelIds.Contains(modelId);

    /// <summary>
    /// 指定したモデルが選択されているかどうかを確認する
    /// </summary>
    internal bool Contains(ModelNode modelNode) => modelNode != null && Contains(modelNode.ModelId);

    /// <summary>
    /// 選択モデルIDの配列を取得する
    /// </summary>
    internal Guid[] GetModelIdArray()
    {
        return _modelIds
            .Where(modelId => modelId != Guid.Empty)
            .ToArray();
    }

    /// <summary>
    /// 選択モデルの配列を取得する
    /// </summary>
    /// <remarks>
    /// 解放済みモデルやツリー外モデルを除外したスナップショットを返す
    /// </remarks>
    internal ModelNode[] GetModelArray()
    {
        return _modelIds
            .Select(ResolveModelNode)
            .Where(modelNode => modelNode != null && GodotObject.IsInstanceValid(modelNode) && modelNode.IsInsideTree())
            .ToArray();
    }

    /// <summary>
    /// 指定したモデルのみの選択状態にする、既存の選択はすべて解除される
    /// </summary>
    /// <param name="modelId">選択するモデルID</param>
    internal void Set(Guid modelId)
    {
        Clear();
        Add(modelId);
    }

    /// <summary>
    /// 指定したモデル群のみの選択状態にする、既存の選択はすべて解除される
    /// </summary>
    /// <param name="modelIds">選択するモデルIDの配列</param>
    internal void Set(Guid[] modelIds)
    {
        Clear();
        foreach (Guid modelId in modelIds)
        {
            Add(modelId);
        }
    }

    /// <summary>
    /// 指定したモデルを選択対象に追加する
    /// </summary>
    /// <param name="modelId">選択するモデルID</param>
    /// <returns>モデルが新たに選択された場合はtrue、それ以外の場合はfalseを返す</returns>
    /// <remarks>モデルがすでに選択されている場合は何も起こらない</remarks>
    internal bool Add(Guid modelId)
    {
        if (modelId == Guid.Empty)
        {
            return false;
        }

        if (_modelIds.Add(modelId))
        {
            Application.Selection.Event.NotifyModelState(modelId, true);
            ModelNode modelNode = ResolveModelNode(modelId);
            Application.Log.Info($"Selected: {modelNode?.Name ?? modelId.ToString()}");
            return true;
        }
        return false;
    }

    /// <summary>
    /// 指定したモデル群を選択対象に追加する
    /// </summary>
    /// <param name="modelIds">選択するモデルIDの配列</param>
    internal void Add(Guid[] modelIds)
    {
        foreach (Guid modelId in modelIds)
        {
            Add(modelId);
        }
    }

    /// <summary>
    /// 指定したモデルを選択対象から外す
    /// </summary>
    /// <param name="modelId">選択から外すモデルID</param>
    /// <returns>モデルが選択から外された場合はtrue、それ以外の場合はfalseを返す</returns>
    /// <remarks>モデルが選択されていない場合は何も起こらない</remarks>
    internal bool Remove(Guid modelId)
    {
        if (modelId == Guid.Empty)
        {
            return false;
        }

        if (_modelIds.Remove(modelId))
        {
            Application.Selection.Event.NotifyModelState(modelId, false);
            ModelNode modelNode = ResolveModelNode(modelId);
            Application.Log.Info($"Deselected: {modelNode?.Name ?? modelId.ToString()}");
            // 選択状態のモデルがなくなった場合、クリア通知も行う
            if (_modelIds.Count == 0)
            {
                Application.Selection.Event.NotifyCleared();
            }
            return true;
        }
        return false;
    }

    /// <summary>
    /// 指定したモデル群を選択対象から外す
    /// </summary>
    /// <param name="modelIds">選択対象から外すモデルIDの配列</param>
    internal void Remove(Guid[] modelIds)
    {
        foreach (Guid modelId in modelIds)
        {
            Remove(modelId);
        }
    }

    /// <summary>
    /// 指定したモデルの選択状態を切り替える
    /// </summary>
    /// <param name="modelId">切り替えるモデルID</param>
    internal void Toggle(Guid modelId)
    {
        if (_modelIds.Contains(modelId))
        {
            Remove(modelId);
        }
        else
        {
            Add(modelId);
        }
    }

    /// <summary>
    /// 指定したモデル群の選択状態を切り替える
    /// </summary>
    /// <param name="modelIds">切り替えるモデルIDの列挙体</param>
    internal void Toggle(Guid[] modelIds)
    {
        // 切り替えるモデルがない場合は何もしない
        if (modelIds == null || modelIds.Length == 0)
        {
            return;
        }

        foreach (Guid modelId in modelIds)
        {
            Toggle(modelId);
        }
    }

    /// <summary>
    /// すべての選択を解除する
    /// </summary>
    /// <returns>選択状態が変更された場合はtrue、それ以外の場合はfalseを返す</returns>
    internal bool Clear()
    {
        if (_modelIds.Count == 0)
        {
            return false;
        }

        Guid[] modelIdsToDeselect = _modelIds.ToArray();

        // 先にクリアしてからシグナル発報することで、シグナルハンドラ内で選択状態確認した際の整合性を保つ
        _modelIds.Clear();

        // モデルの選択解除シグナルとハイライト解除は個々に行う
        foreach (Guid modelId in modelIdsToDeselect)
        {
            Application.Selection.Event.NotifyModelState(modelId, false);
            ModelNode modelNode = ResolveModelNode(modelId);
            Application.Log.Info($"Deselected: {modelNode?.Name ?? modelId.ToString()}");
        }

        Application.Selection.Event.NotifyCleared();
        return true;
    }

    private static ModelNode ResolveModelNode(Guid modelId)
    {
        if (modelId == Guid.Empty)
        {
            return null;
        }

        ModelService modelService = Application.Model.Service;
        if (modelService == null)
        {
            return null;
        }

        RootModelNode rootModelNode = modelService.Root;
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