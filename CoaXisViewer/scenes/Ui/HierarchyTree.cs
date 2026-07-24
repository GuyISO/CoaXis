using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// 階層ツリーの表示と操作を行うUIコンポーネント
/// </summary>
public partial class HierarchyTree : Tree
{
    #region Fields

    // 関連ノードのキャッシュ
    private RootModel _rootModel = null; // 3Dモデル用ルートノードのキャッシュ
    private Texture2D _visibleIcon; // 表示アイコンのキャッシュ
    private Texture2D _invisibleIcon; // 非表示アイコンのキャッシュ
    private TreeItem _lastSelectedItem; // 最後に選択された TreeItem を保持
    private readonly ModelBinder _binder = new(); // このツリー専用のモデルバインダー

    private Color _selectedColor = new Color(231f / 255f, 177f / 255f, 246f / 255f);    
    private Color _defaultColor = new Color(1.0f, 1.0f, 1.0f); // デフォルトの背景色

    private bool _isInternalSelectionChange = false; // 内部的な選択状態の変更を通知するフラグ

    #endregion

    #region Lifecycle

    public override void _Ready()
    {
        EnsureState();
        SubscribeUiEvents();
        SubscribeApplicationEvents();

        _defaultColor = GetThemeColor("bg_color", "Tree"); // デフォルトの背景色をテーマから取得
    }

    public override void _ExitTree()
    {
        _binder.Clear();
        UnsubscribeUiEvents();
        UnsubscribeApplicationEvents();

        base._ExitTree();
    }

    public override void _Process(double delta)
    {
        // ルートモデルがまだ取得できていない場合は、ModelEvent に通知をリクエストする、Ready段階ではノードの読み込み順序の都合などで取得できないことを想定し、毎フレームチェックする
        if (_rootModel == null)
        {
            Application.Model.Event.AskRootModel();
        }
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
        Application.Selection.Event.ModelStateNotified += OnModelSelectionStateNotified;
        Application.Selection.Event.ClearedNotified += OnClearedNotified;
        Application.Model.Event.AddModelRequested += OnAddModelRequested;
        Application.Model.Event.ModelVisibilityStateNotified += OnModelVisibilityStateNotified;
        Application.Model.Event.RootModelNotified += OnRootModelNotified;
    }

