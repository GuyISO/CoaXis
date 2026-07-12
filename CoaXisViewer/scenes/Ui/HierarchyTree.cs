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

    #endregion

    #region Lifecycle

    public override void _Ready()
    {
        // Tree選択イベントの購読開始
        MultiSelected += OnMultiSelected;
        CellSelected += OnCellSelected;

        // イベントの購読
        Application.Model.AddModelRequested += OnAddModelRequested;
        Application.Model.ModelSelectionStateNotified += OnModelSelectionStateNotified;
        Application.Model.ModelVisibilityStateNotified += OnModelVisibilityStateNotified;
        Application.Model.RootModelNotified += OnRootModelNotified;

        _visibleIcon = Application.Asset.GetVisibilityIcon(true, 24);
        _invisibleIcon = Application.Asset.GetVisibilityIcon(false, 24);

        // VisibleButton 列を固定幅にする
        SetColumnExpand((int)HierarchyTreeColumn.VisibleButton, false);
        SetColumnCustomMinimumWidth((int)HierarchyTreeColumn.VisibleButton, 24); // 24px など
    }

    public override void _ExitTree()
    {
        // Tree選択イベントの購読解除
        MultiSelected -= OnMultiSelected;
        CellSelected -= OnCellSelected;

        // イベントの購読解除
        Application.Model.AddModelRequested -= OnAddModelRequested;
        Application.Model.ModelSelectionStateNotified -= OnModelSelectionStateNotified;
        Application.Model.ModelVisibilityStateNotified -= OnModelVisibilityStateNotified;
        Application.Model.RootModelNotified -= OnRootModelNotified;
    }

    public override void _Process(double delta)
    {
        // ルートモデルがまだ取得できていない場合は、ModelEvent に通知をリクエストする、Ready団塊ではノードの読み込み順序の都合などで取得できないことを想定し、毎フレームチェックする
        if (_rootModel == null)
        {
            Application.Model.AskRootModel();
        }
    }

    #endregion

    #region Events

    /// <summary>
    /// TreeItem が選択・解除されたときのイベントハンドラ
    /// </summary>
    private void OnMultiSelected(TreeItem item, long column, bool selected)
    {
        if (column != (int)HierarchyTreeColumn.Name)
        {
            // 名前列以外の列が選択された場合はそのセルの選択状態を解除して終了する
            if (selected)
            {
                item.Deselect((int)column);
                return;
            }
        }

        if (selected)
        {
            // 選択されたアイテムに対応する AnyModel を選択状態にする
            Application.Selection.Add(ModelBinder.GetModel(item));
        }
        else
        {
            // 選択が解除されたアイテムに対応する AnyModel を選択解除状態にする
            Application.Selection.Remove(ModelBinder.GetModel(item));
        }
    }

    /// <summary>
    /// セルが選択されたときのイベントハンドラ、主にボタンのクリックを検知するために使用する
    /// </summary>
    private void OnCellSelected()
    {
        int column = GetSelectedColumn();
        TreeItem item = GetSelected();
        if (item == null)
        {
            return;
        }

        switch (column)
        {
            case (int)HierarchyTreeColumn.VisibleButton:
                OnVisibleButtonClicked(item);
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// VisibleButton がクリックされたときのイベントハンドラ
    /// </summary>
    /// <param name="item">クリックされた TreeItem</param>
    private void OnVisibleButtonClicked(TreeItem item)
    {
        AnyModel model = ModelBinder.GetModel(item);
        if (model == null)
        {
            Application.Log.Warn("HierarchyTree: clicked item has no associated model.");
            return;
        }

        // モデルの表示状態を切り替える
        Application.Model.ToggleModelVisibility(model);
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
    /// モデルの選択状態が通知されたときのイベントハンドラ
    /// </summary>
    /// <param name="model">選択状態が変更されたモデル</param>
    /// <param name="isSelected">モデルが選択されている場合はtrue、選択されていない場合はfalse</param>
    private void OnModelSelectionStateNotified(AnyModel model, bool isSelected)
    {
        TreeItem item = ModelBinder.GetItem(model);
        if (item != null)
        {
            if (isSelected)
            {
                item.Select((int)HierarchyTreeColumn.Name);
            }
            else
            {
                item.Deselect((int)HierarchyTreeColumn.Name);
            }
        }
    }

    /// <summary>
    /// モデルの表示状態が通知されたときのイベントハンドラ
    /// </summary>
    /// <param name="model">表示状態が変更されたモデル</param>
    /// <param name="isVisible">モデルが表示されている場合はtrue、非表示の場合はfalse</param>
    private void OnModelVisibilityStateNotified(AnyModel model, bool isVisible)
    {
        Application.Log.Debug($"HierarchyTree: visibility state notified. model='{model.Name}', isVisible={isVisible}");
        TreeItem item = ModelBinder.GetItem(model);
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

    /// <summary>
    /// AnyModel を TreeItem に追加する
    /// </summary>
    /// <param name="node">追加する AnyModel</param>
    /// <param name="parentTreeItem">親の TreeItem</param>
    private void AddToTree(AnyModel model, TreeItem parentTreeItem = null)
    {
        // ツリーにアイテムを追加、親が null の場合はルートアイテムとして追加される便利仕様
        TreeItem item = CreateItem(parentTreeItem);
        item.SetText((int)HierarchyTreeColumn.Name, model.Name);

        // 非表示切り替えのためのアイコンを設定
        item.SetCellMode((int)HierarchyTreeColumn.VisibleButton, TreeItem.TreeCellMode.Icon);
        item.SetIcon((int)HierarchyTreeColumn.VisibleButton, model.Visible ? _visibleIcon : _invisibleIcon);
        //item.SetEditable((int)HierarchyTreeColumn.VisibleButton, true); // アイコンをクリックして編集可能にする

        // AnyModel と TreeItem の対応を登録
        if (!ModelBinder.Bind(model, item))
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

    private void AddToTree(AnyModel childModel, AnyModel parentModel)
    {
        TreeItem parentTreeItem = ModelBinder.GetItem(parentModel);
        if (parentTreeItem != null)
        {
            AddToTree(childModel, parentTreeItem);
        }
    }

}