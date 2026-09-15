using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// モデルの階層ツリー表示と操作を行うUIコンポーネント
/// </summary>
public partial class ModelEntityTree : Tree
{
    #region Fields

    private Dictionary<Guid, TreeItem> _modelIdToTreeItem = new(); // ModelId -> TreeItem の対応辞書、TreeItem -> ModelId は各TreeItemにMetaDataで設定する

    private TreeItem _lastSelectedItem; // 最後に選択された TreeItem を保持

    private ModelEntity _rootModelEntity; // このツリーのルートモデル実体のキャッシュ、シーン全体のルートではないことに注意

    private Color _selectedColor;

    private ModelTreeMenu _menu;

    private const int VisibilityButtonId = 1;
    private const int FitButtonId = 2;
    private const int SpinAppealButtonId = 3;

    #endregion

    #region Lifecycle

    public override void _Ready()
    {
        _menu = GetNode<ModelTreeMenu>("PopupMenu");

        SubscribeUiEvents();
        SubscribeApplicationEvents();
        ApplySettings();
    }

    public override void _ExitTree()
    {
        UnsubscribeUiEvents();
        UnsubscribeApplicationEvents();

        base._ExitTree();
    }

    public override void _GuiInput(InputEvent @event)
    {
        // 右クリックによるコンテキストメニュー表示の処理
        if (@event is InputEventMouseButton mb &&
            mb.ButtonIndex == MouseButton.Right &&
            mb.Pressed)
        {
            // Popup(Window)のPositionはOS画面座標系のため、ビューポート内座標のGetGlobalMousePositionではなくDisplayServerの実マウス座標を使う
            Vector2I mousePosition = DisplayServer.MouseGetPosition();
            TreeItem targetItem = GetItemAtPosition(GetLocalMousePosition());
            _menu.ShowForItem(targetItem, mousePosition);
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
        ButtonClicked += OnButtonClicked;
        ItemActivated += OnItemActivated;
    }

    /// <summary>
    /// UIイベントの購読を解除する
    /// </summary>
    private void UnsubscribeUiEvents()
    {
        CellSelected -= OnCellSelected;
        ButtonClicked -= OnButtonClicked;
        ItemActivated -= OnItemActivated;
    }
    
    /// <summary>
    /// Applicationイベントの購読を開始する
    /// </summary>
    private void SubscribeApplicationEvents()
    {
        Application.Setting.Event.SettingsNotified += ApplySettings;
        Application.Selection.Event.ModelStateNotified += OnModelSelectionStateNotified;
        Application.Selection.Event.ClearedNotified += OnClearedNotified;
        Application.Model.Event.ModelAdded += OnModelAddedNotified;
        Application.Model.Event.ModelVisibilityStateNotified += OnModelVisibilityStateNotified;
        Application.Model.Event.ModelStatusNotified += OnModelStatusNotified;
        Application.Model.Event.RegistryCleared += OnRegistryClearedNotified;
    }

    /// <summary>
    /// Applicationイベントの購読を解除する
    /// </summary>
    private void UnsubscribeApplicationEvents()
    {
        Application.Setting.Event.SettingsNotified -= ApplySettings;
        Application.Selection.Event.ModelStateNotified -= OnModelSelectionStateNotified;
        Application.Selection.Event.ClearedNotified -= OnClearedNotified;
        Application.Model.Event.ModelAdded -= OnModelAddedNotified;
        Application.Model.Event.ModelVisibilityStateNotified -= OnModelVisibilityStateNotified;
        Application.Model.Event.ModelStatusNotified -= OnModelStatusNotified;
        Application.Model.Event.RegistryCleared -= OnRegistryClearedNotified;
    }

    /// <summary>
    /// セルが選択されたときのイベントハンドラ、主にボタンのクリックを検知するために使用する
    /// </summary>
    private void OnCellSelected()
    {
        TreeItem item = GetSelected();
        if (item == null)
        {
            return;
        }

        HandleSelected(item);        
        _lastSelectedItem = item;
    }

    /// <summary>
    /// TreeItem のボタンが押されたときのイベントハンドラ
    /// </summary>
    /// <param name="item">ボタンが押された TreeItem</param>
    /// <param name="column">ボタンがある列</param>
    /// <param name="buttonId">ボタンの識別 ID</param>
    /// <param name="mouseButtonIndex">マウスボタンの識別子</param>
    private void OnButtonClicked(TreeItem item, long column, long buttonId, long mouseButtonIndex)
    {
        if (item == null || column != 0)
        {
            return;
        }

        switch (buttonId)
        {
            case VisibilityButtonId:
                HandleVisibleButtonClicked(item);
                break;
            case FitButtonId:
                HandleFitButtonClicked(item);
                break;
            case SpinAppealButtonId:
                HandleSpinAppealButtonClicked(item);
                break;
        }
    }

    /// <summary>
    /// TreeItem がアクティブ化されたときのイベントハンドラ、主にダブルクリックやEnterキー押下時に呼び出される
    /// </summary>
    private void OnItemActivated()
    {
        TreeItem item = GetSelected();
        if (item == null)
        {
            return;
        }

        // 現在はターゲットボタンと同じフィット処理を行い、将来はここで専用の操作へ拡張できるようにする。
        HandleFitButtonClicked(item);
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

        TreeItem treeItem = _modelIdToTreeItem.TryGetValue(parsedModelId, out TreeItem item) ? item : null;
        if (treeItem == null)
        {
            return;
        }

        if (isSelected)
        {
            treeItem.SetCustomBgColor(0, _selectedColor);
        }
        else
        {
            treeItem.ClearCustomBgColor(0);
        }
    }

    /// <summary>
    /// 選択がクリアされたことを通知されたときのイベントハンドラ
    /// </summary>
    private void OnClearedNotified()
    {
        _lastSelectedItem = null;
    }

    /// <summary>
    /// モデルの追加がリクエストされたときのイベントハンドラ
    /// </summary>
    /// <param name="modelId">追加する子モデルID</param>
    /// <param name="parentModelId">追加先の親モデルID</param>
    private void OnModelAddedNotified(string modelId, string parentModelId)
    {
        if (!Guid.TryParse(modelId, out Guid parsedModelId) || parsedModelId == Guid.Empty)
        {
            Application.Log.Warn($"ModelTree: failed to add model. invalid child modelId='{modelId}'");
            return;
        }

        Guid parsedParentModelId = Guid.Empty;
        if (!string.IsNullOrWhiteSpace(parentModelId))
        {
            Guid.TryParse(parentModelId, out parsedParentModelId);
        }

        if (parsedParentModelId == Guid.Empty)
        {
            AddToTree(parsedModelId, Guid.Empty);
            return;
        }
        
        AddToTree(parsedModelId, parsedParentModelId);
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

        ModelEntity modelEntity = Application.Model.Registry.GetEntity(parsedModelId);
        if (modelEntity == null)
        {
            return;
        }

        Application.Log.Debug($"ModelTree: visibility state notified. modelId='{parsedModelId}', visibility={modelEntity.Visibility}");
        TreeItem treeItem = _modelIdToTreeItem.TryGetValue(parsedModelId, out TreeItem item) ? item : null;
        if (treeItem != null)
        {
            Texture2D buttonIcon = Application.Asset.Service.GetVisibilityIcon(
                modelEntity.Visibility,
                isVisible,
                Constant.Ui.Tree.HierarchyVisibleIconSize)
                ?? Application.Asset.Service.GetVisibilityIcon(
                    ModelVisibility.Visible,
                    Constant.Ui.Tree.HierarchyVisibleIconSize);
            treeItem.SetButton(0, 0, buttonIcon);
        }
    }

    /// <summary>
    /// モデルのステータスが更新されたときのイベントハンドラ
    /// </summary>
    /// <param name="modelId">ステータス更新対象のモデル識別子</param>
    /// <param name="status">更新後のステータス</param>
    private void OnModelStatusNotified(string modelId, int status)
    {
        if (!Guid.TryParse(modelId, out Guid parsedModelId) || parsedModelId == Guid.Empty)
        {
            return;
        }

        TreeItem treeItem = _modelIdToTreeItem.TryGetValue(parsedModelId, out TreeItem item) ? item : null;
        if (treeItem == null)
        {
            return;
        }

        treeItem.SetCustomColor(0, ResolveTextColor((ModelStatus)status));
    }

    /// <summary>
    /// モデルレジストリがクリアされたことを通知されたときのイベントハンドラ
    /// </summary>
    private void OnRegistryClearedNotified()
    {
        // レジストリがクリアされたあとにツリーだけ残ると、
        // 古い TreeItem を参照したまま UI が壊れるので、先にツリーを空にして Root から再構築する。
        Clear();
        _modelIdToTreeItem.Clear();
        _lastSelectedItem = null;

        _rootModelEntity = Application.Model.Service.Root;
        if (_rootModelEntity == null)
        {
            return;
        }

        if (Application.Model.Registry.GetEntity(_rootModelEntity.Id) == null)
        {
            return;
        }

        AddToTree(_rootModelEntity.Id, Guid.Empty);
    }

    #endregion

    #region public Methods

    // TODO: ちゃんとやる
    internal void SetRootModelEntity(ModelEntity rootModelEntity)
    {
        if (_rootModelEntity != null)
        {
            return;
        }

        _rootModelEntity = rootModelEntity;

        AddToTree(_rootModelEntity.Id, Guid.Empty);
    }
    
    #endregion

    #region Internal Helpers

    /// <summary>
    /// 指定したモデルをツリーに追加する
    /// </summary>
    /// <param name="modelId">追加する子モデル</param>
    /// <param name="parentModelId">親モデル</param>
    private void AddToTree(Guid modelId, Guid parentModelId)
    {
        // Tree への再構築時に、レジストリから既に消えているモデルや root 直前の空 ID を拾わないように防ぐ。
        // これがないと、Clear 中や既存モデルが破棄済みのタイミングで NullReference になりやすい。
        if (modelId == Guid.Empty)
        {
            return;
        }

        // 親追加時の子孫再帰と個別のModelAdded通知が重なるため、同じモデルのTreeItemは一度だけ作る。
        if (_modelIdToTreeItem.ContainsKey(modelId))
        {
            return;
        }

        TreeItem parentTreeItem = _modelIdToTreeItem.TryGetValue(parentModelId, out TreeItem item) ? item : null;
        ModelEntity modelEntity = Application.Model.Registry.GetEntity(modelId);
        if (modelEntity == null)
        {
            return;
        }

        // ツリーにアイテムを追加、親が null の場合は初回のみルートアイテムとして追加される便利仕様
        TreeItem treeItem = CreateItem(parentTreeItem);

        // --- テキスト（名前） ---
        treeItem.SetText(0, modelEntity.Name);

        // --- 左側アイコン（ModelEntity に紐づくモデルアイコン。不在時は既定アイコンで代替） ---
        Texture2D icon = Application.Asset.Service.GetIcon(modelEntity.IconPath, Constant.Ui.Tree.HierarchyVisibleIconSize)
            ?? Application.Asset.Service.GetDefaultIcon(Constant.Ui.Tree.HierarchyVisibleIconSize);
        treeItem.SetIcon(0, icon);

        // --- 右側ボタン（表示切替・演出用） ---
        Texture2D btnIcon = Application.Asset.Service.GetVisibilityIcon(
            modelEntity.Visibility,
            ModelVisibilityResolver.IsVisible(modelEntity),
            Constant.Ui.Tree.HierarchyVisibleIconSize)
            ?? Application.Asset.Service.GetVisibilityIcon(
                ModelVisibility.Visible,
                Constant.Ui.Tree.HierarchyVisibleIconSize);
        Texture2D fitIcon = Application.Asset.Service.GetIcon(
            "res://assets/icon/mono/target.svg",
            Constant.Ui.Tree.HierarchyVisibleIconSize);
        Texture2D spinAppealIcon = Application.Asset.Service.GetIcon(
            "res://assets/icon/oop/360.svg",
            Constant.Ui.Tree.HierarchyVisibleIconSize);
        treeItem.AddButton(0, btnIcon, id: VisibilityButtonId);
        treeItem.AddButton(0, fitIcon, id: FitButtonId);
        treeItem.AddButton(0, spinAppealIcon, id: SpinAppealButtonId);

        // EntityId と TreeItem の対応を登録
        treeItem.SetMeta("EntityId", modelId.ToString());
        treeItem.SetCustomColor(0, ResolveTextColor(modelEntity.Status));
         treeItem.Collapsed = modelEntity.IsCollapsed;
        _modelIdToTreeItem.Add(modelId, treeItem);

        // 子ノードを再帰的に追加
        foreach (ModelEntity childModelEntity in modelEntity.Children)
        {
            if (childModelEntity == null)
            {
                continue;
            }

            // ModelNode のみをツリーに追加する
            AddToTree(childModelEntity.Id, modelId);
        }
    }

    /// <summary>
    /// 設定値を反映する
    /// </summary>
    private void ApplySettings()
    {
        _selectedColor = Color.FromHtml(Application.Setting.Service.Current.Color.HierarchySelectedColor);
        ReapplySelectedRowColors();
    }

    private static Color ResolveTextColor(ModelStatus status)
    {
        return status switch
        {
            ModelStatus.Loading => new Color(0.5f, 0.5f, 0.5f),
            ModelStatus.Loaded => Colors.White,
            ModelStatus.LoadFailed => new Color(1.0f, 0.5f, 0.5f),
            ModelStatus.Initialized => new Color(0.0f, 0.0f, 0.0f),
            ModelStatus.Registered => new Color(0.25f, 0.25f, 0.25f),
            _ => new Color(0.0f, 0.0f, 0.0f),
        };
    }

    /// <summary>
    /// 現在選択中モデル実体に対して背景色を再適用する
    /// </summary>
    private void ReapplySelectedRowColors()
    {
        IReadOnlyCollection<Guid> selectedEntityIds = Application.Selection.Service.EntityIds;
        if (selectedEntityIds == null || selectedEntityIds.Count == 0)
        {
            return;
        }

        foreach (Guid entityId in selectedEntityIds)
        {
            if (entityId == Guid.Empty)
            {
                continue;
            }

            TreeItem item = _modelIdToTreeItem.TryGetValue(entityId, out TreeItem treeItem) ? treeItem : null;
            if (item == null)
            {
                continue;
            }

            item.SetCustomBgColor(0, _selectedColor);
        }
    }

    /// <summary>
    /// TreeItem が選択されたときの処理を行う
    /// </summary>
    /// <param name="item">選択された TreeItem</param>
    private void HandleSelected(TreeItem item)
    {
        Guid entityId = TryGetEntityId(item);
        if (entityId == Guid.Empty)
        {
            Application.Log.Warn("ModelTree: selected item has no associated entity.");
            return;
        }

        SelectionMode mode = Application.Selection.Service.Mode;
        bool shouldHandleAsRange = ShouldHandleAsRangeSelection(mode);

        if (!shouldHandleAsRange)
        {
            Application.Pick.Event.NotifyResult(PickUtility.PickByEntityId(entityId));
        }
        else
        {
            // Add/Removeモードでは範囲選択として扱い、複数実体の選択を通知する
            Guid[] entityIds = GetAllModelsInRange(_lastSelectedItem, item);
            Application.Pick.Event.NotifyResults(PickUtility.PickByEntityIds(entityIds));
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

        List<TreeItem> visibleItems = GetVisibleItemsInDisplayOrder();
        int lastIndex = visibleItems.IndexOf(lastItem);
        int selectedIndex = visibleItems.IndexOf(selectedItem);
        if (lastIndex < 0 || selectedIndex < 0)
        {
            return Array.Empty<Guid>();
        }

        int startIndex = Math.Min(lastIndex, selectedIndex);
        int endIndex = Math.Max(lastIndex, selectedIndex);

        List<Guid> modelIdsInRange = new();
        for (int i = startIndex; i <= endIndex; i++)
        {
            Guid modelId = TryGetModelId(visibleItems[i]);
            if (modelId != Guid.Empty)
            {
                modelIdsInRange.Add(modelId);
            }
        }

        return modelIdsInRange.ToArray();
    }

    /// <summary>
    /// 現在の見た目順（展開状態を考慮）で表示中の TreeItem 一覧を取得する
    /// </summary>
    private List<TreeItem> GetVisibleItemsInDisplayOrder()
    {
        List<TreeItem> visibleItems = new();
        TreeItem rootItem = GetRoot();
        if (rootItem == null)
        {
            return visibleItems;
        }

        CollectVisibleItemsDepthFirst(rootItem, visibleItems);
        return visibleItems;
    }

    private static void CollectVisibleItemsDepthFirst(TreeItem item, List<TreeItem> visibleItems)
    {
        TreeItem currentItem = item;
        while (currentItem != null)
        {
            visibleItems.Add(currentItem);

            if (!currentItem.Collapsed)
            {
                TreeItem child = currentItem.GetFirstChild();
                if (child != null)
                {
                    CollectVisibleItemsDepthFirst(child, visibleItems);
                }
            }

            currentItem = currentItem.GetNext();
        }
    }

    private static Guid TryGetEntityId(TreeItem item)
    {
        if (item == null)
        {
            return Guid.Empty;
        }

        Variant entityIdVariant = item.GetMeta("EntityId", Variant.CreateFrom(string.Empty));
        string entityIdText = entityIdVariant.AsString();
        return Guid.TryParse(entityIdText, out Guid entityId) ? entityId : Guid.Empty;
    }

    private static Guid TryGetModelId(TreeItem item) => TryGetEntityId(item);

    /// <summary>
    /// TreeItem の VisibleButton がクリックされたときの処理を行う
    /// </summary>
    /// <param name="item">クリックされた TreeItem</param>
    private void HandleVisibleButtonClicked(TreeItem item)
    {
        Guid entityId = TryGetEntityId(item);
        if (entityId == Guid.Empty)
        {
            Application.Log.Warn("ModelTree: clicked item has no associated entity.");
            return;
        }

        // モデル実体の表示状態を切り替える
        Application.Model.Event.ToggleModelVisibility(entityId);
    }

    /// <summary>
    /// TreeItem のフィットボタンがクリックされたとき、対応するモデルが画面に収まるよう表示する
    /// </summary>
    /// <param name="item">クリックされた TreeItem</param>
    private void HandleFitButtonClicked(TreeItem item)
    {
        ModelNode modelNode = GetModelNode(item);
        if (modelNode == null)
        {
            return;
        }

        Node3D[] fitTargetNodes = new Node3D[] { modelNode };
        Application.Viewport.Event.Fit(fitTargetNodes, true);
    }

    /// <summary>
    /// TreeItem の回転アピールボタンがクリックされたとき、対応するモデルを回転させる
    /// </summary>
    /// <param name="item">クリックされた TreeItem</param>
    private void HandleSpinAppealButtonClicked(TreeItem item)
    {
        ModelNode modelNode = GetModelNode(item);
        modelNode?.Emphasize();
        modelNode?.SpinAppeal();
    }

    /// <summary>
    /// TreeItem に対応する ModelNode を取得する
    /// </summary>
    /// <param name="item">対象の TreeItem</param>
    /// <returns>対応する ModelNode。取得できない場合は null</returns>
    private static ModelNode GetModelNode(TreeItem item)
    {
        Guid entityId = TryGetEntityId(item);
        if (entityId == Guid.Empty)
        {
            return null;
        }

        return Application.Model.Registry.GetEntity(entityId)?.Node;
    }

    #endregion
}