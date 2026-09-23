using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// モデルの階層ツリー表示と操作を行うUIコンポーネント
/// </summary>
public partial class ModelEntityTree : Tree
{
    #region Fields

    private Dictionary<Guid, TreeItem> _entityIdToTreeItem = new(); // ModelEntity の Id -> TreeItem の対応辞書

    private TreeItem _lastSelectedItem; // 最後に選択された TreeItem を保持

    private readonly HashSet<TreeItem> _highlightedItems = new(); // 直近のRefreshAllHighlightsで着色した TreeItem の集合、差分更新の比較に使う

    private ModelEntity _rootModelEntity; // このツリーのルートモデル実体のキャッシュ、シーン全体のルートではないことに注意

    private Color _selectedColor;

    // ユーザー操作による選択か、内部的なプログラムによる選択かを判定するフラグ
    private bool _isInternalSelection = false;

    private const int VisibilityButtonId = 1;
    private const int FitButtonId = 2;

    #endregion

    #region Lifecycle

    public override void _Ready()
    {
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
        if (@event is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Right && mb.Pressed)
        {
            TreeItem targetItem = GetItemAtPosition(GetLocalMousePosition());
            // 空白領域での右クリックは対象不明のメニュー表示になり紛らわしいため、TreeItem上のみメニューを出す
            if (targetItem == null)
            {
                return;
            }

            // メニューはTreeItemではなくModelEntity単位で対象を扱う設計のため、ここでGuidへ変換してから渡す
            Guid entityId = TryGetEntityId(targetItem);
            // 対象が無い空振りクリックではメニューを表示しない
            if (entityId == Guid.Empty)
            {
                return;
            }

            Application.Menu.Service.ShowModelEntityMenu(entityId);
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
        ItemCollapsed += OnItemCollapsed;
    }

    /// <summary>
    /// UIイベントの購読を解除する
    /// </summary>
    private void UnsubscribeUiEvents()
    {
        CellSelected -= OnCellSelected;
        ButtonClicked -= OnButtonClicked;
        ItemActivated -= OnItemActivated;
        ItemCollapsed -= OnItemCollapsed;
    }
    
    /// <summary>
    /// Applicationイベントの購読を開始する
    /// </summary>
    private void SubscribeApplicationEvents()
    {
        Application.Setting.Event.SettingsNotified += ApplySettings;
        Application.Selection.Event.ModelStateNotified += OnModelSelectionStateNotified;
        Application.Selection.Event.ClearedNotified += OnClearedNotified;
        Application.Model.Event.Added += OnModelAddedNotified;
        Application.Model.Event.VisibilityNotified += OnModelVisibilityNotified;
        Application.Model.Event.Collapsed += OnModelCollapsed;
        Application.Model.Event.StatusNotified += OnModelStatusNotified;
        Application.Model.Event.RegistryCleared += OnRegistryClearedNotified;
        Application.Model.Event.TreeCenteringRequested += OnTreeCenteringRequested;
    }

    /// <summary>
    /// Applicationイベントの購読を解除する
    /// </summary>
    private void UnsubscribeApplicationEvents()
    {
        Application.Setting.Event.SettingsNotified -= ApplySettings;
        Application.Selection.Event.ModelStateNotified -= OnModelSelectionStateNotified;
        Application.Selection.Event.ClearedNotified -= OnClearedNotified;
        Application.Model.Event.Added -= OnModelAddedNotified;
        Application.Model.Event.VisibilityNotified -= OnModelVisibilityNotified;
        Application.Model.Event.StatusNotified -= OnModelStatusNotified;
        Application.Model.Event.Collapsed -= OnModelCollapsed;
        Application.Model.Event.RegistryCleared -= OnRegistryClearedNotified;
        Application.Model.Event.TreeCenteringRequested -= OnTreeCenteringRequested;
    }

    /// <summary>
    /// セルが選択されたときのイベントハンドラ、主にボタンのクリックを検知するために使用する
    /// </summary>
    private void OnCellSelected()
    {
        if (_isInternalSelection)
        {
            return;
        }

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

        Application.Model.Service.EmbededModelPropertyTree.Show(TryGetEntityId(item));
    }

    /// <summary>
    /// TreeItem の折り畳み状態が変化したときのイベントハンドラ
    /// </summary>
    /// <param name="item">折り畳み状態が変化した TreeItem</param>
    private void OnItemCollapsed(TreeItem item)
    {
        // 折り畳み/展開により「表示中の代替祖先」が変わりうるため、選択中モデル分のハイライトを全件見直して再描画する
        RefreshAllHighlights();

        // モデルの折り畳み状態が変更されたことを通知
        Application.Model.Event.NotifyCollapsed(TryGetEntityId(item), item.IsCollapsed());
    }

    /// <summary>
    /// モデルの選択状態が通知されたときのイベントハンドラ
    /// </summary>
    /// <param name="entityId">選択状態が変更された ModelEntity の識別子</param>
    /// <param name="isSelected">モデルが選択されている場合はtrue、選択されていない場合はfalse</param>
    private void OnModelSelectionStateNotified(string entityId, bool isSelected)
    {
        if (!Guid.TryParse(entityId, out Guid parsedEntityId) || parsedEntityId == Guid.Empty)
        {
            return;
        }

        TreeItem treeItem = _entityIdToTreeItem.TryGetValue(parsedEntityId, out TreeItem item) ? item : null;
        if (treeItem == null)
        {
            return;
        }

        // 差分更新だと折り畳み状態とのタイミングでズレるため、選択変更のたびに全件を見直して再描画する
        RefreshAllHighlights();

        if (isSelected)
        {
            // 対象が畳まれた祖先の下に隠れている場合、CATIA同様に表示中の祖先までスクロールする
            ScrollToItem(FindNearestVisibleAncestor(treeItem));
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
    /// <param name="entityId">追加する子 ModelEntity の識別子</param>
    /// <param name="parentEntityId">追加先の親 ModelEntity の識別子</param>
    private void OnModelAddedNotified(string entityId, string parentEntityId)
    {
        if (!Guid.TryParse(entityId, out Guid parsedEntityId) || parsedEntityId == Guid.Empty)
        {
            Application.Log.Warn($"ModelTree: failed to add entity. invalid child entityId='{entityId}'");
            return;
        }

        Guid parsedParentEntityId = Guid.Empty;
        if (!string.IsNullOrWhiteSpace(parentEntityId))
        {
            Guid.TryParse(parentEntityId, out parsedParentEntityId);
        }

        if (parsedParentEntityId == Guid.Empty)
        {
            AddToTree(parsedEntityId, Guid.Empty);
            return;
        }
        
        AddToTree(parsedEntityId, parsedParentEntityId);
    }

    /// <summary>
    /// モデルの表示状態が通知されたときのイベントハンドラ
    /// </summary>
    /// <param name="entityId">表示状態が変更された ModelEntity の識別子</param>
    /// <param name="visibility">変更後のモデル表示設定</param>
    private void OnModelVisibilityNotified(string entityId, ModelVisibility visibility)
    {
        if (!Guid.TryParse(entityId, out Guid parsedEntityId) || parsedEntityId == Guid.Empty)
        {
            return;
        }

        ModelEntity modelEntity = Application.Model.Registry.GetEntity(parsedEntityId);
        if (modelEntity == null)
        {
            return;
        }

        Application.Log.Debug($"ModelTree: visibility state notified. entityId='{parsedEntityId}', visibility={modelEntity.Visibility}");
        TreeItem treeItem = _entityIdToTreeItem.TryGetValue(parsedEntityId, out TreeItem item) ? item : null;
        if (treeItem != null)
        {
            Texture2D buttonIcon = Application.Asset.Service.GetVisibilityIcon(
                visibility,
                ModelVisibilityResolver.IsVisible(modelEntity),
                Constant.Ui.Tree.HierarchyVisibleIconSize)
                ?? Application.Asset.Service.GetVisibilityIcon(
                    ModelVisibility.Visible,
                    Constant.Ui.Tree.HierarchyVisibleIconSize);
            treeItem.SetButton(0, 0, buttonIcon);
        }
    }

    /// <summary>
    /// モデルの折り畳み状態が通知されたときのイベントハンドラ
    /// </summary>
    /// <param name="entityId">折り畳み状態が変更された ModelEntity の識別子</param>
    /// <param name="isCollapsed">モデルが折り畳まれている場合はtrue、展開されている場合はfalse</param>
    private void OnModelCollapsed(string entityId, bool isCollapsed)
    {
        TreeItem item = _entityIdToTreeItem.TryGetValue(Guid.Parse(entityId), out TreeItem foundItem) ? foundItem : null;
        if (item == null)
        {
            return;
        }
        // すでに折り畳み状態が一致している場合は自分が発した通知による変更の可能性があり何もしない
        if (item.IsCollapsed() == isCollapsed)
        {
            return;
        }
        item.SetCollapsed(isCollapsed);
    }

    /// <summary>
    /// モデルのステータスが更新されたときのイベントハンドラ
    /// </summary>
    /// <param name="entityId">ステータス更新対象の ModelEntity の識別子</param>
    /// <param name="status">更新後のステータス</param>
    private void OnModelStatusNotified(string entityId, int status)
    {
        if (!Guid.TryParse(entityId, out Guid parsedEntityId) || parsedEntityId == Guid.Empty)
        {
            return;
        }

        TreeItem treeItem = _entityIdToTreeItem.TryGetValue(parsedEntityId, out TreeItem item) ? item : null;
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
        _entityIdToTreeItem.Clear();
        _highlightedItems.Clear();
        _lastSelectedItem = null;

        if (Application.Model.Registry.RootEntity == null)
        {
            return;
        }

        if (Application.Model.Registry.GetEntity(Application.Model.Registry.RootEntity.Id) == null)
        {
            return;
        }

        AddToTree(Application.Model.Registry.RootEntity.Id, Guid.Empty);
    }

    /// <summary>
    /// ツリーのセンタリング要求を受け取ったとき、対象モデルを中央へ表示する
    /// </summary>
    /// <param name="entityId">中央へ表示するモデル実体の識別子</param>
    private void OnTreeCenteringRequested(string entityId)
    {
        if (!Guid.TryParse(entityId, out Guid parsedEntityId) || parsedEntityId == Guid.Empty)
        {
            return;
        }

        if (!_entityIdToTreeItem.TryGetValue(parsedEntityId, out TreeItem treeItem))
        {
            return;
        }

        // 対象行を画面中央へ出すため、先に祖先を展開してからスクロールする。
        ExpandAncestors(treeItem);
        ScrollToItem(treeItem, true);
        
        // 選択イベントを発火させず、ツリー選択状態ハイライトでユーザーに視覚的なフィードバックを与える
        _isInternalSelection = true;
        treeItem.Select(0);
        _isInternalSelection = false;
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
    /// <param name="entityId">追加する子 ModelEntity の識別子</param>
    /// <param name="parentEntityId">親 ModelEntity の識別子</param>
    private void AddToTree(Guid entityId, Guid parentEntityId)
    {
        // Tree への再構築時に、レジストリから既に消えているモデルや root 直前の空 ID を拾わないように防ぐ。
        // これがないと、Clear 中や既存モデルが破棄済みのタイミングで NullReference になりやすい。
        if (entityId == Guid.Empty)
        {
            return;
        }

        // 親追加時の子孫再帰と個別のModelAdded通知が重なるため、同じモデルのTreeItemは一度だけ作る。
        if (_entityIdToTreeItem.ContainsKey(entityId))
        {
            return;
        }

        TreeItem parentTreeItem = _entityIdToTreeItem.TryGetValue(parentEntityId, out TreeItem item) ? item : null;
        ModelEntity modelEntity = Application.Model.Registry.GetEntity(entityId);
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
        treeItem.AddButton(0, btnIcon, id: VisibilityButtonId);
        treeItem.AddButton(0, fitIcon, id: FitButtonId);

        // EntityId と TreeItem の対応を登録
        treeItem.SetMeta("EntityId", entityId.ToString());
        treeItem.SetCustomColor(0, ResolveTextColor(modelEntity.Status));
         treeItem.Collapsed = modelEntity.IsCollapsed;
        _entityIdToTreeItem.Add(entityId, treeItem);

        // 子ノードを再帰的に追加
        foreach (ModelEntity childModelEntity in modelEntity.Children)
        {
            if (childModelEntity == null)
            {
                continue;
            }

            // ModelNode のみをツリーに追加する
            AddToTree(childModelEntity.Id, entityId);
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
        RefreshAllHighlights();
    }

    /// <summary>
    /// 祖先の折り畳みで隠れている場合に、実際に画面へ表示される最も近い祖先アイテムを取得する
    /// </summary>
    /// <param name="item">起点となる TreeItem</param>
    /// <returns>表示中の TreeItem、隠れていなければ item 自身</returns>
    private static TreeItem FindNearestVisibleAncestor(TreeItem item)
    {
        List<TreeItem> pathToRoot = new();
        for (TreeItem current = item; current != null; current = current.GetParent())
        {
            pathToRoot.Add(current);
        }

        // ルート側から辿り、最初に畳まれている祖先が実際の表示上の代替アイテムとなる
        for (int i = pathToRoot.Count - 1; i >= 1; i--)
        {
            if (pathToRoot[i].Collapsed)
            {
                return pathToRoot[i];
            }
        }

        return item;
    }

    /// <summary>
    /// 現在の選択状態と折り畳み状態から着色すべき TreeItem 集合を計算し直し、前回との差分だけ着色/解除する。
    /// 参照カウントによる差分更新は Collapsed/Selected の発火順序によって選択状態と着色状態がズレる不具合があったため、
    /// 「都度全件を計算し直す」方針は維持しつつ、実際に描画APIを叩くのは変化があった TreeItem のみに絞って高速化している。
    /// </summary>
    private void RefreshAllHighlights()
    {
        HashSet<TreeItem> nextHighlightedItems = new();
        foreach (Guid entityId in Application.Selection.Service.EntityIds)
        {
            if (entityId == Guid.Empty)
            {
                continue;
            }

            TreeItem treeItem = _entityIdToTreeItem.TryGetValue(entityId, out TreeItem item) ? item : null;
            if (treeItem == null)
            {
                continue;
            }

            // 祖先が畳まれている場合は、CATIA同様に表示中の代替祖先へハイライトを譲る
            nextHighlightedItems.Add(FindNearestVisibleAncestor(treeItem));
        }

        // 今回不要になったものだけ解除し、新規に必要なものだけ着色する
        foreach (TreeItem item in _highlightedItems)
        {
            if (!nextHighlightedItems.Contains(item))
            {
                item.ClearCustomBgColor(0);
            }
        }
        foreach (TreeItem item in nextHighlightedItems)
        {
            if (!_highlightedItems.Contains(item))
            {
                item.SetCustomBgColor(0, _selectedColor);
            }
        }

        _highlightedItems.Clear();
        _highlightedItems.UnionWith(nextHighlightedItems);
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

        List<Guid> entityIdsInRange = new();
        for (int i = startIndex; i <= endIndex; i++)
        {
            Guid entityId = TryGetEntityId(visibleItems[i]);
            if (entityId != Guid.Empty)
            {
                entityIdsInRange.Add(entityId);
            }
        }

        return entityIdsInRange.ToArray();
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

    /// <summary>
    /// 指定したツリー項目までの祖先をすべて展開する
    /// </summary>
    /// <param name="treeItem">展開対象の TreeItem</param>
    private static void ExpandAncestors(TreeItem treeItem)
    {
        for (TreeItem ancestor = treeItem.GetParent(); ancestor != null; ancestor = ancestor.GetParent())
        {
            ancestor.Collapsed = false;
        }
    }

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