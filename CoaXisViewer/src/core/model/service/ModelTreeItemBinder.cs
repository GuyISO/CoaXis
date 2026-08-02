using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// ModelNode と TreeItem の対応を双方向に管理する
/// モデルから TreeItem、TreeItem からモデルを相互に引ける対応表として利用する
/// </summary>
/// <remarks>
/// このクラスは対応関係の保持と解除のみを担当する
/// TreeItem の生成、表示更新、バインド対象の追加タイミングは呼び出し側で制御する
/// </remarks>
public class ModelTreeItemBinder
{
    #region Fields

    // ModelId ↔ TreeItem の対応辞書
    private readonly Dictionary<Guid, TreeItem> _modelIdToTreeItem = new();
    private readonly Dictionary<TreeItem, Guid> _treeItemToModelId = new();

    #endregion

    #region Public Methods

    /// <summary>
    /// 指定した TreeItem に対応する ModelId を取得する
    /// </summary>
    /// <param name="treeItem">対応を取得する TreeItem</param>
    /// <returns>対応する ModelId、対応がない場合は Guid.Empty</returns>
    public Guid GetModelId(TreeItem treeItem)
    {
        if (treeItem == null)
        {
            Application.Log.Warn("ModelTreeItemBinder.GetModelId called with null tree item.");
            return Guid.Empty;
        }

        return _treeItemToModelId.TryGetValue(treeItem, out Guid modelId) ? modelId : Guid.Empty;
    }

    /// <summary>
    /// 指定した TreeItem に対応する ModelNode を取得する
    /// </summary>
    public ModelNode GetModelNode(TreeItem treeItem)
    {
        Guid modelId = GetModelId(treeItem);
        return ResolveModelNode(modelId);
    }

    /// <summary>
    /// 指定した ModelId に対応する TreeItem を取得する
    /// </summary>
    /// <param name="modelId">対応を取得する ModelId</param>
    /// <returns>対応する TreeItem、対応がない場合は null</returns>
    public TreeItem GetTreeItem(Guid modelId)
    {
        if (modelId == Guid.Empty)
        {
            Application.Log.Warn("ModelTreeItemBinder.GetTreeItem called with empty modelId.");
            return null;
        }

        return _modelIdToTreeItem.TryGetValue(modelId, out TreeItem treeItem) ? treeItem : null;
    }

    /// <summary>
    /// 指定した ModelNode に対応する TreeItem を取得する
    /// </summary>
    /// <param name="modelNode">対応を取得する ModelNode</param>
    /// <returns>対応する TreeItem、対応がない場合は null</returns>
    public TreeItem GetTreeItem(ModelNode modelNode)
    {
        if (modelNode == null)
        {
            Application.Log.Warn("ModelTreeItemBinder.GetTreeItem called with null model.");
            return null;
        }

        return GetTreeItem(modelNode.ModelId);
    }

    /// <summary>
    /// ModelId と TreeItem の対応を登録する
    /// </summary>
    /// <param name="modelId">登録する ModelId</param>
    /// <param name="treeItem">対応する TreeItem</param>
    /// <returns>
    /// 登録に成功した場合は true
    /// いずれかが null、またはどちらかが既に別の対応で登録済みの場合は false
    /// </returns>
    public bool Bind(Guid modelId, TreeItem treeItem)
    {
        if (modelId == Guid.Empty || treeItem == null)
        {
            Application.Log.Warn("ModelTreeItemBinder.Bind skipped: modelId is empty or tree item is null.");
            return false;
        }

        if (_modelIdToTreeItem.ContainsKey(modelId))
        {
            Application.Log.Warn($"ModelTreeItemBinder.Bind skipped: modelId '{modelId}' is already bound.");
            return false; // すでに登録されている
        }

        if (_treeItemToModelId.ContainsKey(treeItem))
        {
            Application.Log.Warn("ModelTreeItemBinder.Bind skipped: tree item is already bound.");
            return false; // すでに登録されている
        }

        _modelIdToTreeItem[modelId] = treeItem;
        _treeItemToModelId[treeItem] = modelId;

        Application.Log.Debug($"ModelTreeItemBinder.Bind: modelId='{modelId}', mappings={_modelIdToTreeItem.Count}");

        return true;
    }

