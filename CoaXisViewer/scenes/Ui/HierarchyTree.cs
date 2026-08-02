using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// 階層ツリーの表示と操作を行うUIコンポーネント
/// </summary>
public partial class HierarchyTree : Tree
{
    #region Fields

    private Dictionary<Guid, TreeItem> _modelIdToTreeItem = new(); // ModelId -> TreeItem の対応辞書、TreeItem -> ModelId はMetaDataで保持する

    // 関連ノードのキャッシュ
    private Texture2D _visibleIcon; // 表示アイコンのキャッシュ
    private Texture2D _invisibleIcon; // 非表示アイコンのキャッシュ
    private TreeItem _lastSelectedItem; // 最後に選択された TreeItem を保持
    private readonly ModelTreeBinder _binder = new(); // このツリー専用のモデルバインダー

    private ModelData _rootModelData; // このツリーのルートモデルのキャッシュ、シーン全体のルートではないことに注意

    private Color _selectedColor;

    private bool _isInternalSelectionChange = false; // 内部的な選択状態の変更を通知するフラグ

    #endregion

    #region Lifecycle

    public override void _Ready()
    {
        EnsureState();
        SubscribeUiEvents();
        SubscribeApplicationEvents();
        ApplySettings();
    }

    public override void _ExitTree()
    {
        _binder.Clear();
        UnsubscribeUiEvents();
        UnsubscribeApplicationEvents();

        base._ExitTree();
    }

    #endregion

    #region Events

    /// <summary>
    /// UIイベントの購読を開始する
    /// </summary>
    private void SubscribeUiEvents()
    {
        CellSelected += OnCellSelected;
    }

    /// <summary>
    /// UIイベントの購読を解除する
    /// </summary>
    private void UnsubscribeUiEvents()
    {
        CellSelected -= OnCellSelected;
    }
    
    /// <summary>
    /// Applicationイベントの購読を開始する
    /// </summary>
    private void SubscribeApplicationEvents()
    {
        Application.Setting.Event.SettingsNotified += ApplySettings;
        Application.Selection.Event.ModelStateNotified += OnModelSelectionStateNotified;
        Application.Selection.Event.ClearedNotified += OnClearedNotified;
        Application.Model.Event.AddModelRequested += OnAddModelRequested;
        Application.Model.Event.ModelVisibilityStateNotified += OnModelVisibilityStateNotified;
    }

    /// <summary>
    /// Applicationイベントの購読を解除する
    /// </summary>
    private void UnsubscribeApplicationEvents()
    {
        Application.Setting.Event.SettingsNotified -= ApplySettings;
        Application.Selection.Event.ModelStateNotified -= OnModelSelectionStateNotified;
        Application.Selection.Event.ClearedNotified -= OnClearedNotified;
        Application.Model.Event.AddModelRequested -= OnAddModelRequested;
        Application.Model.Event.ModelVisibilityStateNotified -= OnModelVisibilityStateNotified;
    }

    /// <summary>
    /// セルが選択されたときのイベントハンドラ、主にボタンのクリックを検知するために使用する
    /// </summary>
    private void OnCellSelected()
    {
        if (_isInternalSelectionChange)
        {
            // 内部的な選択状態の変更による通知は無視する
            return;
        }

        TreeItem item = GetSelected();
        if (item == null)
        {
            return;
        }

        int column = GetSelectedColumn();

        switch (column)
        {
            case (int)HierarchyTreeColumn.Name:
                HandleSelected(item);        
                _lastSelectedItem = item;
                break;
            case (int)HierarchyTreeColumn.VisibleButton:
                HandleVisibleButtonClicked(item);
                break;
            default:
                break;
        }

        if (_lastSelectedItem != null)
        {
            _isInternalSelectionChange = true; // 内部的な選択状態の変更を通知するフラグを立てる
            _lastSelectedItem.Select((int)HierarchyTreeColumn.Name); // 最後に選択されたアイテムを保持する
            _isInternalSelectionChange = false; // フラグをリセットする
        }
    }

