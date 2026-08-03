using System;
using Godot;
using System.Collections.Generic;

public class ModelData
{
    #region Fields

    private readonly Dictionary<string, ModelData> _children = new Dictionary<string, ModelData>();

    #endregion

    #region Properties

    // DTOから受け取った情報
    public Guid Id { get; }
    public Guid ParentId { get; }
    public string Type { get; }
    public string Name { get; }
    public Vector3 Position { get; set; } = Vector3.Zero;
    public Quaternion Rotation { get; set; } = Quaternion.Identity;
    public string IconPath { get; set; } = string.Empty;
    public string GlbPath { get; set; } = string.Empty;
    public string WrlPath { get; set; } = string.Empty;

    // ModelData の状態を表す情報
    public ModelStatus Status { get; set; } = ModelStatus.Unloaded;
    public ModelNode Node { get; set; } = null;

    // 外部から参照するための構造情報
    public ModelData Parent => ParentId != Guid.Empty ? Application.Model.Registry.GetModelData(ParentId) : null;
    public IReadOnlyCollection<ModelData> Children => _children.Values;

    #endregion

    #region Constructors
    public ModelData(Guid id, Guid parentId, string type, string name)
    {
        Id = id;
        ParentId = parentId;
        Type = type ?? string.Empty;
        Name = name ?? string.Empty;
    }
    public ModelData(Guid id, Guid parentId, string name)
        : this(id, parentId, string.Empty, name)
    {
    }
    #endregion

    #region Public Methods

    internal void AttachChild(ModelData child)
    {
        if (child == null)
        {
            return;
        }

        if (_children.ContainsKey(child.Name))
        {
            return;
        }

        _children.Add(child.Name, child);
    }

    internal void DetachChild(ModelData child)
    {
        if (child == null)
        {
            return;
        }

        _children.Remove(child.Name);
    }

    internal void ClearChildren()
    {
        _children.Clear();
    }

    #endregion
}
