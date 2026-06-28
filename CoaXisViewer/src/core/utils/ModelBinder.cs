using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// AnyModel と TreeItem の対応を管理するシングルトンクラスで、Autoload としてシーンに追加して使用する
/// モデルのヒエラルキーを TreeItem に反映するための登録処理と登録解除処理、対応取得処理を提供する
/// </summary>
/// <remarks>
/// 実際の動的ロード時は ModelManager でメインシーンへ追加し、その後 ModelEventHub の通知で HierarchyTree が更新されて ModelBinder に登録される
/// </remarks>
public partial class ModelBinder
{
    // AnyModel ↔ TreeItem の対応辞書
    private static readonly Dictionary<AnyModel, TreeItem> _modelToItem = new();
    private static readonly Dictionary<TreeItem, AnyModel> _itemToModel = new();

    /// <summary>
    /// TreeItem から対応する AnyModel を取得する
    /// </summary>
    /// <param name="item">対応を取得する TreeItem</param>
    /// <returns>対応する AnyModel</returns>
    public static AnyModel GetModel(TreeItem item)
    {
        if (item == null)
        {
            LogHub.Warn("ModelBinder.GetModel called with null item.");
            return null;
        }

        return _itemToModel.TryGetValue(item, out var model) ? model : null;
    }

    /// <summary>
    /// AnyModel から対応する TreeItem を取得する
    /// </summary>
    /// <param name="model">対応を取得する AnyModel</param>
    /// <returns>対応する TreeItem</returns>
    public static TreeItem GetItem(AnyModel model)
    {
        if (model == null)
        {
            LogHub.Warn("ModelBinder.GetItem called with null model.");
            return null;
        }

        return _modelToItem.TryGetValue(model, out var item) ? item : null;
    }

    /// <summary>
    /// AnyModel を登録する
    /// </summary>
    /// <param name="model">登録する AnyModel</param>
    /// <param name="item">対応する TreeItem</param>
    /// <returns>登録に成功した場合は true、すでに登録されている場合は false</returns>
    public static bool Bind(AnyModel model, TreeItem item)
    {
        if (model == null || item == null)
        {
            LogHub.Warn("ModelBinder.Bind skipped: model or item is null.");
            return false;
        }

        if (_modelToItem.ContainsKey(model))
        {
            LogHub.Warn($"ModelBinder.Bind skipped: model '{model.Name}' is already bound.");
            return false; // すでに登録されている
        }

        if (_itemToModel.ContainsKey(item))
        {
            LogHub.Warn("ModelBinder.Bind skipped: tree item is already bound.");
            return false; // すでに登録されている
        }

        _modelToItem[model] = item;
        _itemToModel[item] = model;

        LogHub.Debug($"ModelBinder.Bind: model='{model.Name}', mappings={_modelToItem.Count}");

        return true;
    }

    /// <summary>
    /// AnyModel を登録解除する
    /// </summary>
    /// <param name="model">登録解除する AnyModel</param>
    public static void Unbind(AnyModel model)
    {
        if (model == null)
        {
            LogHub.Warn("ModelBinder.Unbind(model) skipped: model is null.");
            return;
        }

        if (!_modelToItem.TryGetValue(model, out var item))
        {
            LogHub.Debug($"ModelBinder.Unbind(model) skipped: model '{model.Name}' is not bound.");
            return;
        }

        _itemToModel.Remove(item);
        _modelToItem.Remove(model);

        LogHub.Debug($"ModelBinder.Unbind(model): model='{model.Name}', mappings={_modelToItem.Count}");

        item.Free();
    }

    /// <summary>
    /// TreeItem を登録解除する
    /// </summary>
    /// <param name="item">登録解除する TreeItem</param>
    public static void Unbind(TreeItem item)
    {
        if (item == null)
        {
            LogHub.Warn("ModelBinder.Unbind(item) skipped: item is null.");
            return;
        }

        if (!_itemToModel.TryGetValue(item, out var model))
        {
            LogHub.Debug("ModelBinder.Unbind(item) skipped: item is not bound.");
            return;
        }

        _modelToItem.Remove(model);
        _itemToModel.Remove(item);

        LogHub.Debug($"ModelBinder.Unbind(item): model='{model.Name}', mappings={_modelToItem.Count}");

        item.Free();
    }
}
