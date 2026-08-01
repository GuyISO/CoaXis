using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// ModelData のインスタンスを Guid で管理するシングルトン
/// </summary>
public sealed class ModelDataRegistry
{
    #region Fields

    private readonly Dictionary<Guid, ModelData> _data = new();

    private readonly List<ModelData> _unresolved = new();

    private readonly Dictionary<Guid, List<ModelData>> _pendingChildrenByParentId = new();

    #endregion

    #region Properties

    public static ModelDataRegistry Instance { get; } = new ModelDataRegistry();

    public IReadOnlyDictionary<Guid, ModelData> ModelDataById => _data;

    #endregion

    #region Constructors

    private ModelDataRegistry()
    {
    }

    #endregion

    #region Public Methods

    public void Register(ModelData modelData)
    {
        if (modelData == null)
        {
            throw new ArgumentNullException(nameof(modelData));
        }

        if (modelData.Id == Guid.Empty)
        {
            throw new ArgumentException("ModelData id must not be empty.", nameof(modelData));
        }

        if (_data.TryGetValue(modelData.Id, out ModelData existingModelData) && !ReferenceEquals(existingModelData, modelData))
        {
            Unregister(existingModelData.Id);
        }

        _data[modelData.Id] = modelData;

        LinkToRegisteredParent(modelData);
        ResolveWaitingChildren(modelData.Id, modelData);
    }

    public ModelData GetModelData(Guid id)
    {
        return _data.TryGetValue(id, out ModelData modelData) ? modelData : null;
    }

    public bool TryGetModelData(Guid id, out ModelData modelData)
    {
        return _data.TryGetValue(id, out modelData);
    }

    public bool Unregister(Guid id)
    {
        if (!_data.TryGetValue(id, out ModelData modelData))
        {
            return false;
        }

        ModelData parent = GetModelData(modelData.ParentId);
        parent?.DetachChild(modelData);

        foreach (ModelData child in modelData.Children.ToList())
        {
            modelData.DetachChild(child);
            AddPendingChild(modelData.Id, child);
        }

        _data.Remove(id);
        RemovePendingEntryForModel(modelData);

        return true;
    }

    public void ResolveAllReferences()
    {
        foreach (ModelData modelData in _data.Values)
        {
            modelData.ClearChildren();
        }

        _pendingChildrenByParentId.Clear();

        foreach (ModelData modelData in _data.Values)
        {
            LinkToRegisteredParent(modelData);
        }
    }

    public void Clear()
    {
        _data.Clear();
        _unresolved.Clear();
    }

    #endregion

    #region Private Methods

    private void LinkToRegisteredParent(ModelData modelData)
    {
        if (modelData.ParentId == Guid.Empty)
        {
            return;
        }

        if (_data.TryGetValue(modelData.ParentId, out ModelData parent))
        {
            parent.AttachChild(modelData);
            return;
        }

        AddPendingChild(modelData.ParentId, modelData);
    }

    private void ResolveWaitingChildren(Guid parentId, ModelData parent)
    {
        if (!_pendingChildrenByParentId.TryGetValue(parentId, out List<ModelData> pendingChildren))
        {
            return;
        }

        foreach (ModelData child in pendingChildren)
        {
            parent.AttachChild(child);
        }

        _pendingChildrenByParentId.Remove(parentId);
    }

    private void AddPendingChild(Guid parentId, ModelData child)
    {
        if (!_pendingChildrenByParentId.TryGetValue(parentId, out List<ModelData> pendingChildren))
        {
            pendingChildren = new List<ModelData>();
            _pendingChildrenByParentId[parentId] = pendingChildren;
        }

        if (!pendingChildren.Contains(child))
        {
            pendingChildren.Add(child);
        }
    }

    private void RemovePendingEntryForModel(ModelData modelData)
    {
        List<Guid> emptyParentIds = null;

        foreach (KeyValuePair<Guid, List<ModelData>> entry in _pendingChildrenByParentId)
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
