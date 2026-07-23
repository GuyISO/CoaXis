using Godot;
using System;

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
    private readonly ModelBinder _modelBinder = new(); // このツリー専用のモデルバインダー

    private Color _selectedColor = new Color(231f / 255f, 177f / 255f, 246f / 255f);    
    private Color _defaultColor = new Color(1.0f, 1.0f, 1.0f); // デフォルトの背景色

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
        _modelBinder.Clear();
        UnsubscribeUiEvents();
        UnsubscribeApplicationEvents();

        base._ExitTree();
    }

    public override void _Process(double delta)
    {
        // ルートモデルがまだ取得できていない場合は、ModelEvent に通知をリクエストする、Ready団塊ではノードの読み込み順序の都合などで取得できないことを想定し、毎フレームチェックする
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
        DeselectAll(); // 選択状態は基本的にUI上に残さない
        
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
                break;
            case (int)HierarchyTreeColumn.VisibleButton:
                HandleVisibleButtonClicked(item);
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// モデルの選択状態が通知されたときのイベントハンドラ
    /// </summary>
    /// <param name="model">選択状態が変更されたモデル</param>
    /// <param name="isSelected">モデルが選択されている場合はtrue、選択されていない場合はfalse</param>
    private void OnModelSelectionStateNotified(AnyModel model, bool isSelected)
    {
        TreeItem item = _modelBinder.GetItem(model);
        if (item != null)
        {
            if (isSelected)
            {
                // TODO: 選択状態の色を設定する
                item.SetCustomBgColor((int)HierarchyTreeColumn.Name, _selectedColor);
                _lastSelectedItem = item;
            }
            else
            {
                item.ClearCustomBgColor((int)HierarchyTreeColumn.Name);
                if (_lastSelectedItem == item)
                {
                    _lastSelectedItem = null;
                }
            }
        }
    }

    /// <summary>
    /// 選択がクリアされたことを通知されたときのイベントハンドラ
    /// </summary>
    private void OnClearedNotified()
    {
        // 基本的にUI上に選択状態は残さないので選択状態でないはずだが、念のためすべての選択状態を解除する
        DeselectAll();
        _lastSelectedItem = null;
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
        TreeItem item = _modelBinder.GetItem(model);
        if (item != null)
        {
            item.SetIcon((int)HierarchyTreeColumn.VisibleButton, isVisible ? _visibleIcon : _invisibleIcon);
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
        SelectMode = SelectModeEnum.Single;

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
        AnyModel model = _modelBinder.GetModel(item);
        if (model == null)
        {
            Application.Log.Warn("HierarchyTree: selected item has no associated model.");
            return;
        }

        // 選択されたモデルを Pick した扱いとして通知する
        PickResult pickResult = PickUtility.PickByModel(model);
        Application.Pick.Event.NotifyResult(pickResult);
        _lastSelectedItem = item;
    }

    /// <summary>
    /// TreeItem の VisibleButton がクリックされたときの処理を行う
    /// </summary>
    /// <param name="item">クリックされた TreeItem</param>
    private void HandleVisibleButtonClicked(TreeItem item)
    {
        AnyModel model = _modelBinder.GetModel(item);
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
        TreeItem item = CreateItem(parentTreeItem);
        item.SetText((int)HierarchyTreeColumn.Name, model.Name);

        // 非表示切り替えのためのアイコンを設定
        item.SetCellMode((int)HierarchyTreeColumn.VisibleButton, TreeItem.TreeCellMode.Icon);
        item.SetIcon((int)HierarchyTreeColumn.VisibleButton, model.Visible ? _visibleIcon : _invisibleIcon);
        //item.SetEditable((int)HierarchyTreeColumn.VisibleButton, true); // アイコンをクリックして編集可能にする

        // AnyModel と TreeItem の対応を登録
        if (!_modelBinder.Bind(model, item))
        {
            Application.Log.Warn($"HierarchyTree: failed to bind model '{model.Name}' to tree item.");
        }

        // 子ノードを再帰的に追加
        foreach (AnyModel childModel in model.ChildModels)
        {
            // AnyModel のみをツリーに追加する
            AddToTree(childModel, item);
        }
    }

    /// <summary>
    /// AnyModel を TreeItem に追加する（親モデルを指定して追加する）
    /// </summary>
    /// <param name="childModel">追加する子モデル</param>
    /// <param name="parentModel">追加先の親モデル</param>
    private void AddToTree(AnyModel childModel, AnyModel parentModel)
    {
        TreeItem parentTreeItem = _modelBinder.GetItem(parentModel);
        if (parentTreeItem != null)
        {
            AddToTree(childModel, parentTreeItem);
        }
    }
}