    /// <summary>
    /// Applicationイベントの購読を解除する
    /// </summary>
    private void UnsubscribeApplicationEvents()
    {
        Application.Selection.Event.ModelStateNotified -= OnModelSelectionStateNotified;
        Application.Selection.Event.ClearedNotified -= OnClearedNotified;
        Application.Model.Event.AddModelRequested -= OnAddModelRequested;
        Application.Model.Event.ModelVisibilityStateNotified -= OnModelVisibilityStateNotified;
        Application.Model.Event.RootModelNotified -= OnRootModelNotified;
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
    /// <param name="model">選択状態が変更されたモデル</param>
    /// <param name="isSelected">モデルが選択されている場合はtrue、選択されていない場合はfalse</param>
    private void OnModelSelectionStateNotified(AnyModel model, bool isSelected)
    {
        TreeItem treeItem = _binder.GetTreeItem(model);
        if (treeItem != null)
        {
            if (isSelected)
            {
                // TODO: 選択状態の色を設定する
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
    /// <param name="child">追加する子モデル</param>
    /// <param name="parent">追加先の親モデル</param>
    private void OnAddModelRequested(AnyModel child, AnyModel parent)
    {
        AddToTree(child, parent);
    }

    /// <summary>
    /// モデルの表示状態が通知されたときのイベントハンドラ
    /// </summary>
    /// <param name="model">表示状態が変更されたモデル</param>
    /// <param name="isVisible">モデルが表示されている場合はtrue、非表示の場合はfalse</param>
    private void OnModelVisibilityStateNotified(AnyModel model, bool isVisible)
    {
        Application.Log.Debug($"HierarchyTree: visibility state notified. model='{model.Name}', isVisible={isVisible}");
        TreeItem treeItem = _binder.GetTreeItem(model);
        if (treeItem != null)
        {
            treeItem.SetIcon((int)HierarchyTreeColumn.VisibleButton, isVisible ? _visibleIcon : _invisibleIcon);
        }
    }

    /// <summary>
    /// ルートモデルが通知されたときのイベントハンドラ
    /// </summary>
    /// <param name="rootModel">通知されたルートモデル</param>
    private void OnRootModelNotified(RootModel rootModel)
    {
        if (_rootModel == null)
        {
            _rootModel = rootModel;
            AddToTree(_rootModel);
            Application.Log.Info("HierarchyTree: RootModel notified and added to tree.");
        }
    }

    #endregion

    #region Internal Helpers

    /// <summary>
    /// ツリー表示に必要な初期状態を整える
    /// </summary>
    private void EnsureState()
    {
        _visibleIcon = Application.Asset.Service.GetVisibilityIcon(true, 24);
        _invisibleIcon = Application.Asset.Service.GetVisibilityIcon(false, 24);

        // VisibleButton 列を固定幅にする
        SetColumnExpand((int)HierarchyTreeColumn.VisibleButton, false);
        SetColumnCustomMinimumWidth((int)HierarchyTreeColumn.VisibleButton, 24); // 24px など
    }

    /// <summary>
    /// TreeItem が選択されたときの処理を行う
    /// </summary>
    /// <param name="item">選択された TreeItem</param>
    private void HandleSelected(TreeItem item)
    {
        AnyModel model = _binder.GetModel(item);
        if (model == null)
        {
            Application.Log.Warn("HierarchyTree: selected item has no associated model.");
            return;
        }

        SelectionMode mode = Application.Selection.Service.Mode;
        bool shouldHandleAsRange = ShouldHandleAsRangeSelection(mode);

        if (!shouldHandleAsRange)
        {
            Application.Pick.Event.NotifyResult(PickUtility.PickByModel(model));
        }
        else
        {
            // Shift押下時またはAdd/Removeモードでは範囲選択として扱い、複数モデルの選択を通知する
            AnyModel[] models = GetAllModelsInRange(_lastSelectedItem, item);
            Application.Pick.Event.NotifyResults(PickUtility.PickByModels(models));
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
    /// <returns>選択対象となるモデル配列</returns>
    private AnyModel[] GetAllModelsInRange(TreeItem lastItem, TreeItem selectedItem)
    {
        if (lastItem == null || selectedItem == null)
        {
            return Array.Empty<AnyModel>();
        }

        List<AnyModel> modelsInRange = CollectModelsInForwardOrder(lastItem, selectedItem);
        if (modelsInRange == null)
        {
            // 逆方向の範囲選択（下から上）にも対応する
            modelsInRange = CollectModelsInForwardOrder(selectedItem, lastItem);
        }

        return modelsInRange?.ToArray() ?? Array.Empty<AnyModel>();
    }

    /// <summary>
    /// startItem から endItem までを前方向にたどり、範囲内モデルを収集する
    /// </summary>
    /// <param name="startItem">走査開始アイテム</param>
    /// <param name="endItem">走査終了アイテム</param>
    /// <returns>到達できた場合はモデル一覧、到達できない場合は null</returns>
    private List<AnyModel> CollectModelsInForwardOrder(TreeItem startItem, TreeItem endItem)
    {
        List<AnyModel> modelsInRange = new List<AnyModel>();

        TreeItem currentItem = startItem;
        while (currentItem != null)
        {
            AnyModel model = _binder.GetModel(currentItem);
            if (model != null)
            {
                modelsInRange.Add(model);
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

        return modelsInRange;
    }

    /// <summary>
    /// TreeItem の VisibleButton がクリックされたときの処理を行う
    /// </summary>
    /// <param name="item">クリックされた TreeItem</param>
    private void HandleVisibleButtonClicked(TreeItem item)
    {
        AnyModel model = _binder.GetModel(item);
        if (model == null)
        {
            Application.Log.Warn("HierarchyTree: clicked item has no associated model.");
            return;
        }

        // モデルの表示状態を切り替える
        Application.Model.Event.ToggleModelVisibility(model);
    }

    #endregion

    /// <summary>
    /// AnyModel を TreeItem に追加する
    /// </summary>
    /// <param name="node">追加する AnyModel</param>
    /// <param name="parentTreeItem">親の TreeItem</param>
    private void AddToTree(AnyModel model, TreeItem parentTreeItem = null)
    {
        // ツリーにアイテムを追加、親が null の場合は初回のみルートアイテムとして追加される便利仕様
        TreeItem treeItem = CreateItem(parentTreeItem);
        treeItem.SetText((int)HierarchyTreeColumn.Name, model.Name);

        // 非表示切り替えのためのアイコンを設定
        treeItem.SetCellMode((int)HierarchyTreeColumn.VisibleButton, TreeItem.TreeCellMode.Icon);
        treeItem.SetIcon((int)HierarchyTreeColumn.VisibleButton, model.Visible ? _visibleIcon : _invisibleIcon);
        //treeItem.SetEditable((int)HierarchyTreeColumn.VisibleButton, true); // アイコンをクリックして編集可能にする

        // AnyModel と TreeItem の対応を登録
        if (!_binder.Bind(model, treeItem))
        {
            Application.Log.Warn($"HierarchyTree: failed to bind model '{model.Name}' to tree item.");
        }

        // 子ノードを再帰的に追加
        foreach (AnyModel childModel in model.ChildModels)
        {
            // AnyModel のみをツリーに追加する
            AddToTree(childModel, treeItem);
        }
    }

    /// <summary>
    /// AnyModel を TreeItem に追加する（親モデルを指定して追加する）
    /// </summary>
    /// <param name="childModel">追加する子モデル</param>
    /// <param name="parentModel">追加先の親モデル</param>
    private void AddToTree(AnyModel childModel, AnyModel parentModel)
    {
        TreeItem parentTreeItem = _binder.GetTreeItem(parentModel);
        if (parentTreeItem != null)
        {
            AddToTree(childModel, parentTreeItem);
        }
    }
}