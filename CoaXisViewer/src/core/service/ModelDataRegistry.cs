using System;
using System.Collections.Generic;
using System.Linq;

public sealed class ModelDataRegistry
{
    #region Fields

    private readonly Dictionary<Guid, AnyModelData> _modelDataById = new();
    private readonly Dictionary<Guid, List<AnyModelData>> _pendingChildrenByParentId = new();

    #endregion

    #region Properties

    public static ModelDataRegistry Instance { get; } = new ModelDataRegistry();

    public IReadOnlyDictionary<Guid, AnyModelData> ModelDataById => _modelDataById;

    #endregion

    #region Constructors

    private ModelDataRegistry()
    {
    }

    #endregion

    #region Public Methods

    public void Register(AnyModelData modelData)
    {
        if (modelData == null)
        {
            throw new ArgumentNullException(nameof(modelData));
        }

        if (modelData.Id == Guid.Empty)
        {
            throw new ArgumentException("ModelData id must not be empty.", nameof(modelData));
        }

        if (_modelDataById.TryGetValue(modelData.Id, out AnyModelData existingModelData) && !ReferenceEquals(existingModelData, modelData))
        {
            Unregister(existingModelData.Id);
        }

        _modelDataById[modelData.Id] = modelData;

        LinkToRegisteredParent(modelData);
        ResolveWaitingChildren(modelData.Id, modelData);
    }

    public AnyModelData GetModelData(Guid id)
    {
        return _modelDataById.TryGetValue(id, out AnyModelData modelData) ? modelData : null;
    }

    public bool TryGetModelData(Guid id, out AnyModelData modelData)
    {
        return _modelDataById.TryGetValue(id, out modelData);
    }

    public bool Unregister(Guid id)
    {
        if (!_modelDataById.TryGetValue(id, out AnyModelData modelData))
        {
            return false;
        }

        AnyModelData parent = GetModelData(modelData.ParentId);
        parent?.DetachChild(modelData);

        foreach (AnyModelData child in modelData.Children.ToList())
        {
            modelData.DetachChild(child);
            AddPendingChild(modelData.Id, child);
        }

        _modelDataById.Remove(id);
        RemovePendingEntryForModel(modelData);

        return true;
    }

    public void ResolveAllReferences()
    {
        foreach (AnyModelData modelData in _modelDataById.Values)
        {
            modelData.ClearChildren();
        }

        _pendingChildrenByParentId.Clear();

        foreach (AnyModelData modelData in _modelDataById.Values)
        {
            LinkToRegisteredParent(modelData);
        }
    }

    public void Clear()
    {
        _modelDataById.Clear();
        _pendingChildrenByParentId.Clear();
    }

    #endregion

    #region Private Methods

    private void LinkToRegisteredParent(AnyModelData modelData)
    {
        if (modelData.ParentId == Guid.Empty)
        {
            return;
        }

        if (_modelDataById.TryGetValue(modelData.ParentId, out AnyModelData parent))
        {
            parent.AttachChild(modelData);
            return;
        }

        AddPendingChild(modelData.ParentId, modelData);
    }

    private void ResolveWaitingChildren(Guid parentId, AnyModelData parent)
    {
        if (!_pendingChildrenByParentId.TryGetValue(parentId, out List<AnyModelData> pendingChildren))
        {
            return;
        }

        foreach (AnyModelData child in pendingChildren)
        {
            parent.AttachChild(child);
        }

        _pendingChildrenByParentId.Remove(parentId);
    }

    private void AddPendingChild(Guid parentId, AnyModelData child)
    {
        if (!_pendingChildrenByParentId.TryGetValue(parentId, out List<AnyModelData> pendingChildren))
        {
            pendingChildren = new List<AnyModelData>();
            _pendingChildrenByParentId[parentId] = pendingChildren;
        }

        if (!pendingChildren.Contains(child))
        {
            pendingChildren.Add(child);
        }
    }

    private void RemovePendingEntryForModel(AnyModelData modelData)
    {
        List<Guid> emptyParentIds = null;

        foreach (KeyValuePair<Guid, List<AnyModelData>> entry in _pendingChildrenByParentId)
        {
            entry.Value.Remove(modelData);

            if (entry.Value.Count == 0)
            {
                emptyParentIds ??= new List<Guid>();
                emptyParentIds.Add(entry.Key);
            }
        }

        if (emptyParentIds == null)
        {
            return;
        }

        foreach (Guid parentId in emptyParentIds)
        {
            _pendingChildrenByParentId.Remove(parentId);
        }
    }

    #endregion
}
