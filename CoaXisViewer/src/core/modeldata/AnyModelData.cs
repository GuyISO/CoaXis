using System;
using Godot;
using System.Collections.Generic;

public class AnyModelData
{
    #region Fields

    private readonly Dictionary<string, AnyModelData> _children = new Dictionary<string, AnyModelData>();

    #endregion

    #region Properties

    public Guid Id { get; }
    public Guid ParentId { get; }
    public string Type { get; }
    public string Name { get; }
    public Vector3 Position { get; set; } = Vector3.Zero;
    public Quaternion Rotation { get; set; } = Quaternion.Identity;
    public string IconPath { get; set; } = string.Empty;
    public string GlbPath { get; set; } = string.Empty;
    public string WrlPath { get; set; } = string.Empty;
    public IReadOnlyCollection<AnyModelData> Children => _children.Values;

    public AnyModelData Parent => ParentId != Guid.Empty ? ModelDataRegistry.Instance.GetModelData(ParentId) : null;

    #endregion

    #region Constructors
    public AnyModelData(Guid id, Guid parentId, string type, string name)
    {
        Id = id;
        ParentId = parentId;
        Type = type ?? string.Empty;
        Name = name ?? string.Empty;
    }
    public AnyModelData(Guid id, Guid parentId, string name)
        : this(id, parentId, string.Empty, name)
    {
    }
    #endregion

    #region Public Methods

    internal void AttachChild(AnyModelData child)
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

    internal void DetachChild(AnyModelData child)
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