    /// <summary>
    /// モデルの選択状態が通知されたときのイベントハンドラ
    /// </summary>
    /// <param name="modelId">選択状態が変更されたモデル識別子</param>
    /// <param name="isSelected">モデルが選択されている場合はtrue、選択されていない場合はfalse</param>
    private void OnModelSelectionStateNotified(string modelId, bool isSelected)
    {
        if (!Guid.TryParse(modelId, out Guid parsedModelId) || parsedModelId == Guid.Empty)
        {
            return;
        }

        TreeItem treeItem = _binder.GetTreeItem(parsedModelId);
        if (treeItem != null)
        {
            if (isSelected)
            {
                treeItem.SetCustomBgColor((int)HierarchyTreeColumn.Name, _selectedColor);
            }
            else
            {
                treeItem.ClearCustomBgColor((int)HierarchyTreeColumn.Name);
            }
        }
    }

    /// <summary>
    /// 選択がクリアされたことを通知されたときのイベントハンドラ
    /// </summary>
    private void OnClearedNotified()
    {
        // 基本的にUI上に選択状態は残さないので選択状態でないはずだが、念のためすべての選択状態を解除する
        _isInternalSelectionChange = true; // 内部的な選択状態の変更を通知するフラグを立てる
        DeselectAll();
        _lastSelectedItem = null;
        _isInternalSelectionChange = false; // フラグをリセットする
    }

    /// <summary>
    /// モデルの追加がリクエストされたときのイベントハンドラ
    /// </summary>
    /// <param name="childModelId">追加する子モデルID</param>
    /// <param name="parentModelId">追加先の親モデルID</param>
    private void OnAddModelRequested(string childModelId, string parentModelId)
    {
        if (!Guid.TryParse(childModelId, out Guid parsedChildModelId) || parsedChildModelId == Guid.Empty)
        {
            Application.Log.Warn($"HierarchyTree: failed to add model. invalid child modelId='{childModelId}'");
            return;
        }

        Guid parsedParentModelId = Guid.Empty;
        if (!string.IsNullOrWhiteSpace(parentModelId))
        {
            Guid.TryParse(parentModelId, out parsedParentModelId);
        }

        ModelNode childModelNode = FindModelNodeById(parsedChildModelId);
        if (childModelNode == null)
        {
            Application.Log.Warn($"HierarchyTree: failed to add model. child model not found. modelId='{parsedChildModelId}'");
            return;
        }

        if (parsedParentModelId == Guid.Empty)
        {
            AddToTree(childModelNode);
            return;
        }

        ModelNode parentModelNode = FindModelNodeById(parsedParentModelId);
        AddToTree(childModelNode, parentModelNode);
    }

    /// <summary>
    /// モデルの表示状態が通知されたときのイベントハンドラ
    /// </summary>
    /// <param name="modelId">表示状態が変更されたモデル識別子</param>
    /// <param name="isVisible">モデルが表示されている場合はtrue、非表示の場合はfalse</param>
    private void OnModelVisibilityStateNotified(string modelId, bool isVisible)
    {
        if (!Guid.TryParse(modelId, out Guid parsedModelId) || parsedModelId == Guid.Empty)
        {
            return;
        }

        Application.Log.Debug($"HierarchyTree: visibility state notified. modelId='{parsedModelId}', isVisible={isVisible}");
        TreeItem treeItem = _binder.GetTreeItem(parsedModelId);
        if (treeItem != null)
        {
            treeItem.SetIcon((int)HierarchyTreeColumn.VisibleButton, isVisible ? _visibleIcon : _invisibleIcon);
        }
    }

    #endregion

    #region public Methods

    internal void SetRootModelData(ModelData rootModelData)
    {
        if (_rootModelData != null)
        {
            return;
        }

        _rootModelData = rootModelData;
    }

    #endregion

    #region Internal Helpers

    /// <summary>
    /// 設定値を反映する
    /// </summary>
    private void ApplySettings()
    {
        _selectedColor = Color.FromHtml(Application.Setting.Service.Current.Color.HierarchySelectedColor);
        ReapplySelectedRowColors();
    }

