using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// ModelNode のインスタンスを Guid で管理するシングルトン
/// </summary>
public sealed class ModelGuidRegistry
{
    #region Fields

    private readonly Dictionary<ModelNode, Guid> _modelToGuidMap = new();
    private readonly Dictionary<Guid, ModelNode> _guidToModelMap = new();

    #endregion

    #region Properties

    public static ModelGuidRegistry Instance { get; } = new ModelGuidRegistry();

    public IReadOnlyDictionary<ModelNode, Guid> ModelToGuidMap => _modelToGuidMap;

    public IReadOnlyDictionary<Guid, ModelNode> GuidToModelMap => _guidToModelMap;

    #endregion

    #region Constructors

    private ModelGuidRegistry()
    {
    }

    #endregion

    #region Public Methods

    public void RegisterRecursively(ModelNode model)
    {
        if (model == null)
        {
            return;
        }

        Register(model);

        foreach (ModelNode childModel in model.ChildModels)
        {
            RegisterRecursively(childModel);
        }
    }

    public void Clear()
    {
        _modelToGuidMap.Clear();
        _guidToModelMap.Clear();
    }

    #endregion

    #region Internal Helpers

    private void Register(ModelNode model)
    {
        if (model == null || model.Data == null || model.Data.Id == Guid.Empty)
        {
            return;
        }

        if (_modelToGuidMap.TryGetValue(model, out Guid existingGuid))
        {
            _guidToModelMap.Remove(existingGuid);
        }

        if (_guidToModelMap.TryGetValue(model.Data.Id, out ModelNode existingModel) && !ReferenceEquals(existingModel, model))
        {
            _modelToGuidMap.Remove(existingModel);
        }

        _modelToGuidMap[model] = model.Data.Id;
        _guidToModelMap[model.Data.Id] = model;
    }

    #endregion
}