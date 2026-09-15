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

    // 選択状態の管理対象となる実体IDのコレクション、HashSet を使用して重複を防ぐ
    private readonly HashSet<Guid> _entityIds = new();

    #endregion

    #region Properties

    /// <summary>
    /// 現在の選択モードを取得する
    /// </summary>
    internal SelectionMode Mode => _mode;

    /// <summary>
    /// 現在の選択実体IDのコレクションの複製を取得する
    /// </summary>
    internal IReadOnlyCollection<Guid> EntityIds => _entityIds.ToList().AsReadOnly();

    /// <summary>
    /// 現在の選択モデル実体の数を取得する
    /// </summary>
    internal int Count => _entityIds.Count;

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
        Application.Model.Event.RegistryCleared += OnModelRegistryCleared;
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
        Application.Model.Event.RegistryCleared -= OnModelRegistryCleared;
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
        }

        Application.Selection.Event.NotifyMode(_mode);
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

        // ピック結果が null または実体が null の場合、Setモードの場合は選択をクリアする、Hitしているかは選択においては関係ない
        if (pickResult == null || pickResult.EntityId == Guid.Empty)
        {
            if (_mode == SelectionMode.Set)
            {
                Clear(); // Setモードの場合、ピック結果がない場合は選択をクリアする
            }
            return;
        }

        Guid entityId = pickResult.EntityId;
        switch (_mode)
        {
            case SelectionMode.Set:
                Set(entityId);
                break;
            case SelectionMode.Add:
                Add(entityId);
                break;
            case SelectionMode.Remove:
                Remove(entityId);
                break;
            case SelectionMode.Toggle:
                Toggle(entityId);
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

        Guid[] entityIds = pickResults
            .Select(result => result.EntityId)
            .Where(entityId => entityId != Guid.Empty)
            .Distinct()
            .ToArray();

        if (entityIds.Length == 0)
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
                Set(entityIds);
                break;
            case SelectionMode.Add:
                Add(entityIds);
                break;
            case SelectionMode.Remove:
                Remove(entityIds);
                break;
            case SelectionMode.Toggle:
                Toggle(entityIds);
                break;
            default:
                Application.Log.Warn($"SelectionService: Unknown selection mode {_mode}.");
                break;
        }
    }

    /// <summary>
    /// モデルレジストリのクリア通知を受け取る
    /// </summary>
    private void OnModelRegistryCleared()
    {
        Clear(); // モデルレジストリがクリアされた場合、選択状態もクリアする
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 現在選択中の実体IDを元に、対応する Node3D 配列を取得する
    /// </summary>
    /// <returns>選択中のモデルノード配列</returns>
    /// <remarks>選択モデルへのFit処理などに利用</remarks>
    internal Node3D[] GetModelNodeArray()
    {
        return EntityIds
            .Select(entityId => Application.Model.Registry.GetEntity(entityId)?.Node)
            .Where(node => node != null)
            .Cast<Node3D>()
            .ToArray();
    }

    /// <summary>
    /// 指定した実体IDが選択されているかどうかを確認する
    /// </summary>
    /// <param name="entityId">確認する実体ID</param>
    /// <returns>実体が選択されている場合はtrue、それ以外の場合はfalseを返す</returns>
    internal bool Contains(Guid entityId) => entityId != Guid.Empty && _entityIds.Contains(entityId);

    /// <summary>
    /// 指定した実体のみの選択状態にする、既存の選択はすべて解除される
    /// </summary>
    /// <param name="entityId">選択する実体ID</param>
    internal void Set(Guid entityId)
    {
        Clear();
        Add(entityId);
    }

    /// <summary>
    /// 指定した実体群のみの選択状態にする、既存の選択はすべて解除される
    /// </summary>
    /// <param name="entityIds">選択する実体IDの配列</param>
    internal void Set(Guid[] entityIds)
    {
        Clear();
        foreach (Guid entityId in entityIds)
        {
            Add(entityId);
        }
    }

    /// <summary>
    /// 指定した実体を選択対象に追加する
    /// </summary>
    /// <param name="entityId">選択する実体ID</param>
    /// <returns>実体が新たに選択された場合はtrue、それ以外の場合はfalseを返す</returns>
    /// <remarks>実体がすでに選択されている場合は何も起こらない</remarks>
    internal bool Add(Guid entityId)
    {
        if (entityId == Guid.Empty)
        {
            return false;
        }

        if (_entityIds.Add(entityId))
        {
            Application.Selection.Event.NotifyModelState(entityId, true);
            Application.Log.Info($"Selected: {entityId}");
            return true;
        }
        return false;
    }

    /// <summary>
    /// 指定した実体群を選択対象に追加する
    /// </summary>
    /// <param name="entityIds">選択する実体IDの配列</param>
    internal void Add(Guid[] entityIds)
    {
        foreach (Guid entityId in entityIds)
        {
            Add(entityId);
        }
    }

    /// <summary>
    /// 指定した実体を選択対象から外す
    /// </summary>
    /// <param name="entityId">選択から外す実体ID</param>
    /// <returns>実体が選択から外された場合はtrue、それ以外の場合はfalseを返す</returns>
    /// <remarks>実体が選択されていない場合は何も起こらない</remarks>
    internal bool Remove(Guid entityId)
    {
        if (entityId == Guid.Empty)
        {
            return false;
        }

        if (_entityIds.Remove(entityId))
        {
            Application.Selection.Event.NotifyModelState(entityId, false);
            Application.Log.Info($"Deselected: {entityId}");
            // 選択状態の実体がなくなった場合、クリア通知も行う
            if (_entityIds.Count == 0)
            {
                Application.Selection.Event.NotifyCleared();
            }
            return true;
        }
        return false;
    }

    /// <summary>
    /// 指定した実体群を選択対象から外す
    /// </summary>
    /// <param name="entityIds">選択対象から外す実体IDの配列</param>
    internal void Remove(Guid[] entityIds)
    {
        foreach (Guid entityId in entityIds)
        {
            Remove(entityId);
        }
    }

    /// <summary>
    /// 指定した実体の選択状態を切り替える
    /// </summary>
    /// <param name="entityId">切り替える実体ID</param>
    internal void Toggle(Guid entityId)
    {
        if (_entityIds.Contains(entityId))
        {
            Remove(entityId);
        }
        else
        {
            Add(entityId);
        }
    }

    /// <summary>
    /// 指定した実体群の選択状態を切り替える
    /// </summary>
    /// <param name="entityIds">切り替える実体IDの列挙体</param>
    internal void Toggle(Guid[] entityIds)
    {
        // 切り替える実体がない場合は何もしない
        if (entityIds == null || entityIds.Length == 0)
        {
            return;
        }

        foreach (Guid entityId in entityIds)
        {
            Toggle(entityId);
        }
    }

    /// <summary>
    /// すべての選択を解除する
    /// </summary>
    /// <returns>選択状態が変更された場合はtrue、それ以外の場合はfalseを返す</returns>
    internal bool Clear()
    {
        if (_entityIds.Count == 0)
        {
            return false;
        }

        Guid[] entityIdsToDeselect = _entityIds.ToArray();

        // 先にクリアしてからシグナル発報することで、シグナルハンドラ内で選択状態確認した際の整合性を保つ
        _entityIds.Clear();

        // 実体の選択解除シグナルとハイライト解除は個々に行う
        foreach (Guid entityId in entityIdsToDeselect)
        {
            Application.Selection.Event.NotifyModelState(entityId, false);
            Application.Log.Info($"Deselected: {entityId}");
        }

        Application.Selection.Event.NotifyCleared();
        return true;
    }

    #endregion
}