    /// <summary>
    /// ModelNode と TreeItem の対応を登録する
    /// </summary>
    public bool Bind(ModelNode modelNode, TreeItem treeItem)
    {
        if (modelNode == null)
        {
            return false;
        }

        return Bind(modelNode.ModelId, treeItem);
    }

    /// <summary>
    /// 指定した ModelId の対応を解除し、対応していた TreeItem を解放する
    /// </summary>
    /// <param name="modelId">登録解除する ModelId</param>
    public void Unbind(Guid modelId)
    {
        if (modelId == Guid.Empty)
        {
            Application.Log.Warn("ModelTreeItemBinder.Unbind(modelId) skipped: modelId is empty.");
            return;
        }

        if (!_modelIdToTreeItem.TryGetValue(modelId, out TreeItem treeItem))
        {
            Application.Log.Debug($"ModelTreeItemBinder.Unbind(modelId) skipped: modelId '{modelId}' is not bound.");
            return;
        }

        _treeItemToModelId.Remove(treeItem);
        _modelIdToTreeItem.Remove(modelId);

        Application.Log.Debug($"ModelTreeItemBinder.Unbind(modelId): modelId='{modelId}', mappings={_modelIdToTreeItem.Count}");

        treeItem.Free();
    }

    /// <summary>
    /// 指定した ModelNode の対応を解除し、対応していた TreeItem を解放する
    /// </summary>
    public void Unbind(ModelNode modelNode)
    {
        if (modelNode == null)
        {
            Application.Log.Warn("ModelTreeItemBinder.Unbind(modelNode) skipped: modelNode is null.");
            return;
        }

        Unbind(modelNode.ModelId);
    }

    /// <summary>
    /// 指定した TreeItem の対応を解除し、TreeItem 自身を解放する
    /// </summary>
    /// <param name="treeItem">登録解除する TreeItem</param>
    public void Unbind(TreeItem treeItem)
    {
        if (treeItem == null)
        {
            Application.Log.Warn("ModelTreeItemBinder.Unbind(treeItem) skipped: tree item is null.");
            return;
        }

        if (!_treeItemToModelId.TryGetValue(treeItem, out Guid modelId))
        {
            Application.Log.Debug("ModelTreeItemBinder.Unbind(treeItem) skipped: tree item is not bound.");
            return;
        }

        _modelIdToTreeItem.Remove(modelId);
        _treeItemToModelId.Remove(treeItem);

        Application.Log.Debug($"ModelTreeItemBinder.Unbind(treeItem): modelId='{modelId}', mappings={_modelIdToTreeItem.Count}");

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
            foreach (TreeItem treeItem in _treeItemToModelId.Keys)
            {
                treeItem?.Free();
            }
        }

        _treeItemToModelId.Clear();
        _modelIdToTreeItem.Clear();

        Application.Log.Debug("ModelTreeItemBinder.Clear: mappings=0");
    }

    private static ModelNode ResolveModelNode(Guid modelId)
    {
        if (modelId == Guid.Empty)
        {
            return null;
        }

        RootModelNode rootModelNode = (RootModelNode)Application.Model.Service?.Root.Node;
        if (rootModelNode == null || !GodotObject.IsInstanceValid(rootModelNode))
        {
            return null;
        }

        return ResolveModelNodeRecursive(rootModelNode, modelId);
    }

    private static ModelNode ResolveModelNodeRecursive(ModelNode modelNode, Guid modelId)
    {
        if (modelNode == null)
        {
            return null;
        }

        if (modelNode.ModelId == modelId)
        {
            return modelNode;
        }

        foreach (ModelNode childModelNode in modelNode.ChildModels)
        {
            ModelNode found = ResolveModelNodeRecursive(childModelNode, modelId);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    #endregion
}
