using Godot;
using System;
using System.Linq;
using System.Collections.Generic;

/// <summary>
/// モデルの選択モードを表す列挙型。
/// </summary>
public enum ModelEntitySelectionMode
{
    /// <summary>対象のみを選択するデフォルトの選択モード。</summary>
    Set,
    /// <summary>追加選択モード。</summary>
    Add,
    /// <summary>削除選択モード。</summary>
    Remove,
    /// <summary>トグル選択モード。</summary>
    Toggle,
}

/// <summary>
/// ModelEntity の選択状態と変更通知を管理するハブ。
/// </summary>
public partial class ModelEntitySelectionHub : BaseHub
{
    #region Fields

    /// <summary>選択中のEntity識別子集合。</summary>
    private readonly HashSet<Guid> _ids = new();

    #endregion

    #region Properties

    /// <summary>現在の選択モードを取得する。</summary>
    internal ModelEntitySelectionMode Mode { get; private set; } = ModelEntitySelectionMode.Set;

    /// <summary>現在の選択実体IDのコレクションの複製を取得する。</summary>
    internal IReadOnlyCollection<Guid> EntityIds => _ids.ToList().AsReadOnly();

    /// <summary>現在の選択モデル実体の数を取得する。</summary>
    internal int Count => _ids.Count;

    #endregion

    #region Signals

    /// <summary>選択モードの変更通知。</summary>
    [Signal] public delegate void ModeNotifiedEventHandler();

    /// <summary>Entityの選択状態の変更通知。選択有無は Contains で参照する。</summary>
    [Signal] public delegate void SelectedEventHandler(string entityId);
    /// <summary>
    /// モデルの選択状態の通知を行う。
    /// </summary>
    /// <param name="entityId">選択状態が変化した ModelEntity の識別子</param>
    private void NotifySelected(Guid entityId)
    {
        EmitSignal(SignalName.Selected, entityId.ToString());
    }

    /// <summary>選択クリアの通知。</summary>
    [Signal] public delegate void ClearedNotifiedEventHandler();
    /// <summary>
    /// 選択がクリアされたことを通知する。
    /// </summary>
    private void NotifyCleared()
    {
        EmitSignal(SignalName.ClearedNotified);
    }

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
    /// Applicationイベントの購読を開始する。
    /// </summary>
    private void SubscribeApplicationEvents()
    {
        Application.Model.Entity.Pick.ResultNotified += OnPickResultNotified;
        Application.Model.Entity.Pick.ResultsNotified += OnPickResultsNotified;
        Application.Model.Entity.Registry.Cleared += OnEntityRegistryCleared;
    }

    /// <summary>
    /// Applicationイベントの購読を解除する。
    /// </summary>
    private void UnsubscribeApplicationEvents()
    {
        Application.Model.Entity.Pick.ResultNotified -= OnPickResultNotified;
        Application.Model.Entity.Pick.ResultsNotified -= OnPickResultsNotified;
        Application.Model.Entity.Registry.Cleared -= OnEntityRegistryCleared;
    }

    /// <summary>
    /// ピック結果の通知を受け取る。
    /// </summary>
    /// <param name="pickResult">通知されたピック結果</param>
    private void OnPickResultNotified(PickResult pickResult)
    {
        if (Application.Model.Entity.Pick.HandlingMode != PickHandlingMode.Selection)
        {
            return; // 選択操作モードでない場合は無視
        }

        // ピック結果が null または実体が null の場合、Setモードの場合は選択をクリアする、Hitしているかは選択においては関係ない
        if (pickResult == null || pickResult.EntityId == Guid.Empty)
        {
            if (Mode == ModelEntitySelectionMode.Set)
            {
                Clear(); // Setモードの場合、ピック結果がない場合は選択をクリアする
            }
            return;
        }

        Guid entityId = pickResult.EntityId;
        switch (Mode)
        {
            case ModelEntitySelectionMode.Set:
                Set(entityId);
                break;
            case ModelEntitySelectionMode.Add:
                Add(entityId);
                break;
            case ModelEntitySelectionMode.Remove:
                Remove(entityId);
                break;
            case ModelEntitySelectionMode.Toggle:
                Toggle(entityId);
                break;
            default:
                Application.Log.Warn($"SelectionHub: Unknown selection mode {Mode}.");
                break;
        }
    }

