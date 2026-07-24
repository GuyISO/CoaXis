using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// AnyModel と TreeItem の対応を双方向に管理する
/// モデルから TreeItem、TreeItem からモデルを相互に引ける対応表として利用する
/// </summary>
/// <remarks>
/// このクラスは対応関係の保持と解除のみを担当する
/// TreeItem の生成、表示更新、バインド対象の追加タイミングは呼び出し側で制御する
/// </remarks>
public class ModelBinder
{
    #region Fields

    // AnyModel ↔ TreeItem の対応辞書
    private readonly Dictionary<AnyModel, TreeItem> _modelToTreeItem = new();
    private readonly Dictionary<TreeItem, AnyModel> _treeItemToModel = new();

    #endregion

    #region Public Methods

    /// <summary>
    /// 指定した TreeItem に対応する AnyModel を取得する
    /// </summary>
    /// <param name="treeItem">対応を取得する TreeItem</param>
    /// <returns>対応する AnyModel、対応がない場合は null</returns>
    public AnyModel GetModel(TreeItem treeItem)
    {
        if (treeItem == null)
        {
            Application.Log.Warn("ModelBinder.GetModel called with null tree item.");
            return null;
        }

        return _treeItemToModel.TryGetValue(treeItem, out var model) ? model : null;
    }

    /// <summary>
    /// 指定した AnyModel に対応する TreeItem を取得する
    /// </summary>
    /// <param name="model">対応を取得する AnyModel</param>
    /// <returns>対応する TreeItem、対応がない場合は null</returns>
    public TreeItem GetTreeItem(AnyModel model)
    {
        if (model == null)
        {
            Application.Log.Warn("ModelBinder.GetTreeItem called with null model.");
            return null;
        }

        return _modelToTreeItem.TryGetValue(model, out var treeItem) ? treeItem : null;
    }

    /// <summary>
    /// AnyModel と TreeItem の対応を登録する
    /// </summary>
    /// <param name="model">登録する AnyModel</param>
    /// <param name="treeItem">対応する TreeItem</param>
    /// <returns>
    /// 登録に成功した場合は true
    /// いずれかが null、またはどちらかが既に別の対応で登録済みの場合は false
    /// </returns>
    public bool Bind(AnyModel model, TreeItem treeItem)
    {
        if (model == null || treeItem == null)
        {
            Application.Log.Warn("ModelBinder.Bind skipped: model or tree item is null.");
            return false;
        }

        if (_modelToTreeItem.ContainsKey(model))
        {
            Application.Log.Warn($"ModelBinder.Bind skipped: model '{model.Name}' is already bound.");
            return false; // すでに登録されている
        }

        if (_treeItemToModel.ContainsKey(treeItem))
        {
            Application.Log.Warn("ModelBinder.Bind skipped: tree item is already bound.");
            return false; // すでに登録されている
        }

        _modelToTreeItem[model] = treeItem;
        _treeItemToModel[treeItem] = model;

        Application.Log.Debug($"ModelBinder.Bind: model='{model.Name}', mappings={_modelToTreeItem.Count}");

        return true;
    }

    /// <summary>
    /// 指定した AnyModel の対応を解除し、対応していた TreeItem を解放する
    /// </summary>
    /// <param name="model">登録解除する AnyModel</param>
    public void Unbind(AnyModel model)
    {
        if (model == null)
        {
            Application.Log.Warn("ModelBinder.Unbind(model) skipped: model is null.");
            return;
        }

        if (!_modelToTreeItem.TryGetValue(model, out var treeItem))
        {
            Application.Log.Debug($"ModelBinder.Unbind(model) skipped: model '{model.Name}' is not bound.");
            return;
        }

        _treeItemToModel.Remove(treeItem);
        _modelToTreeItem.Remove(model);

        Application.Log.Debug($"ModelBinder.Unbind(model): model='{model.Name}', mappings={_modelToTreeItem.Count}");

        treeItem.Free();
    }

    /// <summary>
    /// 指定した TreeItem の対応を解除し、TreeItem 自身を解放する
    /// </summary>
    /// <param name="treeItem">登録解除する TreeItem</param>
    public void Unbind(TreeItem treeItem)
    {
        if (treeItem == null)
        {
            Application.Log.Warn("ModelBinder.Unbind(treeItem) skipped: tree item is null.");
            return;
        }

        if (!_treeItemToModel.TryGetValue(treeItem, out var model))
        {
            Application.Log.Debug("ModelBinder.Unbind(treeItem) skipped: tree item is not bound.");
            return;
        }

        _modelToTreeItem.Remove(model);
        _treeItemToModel.Remove(treeItem);

        Application.Log.Debug($"ModelBinder.Unbind(treeItem): model='{model.Name}', mappings={_modelToTreeItem.Count}");

        treeItem.Free();
    }

    /// <summary>
    /// このバインダーが保持する対応をすべて解除する
    /// </summary>
    /// <param name="freeItems">true の場合は、保持しているすべての TreeItem も解放する</param>
    public void Clear(bool freeItems = false)
    {
        if (freeItems)
        {
            foreach (TreeItem treeItem in _treeItemToModel.Keys)
            {
                treeItem?.Free();
            }
        }

        _treeItemToModel.Clear();
        _modelToTreeItem.Clear();

        Application.Log.Debug("ModelBinder.Clear: mappings=0");
    }

    #endregion
}