    /// <summary>
    /// 現在選択中モデルに対して背景色を再適用する
    /// </summary>
    private void ReapplySelectedRowColors()
    {
        IReadOnlyCollection<Guid> selectedModelIds = Application.Selection.Service.ModelIds;
        if (selectedModelIds == null || selectedModelIds.Count == 0)
        {
            return;
        }

        foreach (Guid modelId in selectedModelIds)
        {
            if (modelId == Guid.Empty)
            {
                continue;
            }

            TreeItem item = _binder.GetTreeItem(modelId);
            if (item == null)
            {
                continue;
            }

            item.SetCustomBgColor((int)HierarchyTreeColumn.Name, _selectedColor);
        }
    }

    /// <summary>
    /// ツリー表示に必要な初期状態を整える
    /// </summary>
    private void EnsureState()
    {
        _visibleIcon = Application.Asset.Service.GetVisibilityIcon(true, Constant.Ui.Tree.HierarchyVisibleIconSize);
        _invisibleIcon = Application.Asset.Service.GetVisibilityIcon(false, Constant.Ui.Tree.HierarchyVisibleIconSize);

        // アイコンサイズに合わせて VisibleButton 列を固定幅にする
        SetColumnExpand((int)HierarchyTreeColumn.VisibleButton, false);
        SetColumnCustomMinimumWidth((int)HierarchyTreeColumn.VisibleButton, Constant.Ui.Tree.HierarchyVisibleIconSize);
    }

    /// <summary>
    /// TreeItem が選択されたときの処理を行う
    /// </summary>
    /// <param name="item">選択された TreeItem</param>
    private void HandleSelected(TreeItem item)
    {
        Guid modelId = _binder.GetModelId(item);
        if (modelId == Guid.Empty)
        {
            Application.Log.Warn("HierarchyTree: selected item has no associated model.");
            return;
        }

        SelectionMode mode = Application.Selection.Service.Mode;
        bool shouldHandleAsRange = ShouldHandleAsRangeSelection(mode);

        if (!shouldHandleAsRange)
        {
            Application.Pick.Event.NotifyResult(PickUtility.PickByModelId(modelId));
        }
        else
        {
            // Add/Removeモードでは範囲選択として扱い、複数モデルの選択を通知する
            Guid[] modelIds = GetAllModelsInRange(_lastSelectedItem, item);
            Application.Pick.Event.NotifyResults(PickUtility.PickByModelIds(modelIds));
        }
    }