    /// <summary>
    /// ピック結果の通知を受け取る。
    /// </summary>
    /// <param name="pickResults">ピック結果の配列</param>
    private void OnPickResultsNotified(PickResult[] pickResults)
    {
        if (Application.Model.Entity.Pick.HandlingMode != PickHandlingMode.Selection)
        {
            return; // 選択操作モードでない場合は無視
        }

        if (pickResults == null || pickResults.Length == 0)
        {
            if (Mode == ModelEntitySelectionMode.Set)
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
            if (Mode == ModelEntitySelectionMode.Set)
            {
                Clear();
            }
            return;
        }

        switch (Mode)
        {
            case ModelEntitySelectionMode.Set:
                Set(entityIds);
                break;
            case ModelEntitySelectionMode.Add:
                Add(entityIds);
                break;
            case ModelEntitySelectionMode.Remove:
                Remove(entityIds);
                break;
            case ModelEntitySelectionMode.Toggle:
                Toggle(entityIds);
                break;
            default:
                Application.Log.Warn($"SelectionHub: Unknown selection mode {Mode}.");
                break;
        }
    }

    /// <summary>
    /// モデルレジストリのクリア通知を受け取る。
    /// </summary>
    private void OnEntityRegistryCleared()
    {
        Clear(); // モデルレジストリがクリアされた場合、選択状態もクリアする
    }

    #endregion

    #region Methods

    /// <summary>
    /// 選択モードを設定する。
    /// </summary>
    /// <param name="mode">設定する選択モード</param>
    internal void SetMode(ModelEntitySelectionMode mode)
    {
        if (Mode != mode)
        {
            Mode = mode;
            Application.Log.Debug($"SelectionHub: Selection mode changed to {Mode}.");
        }

        EmitSignal(SignalName.ModeNotified);
    }

    /// <summary>
    /// 現在選択中の実体IDを元に、対応する Node3D 配列を取得する。
    /// </summary>
    /// <returns>選択中のモデルノード配列</returns>
    /// <remarks>選択モデルへのFit処理などに利用</remarks>
    internal Node3D[] GetModelNodeArray()
    {
        return EntityIds
            .Select(entityId => Application.Model.Entity.Registry.GetEntity(entityId)?.Node)
            .Where(node => node != null)
            .Cast<Node3D>()
            .ToArray();
    }

    /// <summary>
    /// 指定した実体IDが選択されているかどうかを確認する。
    /// </summary>
    /// <param name="entityId">確認する実体ID</param>
    /// <returns>実体が選択されている場合はtrue、それ以外の場合はfalseを返す</returns>
    internal bool Contains(Guid entityId) => entityId != Guid.Empty && _ids.Contains(entityId);

    /// <summary>
    /// 指定した実体のみの選択状態にする、既存の選択はすべて解除される。
    /// </summary>
    /// <param name="entityId">選択する実体ID</param>
    internal void Set(Guid entityId)
    {
        Clear();
        Add(entityId);
    }

    /// <summary>
    /// 指定した実体群のみの選択状態にする、既存の選択はすべて解除される。
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
    /// 指定した実体を選択対象に追加する。
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

        if (_ids.Add(entityId))
        {
            NotifySelected(entityId);
            Application.Log.Info($"Selected: {entityId}");
            return true;
        }
        return false;
    }

    /// <summary>
    /// 指定した実体群を選択対象に追加する。
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
    /// 指定した実体を選択対象から外す。
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

        if (_ids.Remove(entityId))
        {
            NotifySelected(entityId);
            Application.Log.Info($"Deselected: {entityId}");
            // 選択状態の実体がなくなった場合、クリア通知も行う
            if (_ids.Count == 0)
            {
                NotifyCleared();
            }
            return true;
        }
        return false;
    }

    /// <summary>
    /// 指定した実体群を選択対象から外す。
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
    /// 指定した実体の選択状態を切り替える。
    /// </summary>
    /// <param name="entityId">切り替える実体ID</param>
    internal void Toggle(Guid entityId)
    {
        if (_ids.Contains(entityId))
        {
            Remove(entityId);
        }
        else
        {
            Add(entityId);
        }
    }

    /// <summary>
    /// 指定した実体群の選択状態を切り替える。
    /// </summary>
    /// <param name="entityIds">切り替える実体IDの配列</param>
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
    /// すべての選択を解除する。
    /// </summary>
    /// <returns>選択状態が変更された場合はtrue、それ以外の場合はfalseを返す</returns>
    internal bool Clear()
    {
        if (_ids.Count == 0)
        {
            return false;
        }

        Guid[] entityIdsToDeselect = _ids.ToArray();

        // 先にクリアしてからシグナル発報することで、シグナルハンドラ内で選択状態確認した際の整合性を保つ
        _ids.Clear();

        // 実体の選択解除シグナルとハイライト解除は個々に行う
        foreach (Guid entityId in entityIdsToDeselect)
        {
            NotifySelected(entityId);
            Application.Log.Info($"Deselected: {entityId}");
        }

        NotifyCleared();
        return true;
    }

    #endregion

    #region Helpers

    #endregion
}