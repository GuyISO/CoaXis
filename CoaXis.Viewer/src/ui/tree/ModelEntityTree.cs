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

    private Guid _pendingScrollEntityId = Guid.Empty;

    private readonly HashSet<TreeItem> _highlightedItems = new(); // 直近のRefreshAllHighlightsで着色した TreeItem の集合、差分更新の比較に使う

    private Guid _rootEntityId = Guid.Empty;

    private Color _selectedColor;

    private bool _isTreeReady;

    // ユーザー操作による選択か、内部的なプログラムによる選択かを判定するフラグ
    private bool _ispublicSelection = false;

    #endregion

    #region Lifecycle

    public override void _Ready()
    {
        SubscribeUiEvents();
        SubscribeApplicationEvents();
        ApplyThemeColors();
        _isTreeReady = true;
        if (_rootEntityId != Guid.Empty)
        {
            RebuildTreeFromRegistry();
        }
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

            Application.Ui.Menu.ShowModelEntityMenu(entityId);
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
        ItemCollapsed += OnItemCollapsed;
        ItemActivated += OnItemActivated;
        Application.Ui.Theme.ThemeChanged += OnThemeChanged;
    }

    /// <summary>
    /// UIイベントの購読を解除する
    /// </summary>
    private void UnsubscribeUiEvents()
    {
        CellSelected -= OnCellSelected;
        ButtonClicked -= OnButtonClicked;
        ItemCollapsed -= OnItemCollapsed;
        ItemActivated -= OnItemActivated;
        Application.Ui.Theme.ThemeChanged -= OnThemeChanged;
    }
    
    /// <summary>
    /// Applicationイベントの購読を開始する
    /// </summary>
    private void SubscribeApplicationEvents()
    {
        Application.Model.Entity.Selection.Selected += OnModelEntitySelected;
        Application.Model.Entity.Selection.SelectionChanged += OnSelectionChanged;
        Application.Model.Entity.Selection.Cleared += OnSelectionCleared;
        Application.Model.Entity.Registry.Registered += OnModelEntityRegistered;
        Application.Model.Entity.Visual.VisibilityResolved += OnModelEntityVisibilityResolved;
        Application.Model.Property.Registry.Changed += OnModelPropertyChanged;
        Application.Model.Entity.State.PropertyTreeVisibleNotified += OnModelEntityPropertyTreeVisibleNotified;
        Application.Model.Entity.State.Collapsed += OnModelEntityCollapsed;
        Application.Model.Entity.State.StatusNotified += OnModelEntityStatusNotified;
        Application.Model.Entity.Registry.Cleared += OnModelEntityRegistryCleared;
        Application.Model.Entity.Tree.CenteringRequested += OnTreeCenteringRequested;
    }

    /// <summary>
    /// Applicationイベントの購読を解除する
    /// </summary>
    private void UnsubscribeApplicationEvents()
    {
        Application.Model.Entity.Selection.Selected -= OnModelEntitySelected;
        Application.Model.Entity.Selection.SelectionChanged -= OnSelectionChanged;
        Application.Model.Entity.Selection.Cleared -= OnSelectionCleared;
        Application.Model.Entity.Registry.Registered -= OnModelEntityRegistered;
        Application.Model.Entity.Visual.VisibilityResolved -= OnModelEntityVisibilityResolved;
        Application.Model.Property.Registry.Changed -= OnModelPropertyChanged;
        Application.Model.Entity.State.PropertyTreeVisibleNotified -= OnModelEntityPropertyTreeVisibleNotified;
        Application.Model.Entity.State.StatusNotified -= OnModelEntityStatusNotified;
        Application.Model.Entity.State.Collapsed -= OnModelEntityCollapsed;
        Application.Model.Entity.Registry.Cleared -= OnModelEntityRegistryCleared;
        Application.Model.Entity.Tree.CenteringRequested -= OnTreeCenteringRequested;
    }

    /// <summary>
    /// セルが選択されたときのイベントハンドラ、主にボタンのクリックを検知するために使用する
    /// </summary>
    private void OnCellSelected()
    {
        if (_ispublicSelection)
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
    /// テーマ切替後、各モデル行の状態色とアイコンを更新する。
    /// </summary>
    private void OnThemeChanged()
    {
        ApplyThemeColors();
    }

    /// <summary>
    /// 現在のテーマ色をモデル行へ適用する。
    /// </summary>
    private void ApplyThemeColors()
    {
        _selectedColor = Application.Ui.Theme.PrimaryColor;

        Dictionary<Guid, int> counts = ModelEntityRowBehavior.BuildPropertySubtreeCounts(Application.Model.Entity.Registry.Root);
        foreach (KeyValuePair<Guid, TreeItem> pair in _entityIdToTreeItem)
        {
            ModelEntity modelEntity = Application.Model.Entity.Registry.Get(pair.Key);
            if (modelEntity != null)
            {
                pair.Value.SetCustomColor(0, ResolveTextColor(modelEntity.Status));
                ModelEntityRowBehavior.UpdateThemeIcons(pair.Value, modelEntity,
                    ModelEntityRowBehavior.ResolvePropertyCount(modelEntity, counts), false, ResolveTextColor(modelEntity.Status));
            }
        }

        ReapplySelectedRowColors();
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

        ModelEntityRowBehavior.HandleButtonClick(TryGetEntityId(item), buttonId);
    }

    /// <summary>
    /// ツリー項目がアクティブ化されたとき、対象モデルのManipulate操作を通知する
    /// </summary>
    private void OnItemActivated()
    {
        Application.Model.Entity.Manipulate(TryGetEntityId(GetSelected()));
    }

    /// <summary>
    /// TreeItem の折り畳み状態が変化したときのイベントハンドラ
    /// </summary>
    /// <param name="item">折り畳み状態が変化した TreeItem</param>
    private void OnItemCollapsed(TreeItem item)
    {
        // 折り畳み/展開により「表示中の代替祖先」が変わりうるため、選択中モデル分のハイライトを全件見直して再描画する
        RefreshAllHighlights();

        Guid entityId = TryGetEntityId(item);
        ModelEntity modelEntity = Application.Model.Entity.Registry.Get(entityId);
        if (modelEntity == null)
        {
            Application.Log.Warn($"ModelEntityTree: collapsed target not found. entityId='{entityId}'");
            return;
        }

        // TreeItemを正本にせず、ModelEntityのアクセサーから状態変更と通知を行う。
        modelEntity.IsCollapsed = item.IsCollapsed();
    }

    /// <summary>
    /// モデルの選択状態が通知されたときのイベントハンドラ
    /// </summary>
    /// <param name="entityId">選択状態が変更された ModelEntity の識別子</param>
    private void OnModelEntitySelected(string entityId)
    {
        if (!Guid.TryParse(entityId, out Guid parsedEntityId) || parsedEntityId == Guid.Empty)
        {
            return;
        }

        if (!_entityIdToTreeItem.ContainsKey(parsedEntityId))
        {
            return;
        }

        if (Application.Model.Entity.Selection.Contains(parsedEntityId))
        {
            _pendingScrollEntityId = parsedEntityId;
        }
        else if (_pendingScrollEntityId == parsedEntityId)
        {
            _pendingScrollEntityId = Guid.Empty;
        }
    }

    /// <summary>
    /// 選択集合の確定後にハイライトとスクロールをまとめて更新する
    /// </summary>
    private void OnSelectionChanged()
    {
        RefreshAllHighlights();

        if (_pendingScrollEntityId != Guid.Empty
            && Application.Model.Entity.Selection.Contains(_pendingScrollEntityId)
            && _entityIdToTreeItem.TryGetValue(_pendingScrollEntityId, out TreeItem item))
        {
            ScrollToItem(FindNearestVisibleAncestor(item));
        }

        _pendingScrollEntityId = Guid.Empty;
    }

    /// <summary>
    /// 選択がクリアされたことを通知されたときのイベントハンドラ
    /// </summary>
    private void OnSelectionCleared()
    {
        _lastSelectedItem = null;
        _pendingScrollEntityId = Guid.Empty;
    }

    /// <summary>
    /// モデル集合が置換されたとき、Registryの階層からツリーを再構築する
    /// </summary>
    private void OnModelEntityRegistered()
    {
        RebuildTreeFromRegistry();
    }

    /// <summary>
    /// モデルの表示状態が通知されたときのイベントハンドラ
    /// </summary>
    /// <param name="entityId">表示状態が変更された ModelEntity の識別子</param>
    private void OnModelEntityVisibilityResolved(string entityId)
    {
        if (!Guid.TryParse(entityId, out Guid parsedEntityId) || parsedEntityId == Guid.Empty)
        {
            return;
        }

        ModelEntity modelEntity = Application.Model.Entity.Registry.Get(parsedEntityId);
        if (modelEntity == null)
        {
            return;
        }

        Application.Log.Debug($"ModelTree: visibility state notified. entityId='{parsedEntityId}', visibility={modelEntity.Visibility}");
        TreeItem treeItem = _entityIdToTreeItem.TryGetValue(parsedEntityId, out TreeItem item) ? item : null;
        if (treeItem != null)
        {
            ModelEntityRowBehavior.UpdateVisibilityButton(treeItem, modelEntity);
        }
    }

    /// <summary>
    /// Property の紐づけが変化したとき、全TreeItemのボタンを再構築する。
    /// </summary>
    private void OnModelPropertyChanged()
    {
        ModelEntity root = Application.Model.Entity.Registry.Root;
        if (root == null)
        {
            return;
        }

        Dictionary<Guid, int> propertySubtreeCounts = ModelEntityRowBehavior.BuildPropertySubtreeCounts(root);
        foreach (KeyValuePair<Guid, TreeItem> pair in _entityIdToTreeItem)
        {
            ModelEntity modelEntity = Application.Model.Entity.Registry.Get(pair.Key);
            if (modelEntity != null && propertySubtreeCounts.TryGetValue(pair.Key, out int subtreeCount))
            {
                int propertyCount = modelEntity.IsCollapsed ? subtreeCount : modelEntity.PropertyCountRecursive;
                ModelEntityRowBehavior.RebuildButtons(pair.Value, modelEntity, propertyCount, ResolveTextColor(modelEntity.Status));
            }
        }
    }

    /// <summary>
    /// Property OverlayTreeの表示状態が通知されたとき、対応するTreeItemのPropertyボタンを更新する。
    /// </summary>
    /// <param name="entityId">表示状態が変更された ModelEntity の識別子</param>
    private void OnModelEntityPropertyTreeVisibleNotified(string entityId)
    {
        if (!Guid.TryParse(entityId, out Guid parsedEntityId)
            || !_entityIdToTreeItem.TryGetValue(parsedEntityId, out TreeItem treeItem))
        {
            return;
        }

        ModelEntity modelEntity = Application.Model.Entity.Registry.Get(parsedEntityId);
        if (modelEntity != null)
        {
            Dictionary<Guid, int> propertySubtreeCounts = ModelEntityRowBehavior.BuildPropertySubtreeCounts(modelEntity);
            ModelEntityRowBehavior.RebuildButtons(treeItem, modelEntity, ModelEntityRowBehavior.ResolvePropertyCount(modelEntity, propertySubtreeCounts), ResolveTextColor(modelEntity.Status));
        }
    }

    /// <summary>
    /// モデルの折り畳み状態が通知されたときのイベントハンドラ
    /// </summary>
    /// <param name="entityId">折り畳み状態が変更された ModelEntity の識別子</param>
    private void OnModelEntityCollapsed(string entityId)
    {
        if (!Guid.TryParse(entityId, out Guid parsedEntityId))
        {
            return;
        }

        TreeItem item = _entityIdToTreeItem.TryGetValue(parsedEntityId, out TreeItem foundItem) ? foundItem : null;
        ModelEntity modelEntity = Application.Model.Entity.Registry.Get(parsedEntityId);
        if (item == null || modelEntity == null)
        {
            return;
        }
        bool isCollapsed = modelEntity.IsCollapsed;
        Dictionary<Guid, int> propertySubtreeCounts = ModelEntityRowBehavior.BuildPropertySubtreeCounts(modelEntity);
        ModelEntityRowBehavior.RebuildButtons(item, modelEntity, ModelEntityRowBehavior.ResolvePropertyCount(modelEntity, propertySubtreeCounts), ResolveTextColor(modelEntity.Status));
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
    private void OnModelEntityStatusNotified(string entityId)
    {
        if (!Guid.TryParse(entityId, out Guid parsedEntityId) || parsedEntityId == Guid.Empty)
        {
            return;
        }

        TreeItem treeItem = _entityIdToTreeItem.TryGetValue(parsedEntityId, out TreeItem item) ? item : null;
        ModelEntity modelEntity = Application.Model.Entity.Registry.Get(parsedEntityId);
        if (treeItem == null || modelEntity == null)
        {
            return;
        }
        Color textColor = ResolveTextColor(modelEntity.Status);
        treeItem.SetCustomColor(0, textColor);
        int propertyIndex = treeItem.GetButtonById(0, ModelEntityRowBehavior.PropertyButtonId);
        if (propertyIndex >= 0)
        {
            int count = modelEntity.PropertyCountRecursive;
            if (modelEntity.IsCollapsed)
            {
                Dictionary<Guid, int> counts = ModelEntityRowBehavior.BuildPropertySubtreeCounts(modelEntity);
                count = ModelEntityRowBehavior.ResolvePropertyCount(modelEntity, counts);
            }

            treeItem.SetButton(0, propertyIndex,
                ModelEntityRowBehavior.GetPropertyIcon(count, modelEntity.IsPropertyTreeVisible, textColor, modelEntity.Properties.Count > 0));
        }
    }

    /// <summary>
    /// モデルエンティティレジストリがクリアされたことを通知されたときのイベントハンドラ
    /// </summary>
    private void OnModelEntityRegistryCleared()
    {
        RebuildTreeFromRegistry();
    }

    private void RebuildTreeFromRegistry()
    {
        // Registryの確定済み階層を正として一度だけ再構築し、個別の親先行通知を不要にする。
        Clear();
        _entityIdToTreeItem.Clear();
        _highlightedItems.Clear();
        _lastSelectedItem = null;
        _pendingScrollEntityId = Guid.Empty;

        ModelEntity rootModelEntity = _rootEntityId == Guid.Empty
            ? Application.Model.Entity.Registry.Root
            : Application.Model.Entity.Registry.Get(_rootEntityId);
        if (rootModelEntity == null)
        {
            return;
        }

        if (Application.Model.Entity.Registry.Get(rootModelEntity.Id) == null)
        {
            return;
        }

        Dictionary<Guid, int> propertySubtreeCounts = ModelEntityRowBehavior.BuildPropertySubtreeCounts(rootModelEntity);
        AddToTree(rootModelEntity.Id, Guid.Empty, propertySubtreeCounts);
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
        _ispublicSelection = true;
        treeItem.Select(0);
        _ispublicSelection = false;
    }

    #endregion

    #region Methods

    /// <summary>
    /// 表示する ModelEntity の部分木を設定する。
    /// </summary>
    /// <param name="rootModelEntity">ツリーの起点となる ModelEntity</param>
    public void SetRoot(ModelEntity rootModelEntity)
    {
        if (rootModelEntity == null || rootModelEntity.Id == Guid.Empty)
        {
            Application.Log.Warn("ModelEntityTree: root entity is null or has an empty ID.");
            return;
        }

        _rootEntityId = rootModelEntity.Id;
        if (_isTreeReady)
        {
            RebuildTreeFromRegistry();
        }
    }
    
    #endregion

    #region Helpers

    /// <summary>
    /// 指定したモデルをツリーに追加する
    /// </summary>
    /// <param name="entityId">追加する子 ModelEntity の識別子</param>
    /// <param name="parentEntityId">親 ModelEntity の識別子</param>
    private void AddToTree(Guid entityId, Guid parentEntityId, IReadOnlyDictionary<Guid, int> propertySubtreeCounts)
    {
        // Tree への再構築時に、レジストリから既に消えているモデルや root 直前の空 ID を拾わないように防ぐ。
        // これがないと、Clear 中や既存モデルが破棄済みのタイミングで NullReference になりやすい。
        if (entityId == Guid.Empty)
        {
            return;
        }

        // 部分木通知の再送や循環参照があっても、同じモデルのTreeItemは一度だけ作る。
        if (_entityIdToTreeItem.ContainsKey(entityId))
        {
            return;
        }

        TreeItem parentTreeItem = _entityIdToTreeItem.TryGetValue(parentEntityId, out TreeItem item) ? item : null;
        ModelEntity modelEntity = Application.Model.Entity.Registry.Get(entityId);
        if (modelEntity == null)
        {
            return;
        }

        // ツリーにアイテムを追加、親が null の場合は初回のみルートアイテムとして追加される便利仕様
        TreeItem treeItem = CreateItem(parentTreeItem);

        // --- テキスト（名前） ---
        treeItem.SetText(0, modelEntity.Name);

        // --- 左側アイコン（ModelEntity に紐づくモデルアイコン） ---
        Texture2D icon = ModelEntityRowBehavior.GetModelIcon(modelEntity.IconId);
        treeItem.SetIcon(0, icon);

        // --- 右側ボタン（Property表示・Visibility切替） ---
        int propertyCount = ModelEntityRowBehavior.ResolvePropertyCount(modelEntity, propertySubtreeCounts);
        ModelEntityRowBehavior.RebuildButtons(treeItem, modelEntity, propertyCount, ResolveTextColor(modelEntity.Status));

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
            AddToTree(childModelEntity.Id, entityId, propertySubtreeCounts);
        }
    }

    /// <summary>
    private static Color ResolveTextColor(ModelStatus status)
    {
        return Application.Ui.Theme.GetModelStatusColor(status);
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
    /// 選択集合や折り畳み状態の変更後に全件を計算し直し、実際の描画APIは変化があった TreeItem のみに適用する。
    /// </summary>
    private void RefreshAllHighlights()
    {
        HashSet<TreeItem> nextHighlightedItems = new();
        foreach (Guid entityId in Application.Model.Entity.Selection.SelectedIds)
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

        ModelSelectionMode mode = Application.Model.Selection.CurrentMode;
        bool shouldHandleAsRange = ShouldHandleAsRangeSelection(mode);

        if (!shouldHandleAsRange)
        {
            Application.Model.Entity.Pick.NotifyResult(PickUtility.PickByEntityId(entityId));
        }
        else
        {
            // Add/Removeモードでは範囲選択として扱い、複数実体の選択を通知する
            Guid[] entityIds = GetAllModelsInRange(_lastSelectedItem, item);
            Application.Model.Entity.Pick.NotifyResults(PickUtility.PickByEntityIds(entityIds));
        }
    }

    /// <summary>
    /// 範囲選択として扱うべきかどうかを判定する
    /// </summary>
    /// <param name="mode">現在の選択モード</param>
    /// <returns>範囲選択として扱う場合は true</returns>
    private bool ShouldHandleAsRangeSelection(ModelSelectionMode mode)
    {
        // AddモードやRemoveモードかつすでに何か選択中のアイテムがある場合は範囲選択として扱う
        if (mode != ModelSelectionMode.Add && mode != ModelSelectionMode.Remove)
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

    #endregion
}