    /// <summary>
    /// 範囲選択として扱うべきかどうかを判定する
    /// </summary>
    /// <param name="mode">現在の選択モード</param>
    /// <returns>範囲選択として扱う場合は true</returns>
    private bool ShouldHandleAsRangeSelection(SelectionMode mode)
    {
        // AddモードやRemoveモードかつすでに何か選択中のアイテムがある場合は範囲選択として扱う
        if (mode != SelectionMode.Add && mode != SelectionMode.Remove)
        {
            return false;
        }

        if (_lastSelectedItem == null)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// lastItem から selectedItem までのすべてのアイテムを選択対象モデルとして取得する
    /// </summary>
    /// <param name="lastItem">範囲選択の起点</param>
    /// <param name="selectedItem">範囲選択の終点</param>
    /// <returns>選択対象となるモデルID配列</returns>
    private Guid[] GetAllModelsInRange(TreeItem lastItem, TreeItem selectedItem)
    {
        if (lastItem == null || selectedItem == null)
        {
            return Array.Empty<Guid>();
        }

        List<Guid> modelIdsInRange = CollectModelsInForwardOrder(lastItem, selectedItem);
        if (modelIdsInRange == null)
        {
            // 逆方向の範囲選択（下から上）にも対応する
            modelIdsInRange = CollectModelsInForwardOrder(selectedItem, lastItem);
        }

        return modelIdsInRange?.ToArray() ?? Array.Empty<Guid>();
    }

    /// <summary>
    /// startItem から endItem までを前方向にたどり、範囲内モデルを収集する
    /// </summary>
    /// <param name="startItem">走査開始アイテム</param>
    /// <param name="endItem">走査終了アイテム</param>
    /// <returns>到達できた場合はモデルID一覧、到達できない場合は null</returns>
    private List<Guid> CollectModelsInForwardOrder(TreeItem startItem, TreeItem endItem)
    {
        List<Guid> modelIdsInRange = new();

        TreeItem currentItem = startItem;
        while (currentItem != null)
        {
            Guid modelId = _binder.GetModelId(currentItem);
            if (modelId != Guid.Empty)
            {
                modelIdsInRange.Add(modelId);
            }

            if (currentItem == endItem)
            {
                break;
            }

            currentItem = currentItem.GetNext();
        }

        if (currentItem == null)
        {
            return null;
        }

        return modelIdsInRange;
    }

    /// <summary>
    /// TreeItem の VisibleButton がクリックされたときの処理を行う
    /// </summary>
    /// <param name="item">クリックされた TreeItem</param>
    private void HandleVisibleButtonClicked(TreeItem item)
    {
        Guid modelId = _binder.GetModelId(item);
        if (modelId == Guid.Empty)
        {
            Application.Log.Warn("HierarchyTree: clicked item has no associated model.");
            return;
        }

        // モデルの表示状態を切り替える
        Application.Model.Event.ToggleModelVisibility(modelId);
    }

    /// <summary>
    /// ModelNode を TreeItem に追加する
    /// </summary>
    /// <param name="node">追加する ModelNode</param>
    /// <param name="parentTreeItem">親の TreeItem</param>
    private void AddToTree(ModelNode modelNode, TreeItem parentTreeItem = null)
    {
        // ツリーにアイテムを追加、親が null の場合は初回のみルートアイテムとして追加される便利仕様
        TreeItem treeItem = CreateItem(parentTreeItem);
        treeItem.SetText((int)HierarchyTreeColumn.Name, modelNode.Name);

        // 非表示切り替えのためのアイコンを設定
        treeItem.SetCellMode((int)HierarchyTreeColumn.VisibleButton, TreeItem.TreeCellMode.Icon);
        treeItem.SetIcon((int)HierarchyTreeColumn.VisibleButton, modelNode.Visible ? _visibleIcon : _invisibleIcon);
        //treeItem.SetEditable((int)HierarchyTreeColumn.VisibleButton, true); // アイコンをクリックして編集可能にする

        // ModelNode と TreeItem の対応を登録
        if (!_binder.Bind(modelNode, treeItem))
        {
            Application.Log.Warn($"HierarchyTree: failed to bind model '{modelNode.Name}' to tree item.");
        }

        // 子ノードを再帰的に追加
        foreach (ModelNode childModelNode in modelNode.ChildModels)
        {
            // ModelNode のみをツリーに追加する
            AddToTree(childModelNode, treeItem);
        }
    }

    /// <summary>
    /// ModelNode を TreeItem に追加する（親モデルを指定して追加する）
    /// </summary>
    /// <param name="childModelNode">追加する子モデル</param>
    /// <param name="parentModelNode">追加先の親モデル</param>
    private void AddToTree(ModelNode childModelNode, ModelNode parentModelNode)
    {
        TreeItem parentTreeItem = _binder.GetTreeItem(parentModelNode);
        if (parentTreeItem != null)
        {
            AddToTree(childModelNode, parentTreeItem);
        }
    }

    private ModelNode FindModelNodeById(Guid modelId)
    {
        if (modelId == Guid.Empty )
        {
            return null;
        }

        return FindModelNodeByIdRecursive(Application.Model.Service.Root.Node, modelId);
    }

    private static ModelNode FindModelNodeByIdRecursive(ModelNode modelNode, Guid modelId)
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
            ModelNode found = FindModelNodeByIdRecursive(childModelNode, modelId);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
    
    #endregion
}