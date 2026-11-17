using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>ModelPropertyの選択状態と変更通知を管理する。</summary>
public partial class ModelPropertySelectionHub : BaseHub
{
    #region Fields
    /// <summary>選択中のProperty識別子集合。</summary>
    private readonly HashSet<Guid> _selectedIds = new();
    #endregion

    #region Properties
    /// <summary>選択中のProperty識別子の複製。</summary>
    public IReadOnlyCollection<Guid> SelectedIds => _selectedIds.ToList().AsReadOnly();
    /// <summary>選択中のProperty数。</summary>
    public int Count => _selectedIds.Count;
    #endregion

    #region Signals
    /// <summary>Propertyの選択状態の変更通知。</summary>
    [Signal] public delegate void SelectedEventHandler(string propertyId);
    /// <summary>選択が解除または置換された通知。</summary>
    [Signal] public delegate void ClearedEventHandler();
    /// <summary>選択集合の変更通知。</summary>
    [Signal] public delegate void SelectionChangedEventHandler();
    #endregion

    #region Lifecycle
    #endregion

    #region Events
    #endregion

    #region Methods
    /// <summary>指定Propertyが選択されているかを取得する。</summary>
    /// <param name="propertyId">確認するProperty識別子</param>
    /// <returns>選択されている場合はtrue</returns>
    public bool Contains(Guid propertyId) => propertyId != Guid.Empty && _selectedIds.Contains(propertyId);

    /// <summary>指定Propertyのみを選択する。</summary>
    /// <param name="propertyId">対象のProperty識別子</param>
    public void Set(Guid propertyId) => Set(new[] { propertyId });

    /// <summary>指定Property群のみを選択する。</summary>
    /// <param name="propertyIds">対象のProperty識別子</param>
    public void Set(Guid[] propertyIds) => Apply(propertyIds, ModelSelectionMode.Set);

    /// <summary>指定Propertyを選択に追加する。</summary>
    /// <param name="propertyId">対象のProperty識別子</param>
    public void Add(Guid propertyId) => Add(new[] { propertyId });

    /// <summary>指定Property群を選択に追加する。</summary>
    /// <param name="propertyIds">対象のProperty識別子</param>
    public void Add(Guid[] propertyIds) => Apply(propertyIds, ModelSelectionMode.Add);

    /// <summary>指定Propertyを選択から外す。</summary>
    /// <param name="propertyId">対象のProperty識別子</param>
    public void Remove(Guid propertyId) => Remove(new[] { propertyId });

    /// <summary>指定Property群を選択から外す。</summary>
    /// <param name="propertyIds">対象のProperty識別子</param>
    public void Remove(Guid[] propertyIds) => Apply(propertyIds, ModelSelectionMode.Remove);

    /// <summary>指定Propertyの選択を切り替える。</summary>
    /// <param name="propertyId">対象のProperty識別子</param>
    public void Toggle(Guid propertyId) => Toggle(new[] { propertyId });

    /// <summary>指定Property群の選択を切り替える。</summary>
    /// <param name="propertyIds">対象のProperty識別子</param>
    public void Toggle(Guid[] propertyIds) => Apply(propertyIds, ModelSelectionMode.Toggle);

    /// <summary>Propertyの選択をすべて解除する。</summary>
    /// <returns>選択が変更された場合はtrue</returns>
    public bool Clear()
    {
        if (_selectedIds.Count == 0)
        {
            return false;
        }

        UpdateSelection(new HashSet<Guid>());
        return true;
    }
    #endregion

    #region Helpers
    /// <summary>指定された操作を選択集合へ適用する。</summary>
    private void Apply(Guid[] propertyIds, ModelSelectionMode mode)
    {
        if (propertyIds == null)
        {
            throw new ArgumentNullException(nameof(propertyIds));
        }

        var validIds = new HashSet<Guid>();
        foreach (Guid propertyId in propertyIds)
        {
            if (propertyId == Guid.Empty || !Application.Model.Property.Registry.IsRegistered(propertyId))
            {
                Application.Log.Warn($"PropertySelectionHub: Property not found. propertyId='{propertyId}'");
                continue;
            }

            validIds.Add(propertyId);
        }

        if (propertyIds.Length > 0 && validIds.Count == 0)
        {
            return;
        }

        var next = new HashSet<Guid>(_selectedIds);
        switch (mode)
        {
            case ModelSelectionMode.Set:
                next = validIds;
                break;
            case ModelSelectionMode.Add:
                next.UnionWith(validIds);
                break;
            case ModelSelectionMode.Remove:
                next.ExceptWith(validIds);
                break;
            case ModelSelectionMode.Toggle:
                next.SymmetricExceptWith(validIds);
                break;
        }

        if (next.Except(_selectedIds).Any())
        {
            Application.Model.Selection.PreparePropertySelection();
        }

        UpdateSelection(next, mode == ModelSelectionMode.Set);
    }

    /// <summary>集合を更新し、変更された識別子と集合を通知する。</summary>
    private void UpdateSelection(HashSet<Guid> next, bool notifyReplacement = false)
    {
        if (_selectedIds.SetEquals(next))
        {
            return;
        }

        bool hadSelection = _selectedIds.Count > 0;
        Guid[] changed = _selectedIds.Except(next).Concat(next.Except(_selectedIds)).ToArray();
        _selectedIds.Clear();
        _selectedIds.UnionWith(next);

        foreach (Guid propertyId in changed)
        {
            EmitSignal(SignalName.Selected, propertyId.ToString());
        }

        if (hadSelection && (_selectedIds.Count == 0 || notifyReplacement))
        {
            EmitSignal(SignalName.Cleared);
        }

        EmitSignal(SignalName.SelectionChanged);
    }
    #endregion
}
