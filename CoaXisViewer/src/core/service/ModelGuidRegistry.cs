using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// AnyModel と Guid の双方向マッピングを管理する
/// </summary>
public class ModelGuidRegistry
{
    #region Fields

    private readonly Dictionary<AnyModel, Guid> _modelToGuid = new();
    private readonly Dictionary<Guid, AnyModel> _guidToModel = new();
    private readonly Dictionary<AnyModel, Action> _modelTreeExitingHandlers = new();

    #endregion

    #region Properties

    public IReadOnlyDictionary<AnyModel, Guid> ModelToGuidMap => _modelToGuid;

    public IReadOnlyDictionary<Guid, AnyModel> GuidToModelMap => _guidToModel;

    #endregion

    #region Public Methods

    /// <summary>
    /// モデルとその子孫を Guid マッピングへ再帰登録する
    /// </summary>
    public void RegisterRecursively(AnyModel model)
    {
        if (model == null)
        {
            return;
        }

        Register(model);

        foreach (AnyModel childModel in model.ChildModels)
        {
            RegisterRecursively(childModel);
        }
    }

    /// <summary>
    /// 1モデル分の Guid マッピングを登録する
    /// </summary>
    public void Register(AnyModel model)
    {
        if (model == null)
        {
            return;
        }

        if (_modelToGuid.ContainsKey(model))
        {
            return;
        }

        Guid guid = model.Guid;
        if (_guidToModel.TryGetValue(guid, out AnyModel mappedModel) && mappedModel != model)
        {
            Application.Log.Warn($"ModelGuidRegistry: duplicate Guid detected. guid='{guid}', model='{model.Name}'");
            return;
        }

        _modelToGuid[model] = guid;
        _guidToModel[guid] = model;

        Action handler = null;
        handler = () =>
        {
            Unregister(model);
        };
        model.TreeExiting += handler;
        _modelTreeExitingHandlers[model] = handler;
    }

    /// <summary>
    /// 1モデル分の Guid マッピングを解除する
    /// </summary>
    public void Unregister(AnyModel model)
    {
        if (model == null)
        {
            return;
        }

        if (_modelTreeExitingHandlers.TryGetValue(model, out Action handler))
        {
            if (GodotObject.IsInstanceValid(model))
            {
                model.TreeExiting -= handler;
            }
            _modelTreeExitingHandlers.Remove(model);
        }

        if (_modelToGuid.TryGetValue(model, out Guid guid))
        {
            _modelToGuid.Remove(model);
            _guidToModel.Remove(guid);
        }
    }

    /// <summary>
    /// 管理中の Guid マッピングをすべて解除する
    /// </summary>
    public void Clear()
    {
        foreach (var pair in _modelTreeExitingHandlers)
        {
            if (GodotObject.IsInstanceValid(pair.Key))
            {
                pair.Key.TreeExiting -= pair.Value;
            }
        }

        _modelTreeExitingHandlers.Clear();
        _modelToGuid.Clear();
        _guidToModel.Clear();
    }

    #endregion
}