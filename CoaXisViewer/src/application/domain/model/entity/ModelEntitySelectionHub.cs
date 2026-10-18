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
    private readonly HashSet<Guid> _selectedIds = new();

    #endregion

    #region Properties

    /// <summary>現在の選択モードを取得する。</summary>
    public ModelEntitySelectionMode Mode { get; private set; } = ModelEntitySelectionMode.Set;

    /// <summary>現在の選択実体IDのコレクションの複製を取得する。</summary>
    public IReadOnlyCollection<Guid> SelectedIds => _selectedIds.ToList().AsReadOnly();

    /// <summary>現在の選択モデル実体の数を取得する。</summary>
    public int Count => _selectedIds.Count;

    #endregion

    #region Signals

    /// <summary>選択モードの変更通知。</summary>
    [Signal] public delegate void ModeNotifiedEventHandler();

    /// <summary>Entityの選択状態の変更通知。選択有無は Contains で参照する。</summary>
    [Signal] public delegate void SelectedEventHandler(string entityId);

    /// <summary>選択クリアの通知。</summary>
    [Signal] public delegate void ClearedEventHandler();

    /// <summary>選択集合の変更通知。</summary>
    [Signal] public delegate void SelectionChangedEventHandler();

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
    public void SetMode(ModelEntitySelectionMode mode)
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
    public Node3D[] GetModelNodeArray()
    {
        return SelectedIds
            .Select(entityId => Application.Model.Entity.Registry.Get(entityId)?.Node)
            .Where(node => node != null)
            .Cast<Node3D>()
            .ToArray();
    }

    /// <summary>
    /// 指定した実体IDが選択されているかどうかを確認する。
    /// </summary>
    /// <param name="entityId">確認する実体ID</param>
    /// <returns>実体が選択されている場合はtrue、それ以外の場合はfalseを返す</returns>
    public bool Contains(Guid entityId) => entityId != Guid.Empty && _selectedIds.Contains(entityId);

    /// <summary>
    /// 指定した実体のみの選択状態にする、既存の選択はすべて解除される。
    /// </summary>
    /// <param name="entityId">選択する実体ID</param>
    public void Set(Guid entityId)
    {
        Set(new[] { entityId });
    }

    /// <summary>
    /// 指定した実体群のみの選択状態にする、既存の選択はすべて解除される。
    /// </summary>
    /// <param name="entityIds">選択する実体IDの配列</param>
    public void Set(Guid[] entityIds)
    {
        if (entityIds == null)
        {
            throw new ArgumentNullException(nameof(entityIds));
        }

        var newSelectedIds = new HashSet<Guid>(entityIds.Where(entityId => entityId != Guid.Empty));
        if (_selectedIds.SetEquals(newSelectedIds))
        {
            return;
        }

        Guid[] previousSelectedIds = _selectedIds.ToArray();
        var previousSelectedIdSet = new HashSet<Guid>(previousSelectedIds);
        _selectedIds.Clear();
        if (previousSelectedIds.Length > 0)
        {
            EmitSignal(SignalName.Cleared);
        }

        _selectedIds.UnionWith(newSelectedIds);

        foreach (Guid entityId in previousSelectedIds)
        {
            if (!newSelectedIds.Contains(entityId))
            {
                EmitSignal(SignalName.Selected, entityId.ToString());
                Application.Log.Info($"Deselected: {entityId}");
            }
        }

        foreach (Guid entityId in newSelectedIds)
        {
            if (!previousSelectedIdSet.Contains(entityId))
            {
                EmitSignal(SignalName.Selected, entityId.ToString());
                Application.Log.Info($"Selected: {entityId}");
            }
        }

        EmitSignal(SignalName.SelectionChanged);
    }

    /// <summary>
    /// 指定した実体を選択対象に追加する。
    /// </summary>
    /// <param name="entityId">選択する実体ID</param>
    /// <returns>実体が新たに選択された場合はtrue、それ以外の場合はfalseを返す</returns>
    /// <remarks>実体がすでに選択されている場合は何も起こらない</remarks>
    public bool Add(Guid entityId)
    {
        if (entityId == Guid.Empty)
        {
            return false;
        }

        if (AddCore(entityId))
        {
            EmitSignal(SignalName.SelectionChanged);
            return true;
        }
        return false;
    }

    /// <summary>
    /// 指定した実体群を選択対象に追加する。
    /// </summary>
    /// <param name="entityIds">選択する実体IDの配列</param>
    public void Add(Guid[] entityIds)
    {
        if (entityIds == null)
        {
            throw new ArgumentNullException(nameof(entityIds));
        }

        bool changed = false;
        foreach (Guid entityId in entityIds)
        {
            changed |= AddCore(entityId);
        }

        if (changed)
        {
            EmitSignal(SignalName.SelectionChanged);
        }
    }

    /// <summary>
    /// 指定した実体を選択対象から外す。
    /// </summary>
    /// <param name="entityId">選択から外す実体ID</param>
    /// <returns>実体が選択から外された場合はtrue、それ以外の場合はfalseを返す</returns>
    /// <remarks>実体が選択されていない場合は何も起こらない</remarks>
    public bool Remove(Guid entityId)
    {
        if (entityId == Guid.Empty)
        {
            return false;
        }

        if (RemoveCore(entityId))
        {
            NotifyEmptySelection();
            EmitSignal(SignalName.SelectionChanged);
            return true;
        }
        return false;
    }

    /// <summary>
    /// 指定した実体群を選択対象から外す。
    /// </summary>
    /// <param name="entityIds">選択対象から外す実体IDの配列</param>
    public void Remove(Guid[] entityIds)
    {
        if (entityIds == null)
        {
            throw new ArgumentNullException(nameof(entityIds));
        }

        bool changed = false;
        foreach (Guid entityId in entityIds)
        {
            changed |= RemoveCore(entityId);
        }

        if (changed)
        {
            NotifyEmptySelection();
            EmitSignal(SignalName.SelectionChanged);
        }
    }

    /// <summary>
    /// 指定した実体の選択状態を切り替える。
    /// </summary>
    /// <param name="entityId">切り替える実体ID</param>
    public void Toggle(Guid entityId)
    {
        if (ToggleCore(entityId))
        {
            NotifyEmptySelection();
            EmitSignal(SignalName.SelectionChanged);
        }
    }

    /// <summary>
    /// 指定した実体群の選択状態を切り替える。
    /// </summary>
    /// <param name="entityIds">切り替える実体IDの配列</param>
    public void Toggle(Guid[] entityIds)
    {
        // 切り替える実体がない場合は何もしない
        if (entityIds == null || entityIds.Length == 0)
        {
            return;
        }

        bool changed = false;
        foreach (Guid entityId in entityIds)
        {
            changed |= ToggleCore(entityId);
        }

        if (changed)
        {
            NotifyEmptySelection();
            EmitSignal(SignalName.SelectionChanged);
        }
    }

    /// <summary>
    /// すべての選択を解除する。
    /// </summary>
    /// <returns>選択状態が変更された場合はtrue、それ以外の場合はfalseを返す</returns>
    public bool Clear()
    {
        if (_selectedIds.Count == 0)
        {
            return false;
        }

        Guid[] entityIdsToDeselect = _selectedIds.ToArray();

        // 先にクリアしてからシグナル発報することで、シグナルハンドラ内で選択状態確認した際の整合性を保つ
        _selectedIds.Clear();

        // 実体の選択解除シグナルとハイライト解除は個々に行う
        foreach (Guid entityId in entityIdsToDeselect)
        {
            EmitSignal(SignalName.Selected, entityId.ToString());
            Application.Log.Info($"Deselected: {entityId}");
        }

        EmitSignal(SignalName.Cleared);
        EmitSignal(SignalName.SelectionChanged);
        return true;
    }

    #endregion

    #region Helpers

    /// <summary>
    /// 選択対象へ追加し、Entity単位の変更通知を発行する。
    /// </summary>
    private bool AddCore(Guid entityId)
    {
        if (entityId == Guid.Empty || !_selectedIds.Add(entityId))
        {
            return false;
        }

        EmitSignal(SignalName.Selected, entityId.ToString());
        Application.Log.Info($"Selected: {entityId}");
        return true;
    }

    /// <summary>
    /// 選択対象から解除し、Entity単位の変更通知を発行する。
    /// </summary>
    private bool RemoveCore(Guid entityId)
    {
        if (entityId == Guid.Empty || !_selectedIds.Remove(entityId))
        {
            return false;
        }

        EmitSignal(SignalName.Selected, entityId.ToString());
        Application.Log.Info($"Deselected: {entityId}");
        return true;
    }

    /// <summary>
    /// 指定Entityの選択状態を切り替える。
    /// </summary>
    private bool ToggleCore(Guid entityId)
    {
        return _selectedIds.Contains(entityId)
            ? RemoveCore(entityId)
            : AddCore(entityId);
    }

    /// <summary>
    /// 選択解除後に選択対象がなくなった場合の通知を発行する。
    /// </summary>
    private void NotifyEmptySelection()
    {
        if (_selectedIds.Count == 0)
        {
            EmitSignal(SignalName.Cleared);
        }
    }

    #endregion
}