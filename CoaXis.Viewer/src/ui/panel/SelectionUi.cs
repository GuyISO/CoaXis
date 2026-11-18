using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// モデル選択状態表示と操作用のパネル
/// </summary>
public partial class SelectionUi : PanelContainer
{
	#region Fields

	private readonly Dictionary<Guid, TreeItem> _entityIdToTreeItem = new();
	private readonly Dictionary<Guid, TreeItem> _propertyEntityIdToTreeItem = new();
	private readonly Dictionary<Guid, TreeItem> _propertyIdToTreeItem = new();
	private bool _isUpdatingTree = false;
	private bool _isRebuildQueued = false;
	private Guid _lastSelectedPropertyId = Guid.Empty;

	// 関連ノードのキャッシュ
	private Tree _tree = null!;
	private Label _selectionCountLabel = null!;
	private Button _buttonSet = null!;
	private Button _buttonAdd = null!;
	private Button _buttonRemove = null!;
	private Button _buttonToggle = null!;
	private Button _buttonClear = null!;

	#endregion

	#region Lifecycle

	public override void _Ready()
	{
		EnsureChildNodes();
		SubscribeUiEvents();
		SubscribeApplicationEvents();
		SyncInitialState();
	}

	public override void _ExitTree()
	{
		UnsubscribeUiEvents();
		UnsubscribeApplicationEvents();

		base._ExitTree();
	}

	#endregion

	#region Events

	/// <summary>
	/// 子ノードを解決し、フィールドに保持する
	/// </summary>
	private void EnsureChildNodes()
	{
		// シーン構造が変更される可能性があるため、名前探索で関連ノードを解決する
		_tree = (Tree)FindChild("Tree");
		_selectionCountLabel = (Label)FindChild("SelectionCountLabel");
		_buttonSet = (Button)FindChild("ButtonSet");
		_buttonAdd = (Button)FindChild("ButtonAdd");
		_buttonRemove = (Button)FindChild("ButtonRemove");
		_buttonToggle = (Button)FindChild("ButtonToggle");
		_buttonClear = (Button)FindChild("ButtonClear");
	}
	
	/// <summary>
	/// UIイベントの購読を開始する
	/// </summary>
	private void SubscribeUiEvents()
	{
		_buttonSet.Pressed += OnButtonSetPressed;
		_buttonAdd.Pressed += OnButtonAddPressed;
		_buttonRemove.Pressed += OnButtonRemovePressed;
		_buttonToggle.Pressed += OnButtonTogglePressed;
		_buttonClear.Pressed += OnButtonClearPressed;
		_tree.GuiInput += OnTreeGuiInput;
		_tree.ItemSelected += OnTreeItemSelected;
		_tree.ItemActivated += OnTreeItemActivated;
		_tree.ButtonClicked += OnTreeButtonClicked;
	}

	/// <summary>
	/// UIイベントの購読を解除する
	/// </summary>
	private void UnsubscribeUiEvents()
	{
		_buttonSet.Pressed -= OnButtonSetPressed;
		_buttonAdd.Pressed -= OnButtonAddPressed;
		_buttonRemove.Pressed -= OnButtonRemovePressed;
		_buttonToggle.Pressed -= OnButtonTogglePressed;
		_buttonClear.Pressed -= OnButtonClearPressed;
		_tree.GuiInput -= OnTreeGuiInput;
		_tree.ItemSelected -= OnTreeItemSelected;
		_tree.ItemActivated -= OnTreeItemActivated;
		_tree.ButtonClicked -= OnTreeButtonClicked;
	}

	/// <summary>
	/// Applicationイベントの購読を開始する
	/// </summary>
	private void SubscribeApplicationEvents()
	{
		Application.Model.Entity.Pick.HandlingModeNotified += OnPickHandlingModeNotified;
		Application.Model.Selection.ModeNotified += OnModeNotified;
		Application.Model.Entity.Selection.SelectionChanged += OnSelectionChanged;
		Application.Model.Property.Selection.SelectionChanged += OnSelectionChanged;
		Application.Model.Property.Selection.Cleared += OnPropertySelectionCleared;
		Application.Model.Entity.Registry.Registered += OnEntityRegistryRegistered;
		Application.Model.Entity.Visual.VisibilityResolved += OnEntityVisibilityResolved;
		Application.Model.Entity.State.PropertyTreeVisibleNotified += OnEntityButtonsChanged;
		Application.Model.Entity.State.Collapsed += OnEntityButtonsChanged;
		Application.Model.Property.Registry.Changed += OnPropertyRegistryChanged;
		Application.Model.Property.Visual.VisibilityResolved += OnPropertyVisibilityResolved;
		Application.Model.Property.State.ValueNotified += OnPropertyValueNotified;
		Application.Ui.Theme.ThemeChanged += OnThemeChanged;
	}

	/// <summary>
	/// Applicationイベントの購読を解除する
	/// </summary>
	private void UnsubscribeApplicationEvents()
	{
		Application.Model.Entity.Pick.HandlingModeNotified -= OnPickHandlingModeNotified;
		Application.Model.Selection.ModeNotified -= OnModeNotified;
		Application.Model.Entity.Selection.SelectionChanged -= OnSelectionChanged;
		Application.Model.Property.Selection.SelectionChanged -= OnSelectionChanged;
		Application.Model.Property.Selection.Cleared -= OnPropertySelectionCleared;
		Application.Model.Entity.Registry.Registered -= OnEntityRegistryRegistered;
		Application.Model.Entity.Visual.VisibilityResolved -= OnEntityVisibilityResolved;
		Application.Model.Entity.State.PropertyTreeVisibleNotified -= OnEntityButtonsChanged;
		Application.Model.Entity.State.Collapsed -= OnEntityButtonsChanged;
		Application.Model.Property.Registry.Changed -= OnPropertyRegistryChanged;
		Application.Model.Property.Visual.VisibilityResolved -= OnPropertyVisibilityResolved;
		Application.Model.Property.State.ValueNotified -= OnPropertyValueNotified;
		Application.Ui.Theme.ThemeChanged -= OnThemeChanged;
	}

	/// <summary>
	/// テーマ切替後、選択行の背景色とアイコンを更新する。
	/// </summary>
	private void OnThemeChanged()
	{
		Color selectionColor = ModelEntityRowBehavior.GetSelectionColor();
		Dictionary<Guid, int> counts = ModelEntityRowBehavior.BuildPropertySubtreeCounts(Application.Model.Entity.Registry.Root);
		foreach (KeyValuePair<Guid, TreeItem> pair in _entityIdToTreeItem)
		{
			ModelEntityRowBehavior.ApplySelectionHighlight(pair.Value, pair.Key, selectionColor);
			ModelEntity entity = Application.Model.Entity.Registry.Get(pair.Key);
			if (entity != null)
			{
				ModelEntityRowBehavior.UpdateThemeIcons(pair.Value, entity,
					ModelEntityRowBehavior.ResolvePropertyCount(entity, counts), true);
			}
		}

		foreach (KeyValuePair<Guid, TreeItem> pair in _propertyEntityIdToTreeItem)
		{
			ModelEntity entity = Application.Model.Entity.Registry.Get(pair.Key);
			if (entity != null)
			{
				pair.Value.SetIcon(0, ModelEntityRowBehavior.GetIndentedIcon(entity));
			}
		}

		foreach (KeyValuePair<Guid, TreeItem> pair in _propertyIdToTreeItem)
		{
			ModelProperty property = Application.Model.Property.Registry.Get(pair.Key);
			if (property != null)
			{
				ModelPropertyRowBehavior.UpdateThemeIcons(pair.Value, property, true);
				ModelPropertyRowBehavior.ApplySelectionHighlight(pair.Value, pair.Key, selectionColor);
			}
		}
	}

	/// <summary>
	/// ピック操作モードの通知を受け取り、Selection以外ではモードボタンを解除する
	/// </summary>
	private void OnPickHandlingModeNotified()
	{
		if (Application.Model.Entity.Pick.HandlingMode == PickHandlingMode.Selection)
		{
			UpdateModeButtons(Application.Model.Selection.CurrentMode);
			return;
		}

		UpdateModeButtons(null);
	}

	/// <summary>
	/// Set ボタン押下時のイベントハンドラ
	/// </summary>
	private void OnButtonSetPressed()
	{
		Application.Model.Entity.Pick.SetHandlingMode(PickHandlingMode.Selection);
		Application.Model.Selection.SetMode(ModelSelectionMode.Set);
	}

	/// <summary>
	/// Add ボタン押下時のイベントハンドラ
	/// </summary>
	private void OnButtonAddPressed()
	{
		Application.Model.Entity.Pick.SetHandlingMode(PickHandlingMode.Selection);
		Application.Model.Selection.SetMode(ModelSelectionMode.Add);
	}

	/// <summary>
	/// Remove ボタン押下時のイベントハンドラ
	/// </summary>
	private void OnButtonRemovePressed()
	{
		Application.Model.Entity.Pick.SetHandlingMode(PickHandlingMode.Selection);
		Application.Model.Selection.SetMode(ModelSelectionMode.Remove);
	}

	/// <summary>
	/// Toggle ボタン押下時のイベントハンドラ
	/// </summary>
	private void OnButtonTogglePressed()
	{
		Application.Model.Entity.Pick.SetHandlingMode(PickHandlingMode.Selection);
		Application.Model.Selection.SetMode(ModelSelectionMode.Toggle);
	}

	/// <summary>
	/// Clear ボタン押下時のイベントハンドラ
	/// </summary>
	private void OnButtonClearPressed()
	{
		Application.Model.Selection.Clear();
	}

	/// <summary>
	/// Tree の GUI 入力を受け取ったときのイベントハンドラ
	/// </summary>
	/// <param name="event">発生した入力イベント</param>
	/// <remarks>右クリックによるコンテキストメニュー表示のみを扱う。ModelEntityTree と同じくTreeItem単位ではなくModelEntity単位で対象を扱う。</remarks>
	private void OnTreeGuiInput(InputEvent @event)
	{
		if (@event is not InputEventMouseButton mb || mb.ButtonIndex != MouseButton.Right || !mb.Pressed)
		{
			return;
		}

		TreeItem targetItem = _tree.GetItemAtPosition(_tree.GetLocalMousePosition());
		if (ModelPropertyRowBehavior.GetPropertyId(targetItem) != Guid.Empty)
		{
			ModelPropertyRowBehavior.ShowContextMenu(targetItem);
			return;
		}

		Guid entityId = TryGetEntityId(targetItem);
		// 対象が無い空振りクリックではメニューを表示しない
		if (entityId == Guid.Empty)
		{
			return;
		}

		ModelEntityRowBehavior.ShowContextMenu(entityId);
	}

	/// <summary>
	/// Tree の項目がアクティブ化されたときのイベントハンドラ
	/// </summary>
	private void OnTreeItemActivated()
	{
		TreeItem selectedItem = _tree.GetSelected();
		Guid propertyId = ModelPropertyRowBehavior.GetPropertyId(selectedItem);
		if (propertyId != Guid.Empty)
		{
			ModelPropertyRowBehavior.Manipulate(propertyId);
			return;
		}

		ModelEntityRowBehavior.Manipulate(TryGetEntityId(selectedItem));
	}

	/// <summary>
	/// Entity行のPropertyまたはVisibilityボタンを操作する。
	/// </summary>
	private void OnTreeButtonClicked(TreeItem item, long column, long buttonId, long mouseButtonIndex)
	{
		if (item == null || column != 0)
		{
			return;
		}

		Guid propertyId = ModelPropertyRowBehavior.GetPropertyId(item);
		if (propertyId != Guid.Empty)
		{
			ModelPropertyRowBehavior.HandleButtonClick(propertyId, buttonId);
			return;
		}

		ModelEntityRowBehavior.HandleButtonClick(TryGetEntityId(item), buttonId);
	}

	/// <summary>
	/// Tree の行が選択されたとき、EntityまたはPropertyの選択を更新する。
	/// </summary>
	private void OnTreeItemSelected()
	{
		if (_isUpdatingTree)
		{
			return;
		}

		TreeItem item = _tree.GetSelected();
		Guid propertyId = ModelPropertyRowBehavior.GetPropertyId(item);
		if (propertyId == Guid.Empty)
		{
			Guid entityId = TryGetEntityId(item);
			if (entityId != Guid.Empty)
			{
				Application.Model.Entity.Pick.NotifyResult(PickUtility.PickByEntityId(entityId));
			}

			return;
		}

		ModelSelectionMode mode = Application.Model.Selection.CurrentMode;
		Guid[] ids = (mode == ModelSelectionMode.Add || mode == ModelSelectionMode.Remove)
			&& _lastSelectedPropertyId != Guid.Empty
			? GetPropertyIdsInRange(_lastSelectedPropertyId, propertyId)
			: new[] { propertyId };

		switch (mode)
		{
			case ModelSelectionMode.Set:
				Application.Model.Property.Selection.Set(ids);
				break;
			case ModelSelectionMode.Add:
				Application.Model.Property.Selection.Add(ids);
				break;
			case ModelSelectionMode.Remove:
				Application.Model.Property.Selection.Remove(ids);
				break;
			case ModelSelectionMode.Toggle:
				Application.Model.Property.Selection.Toggle(ids);
				break;
			default:
				Application.Log.Warn($"SelectionUi: Unknown selection mode {mode}.");
				break;
		}

		_lastSelectedPropertyId = propertyId;
	}

	/// <summary>
	/// 選択モード通知を受け取ったときに呼び出されるイベントハンドラ
	/// </summary>
	private void OnModeNotified()
	{
		UpdateModeButtons(Application.Model.Selection.CurrentMode);
	}

	/// <summary>
	/// EntityまたはPropertyの選択状態変更後に表示を更新する。
	/// </summary>
	private void OnSelectionChanged()
	{
		QueueRebuildTree();
	}

	/// <summary>
	/// Property選択が解除または置換されたとき、範囲選択の起点を更新する。
	/// </summary>
	private void OnPropertySelectionCleared()
	{
		_lastSelectedPropertyId = Guid.Empty;
	}

	/// <summary>
	/// Entity集合の更新後に一覧を再構築する。
	/// </summary>
	private void OnEntityRegistryRegistered()
	{
		if (Application.Model.Entity.Selection.Count > 0)
		{
			QueueRebuildTree();
		}
	}

	/// <summary>
	/// 実効表示状態が変化したEntityのボタンを更新する。
	/// </summary>
	private void OnEntityVisibilityResolved(string entityId)
	{
		if (Guid.TryParse(entityId, out Guid id) &&
			_entityIdToTreeItem.TryGetValue(id, out TreeItem item))
		{
			ModelEntity entity = Application.Model.Entity.Registry.Get(id);
			if (entity != null)
			{
				ModelEntityRowBehavior.UpdateVisibilityButton(item, entity);
			}
		}
	}

	/// <summary>
	/// 実効表示状態が変化したPropertyのVisibilityボタンを更新する。
	/// </summary>
	private void OnPropertyVisibilityResolved(string propertyId)
	{
		if (Guid.TryParse(propertyId, out Guid id)
			&& _propertyIdToTreeItem.TryGetValue(id, out TreeItem item))
		{
			ModelProperty property = Application.Model.Property.Registry.Get(id);
			if (property != null)
			{
				ModelPropertyRowBehavior.UpdateVisibilityButton(item, property);
			}
		}
	}

	/// <summary>
	/// 選択中Propertyの値変更を行表示へ反映する。
	/// </summary>
	private void OnPropertyValueNotified(string propertyId)
	{
		if (Guid.TryParse(propertyId, out Guid id)
			&& _propertyIdToTreeItem.TryGetValue(id, out TreeItem item))
		{
			ModelProperty property = Application.Model.Property.Registry.Get(id);
			if (property != null)
			{
				ModelPropertyRowBehavior.UpdateText(item, property);
			}
		}
	}

	/// <summary>
	/// Property表示または折り畳み状態が変化したEntityのボタンを更新する。
	/// </summary>
	private void OnEntityButtonsChanged(string entityId)
	{
		if (Guid.TryParse(entityId, out Guid id) &&
			_entityIdToTreeItem.TryGetValue(id, out TreeItem item))
		{
			ModelEntity entity = Application.Model.Entity.Registry.Get(id);
			if (entity != null)
			{
				Dictionary<Guid, int> counts = ModelEntityRowBehavior.BuildPropertySubtreeCounts(entity);
				ModelEntityRowBehavior.RebuildButtons(item, entity, ModelEntityRowBehavior.ResolvePropertyCount(entity, counts));
			}
		}
	}

	/// <summary>
	/// Property集合の変更を各行のボタンへ反映する。
	/// </summary>
	private void OnPropertyRegistryChanged()
	{
		if (_entityIdToTreeItem.Count > 0)
		{
			Dictionary<Guid, int> counts = ModelEntityRowBehavior.BuildPropertySubtreeCounts(Application.Model.Entity.Registry.Root);
			foreach (KeyValuePair<Guid, TreeItem> pair in _entityIdToTreeItem)
			{
				ModelEntity entity = Application.Model.Entity.Registry.Get(pair.Key);
				if (entity != null)
				{
					ModelEntityRowBehavior.RebuildButtons(pair.Value, entity, ModelEntityRowBehavior.ResolvePropertyCount(entity, counts));
				}
			}
		}

		if (Application.Model.Property.Selection.Count > 0)
		{
			QueueRebuildTree();
		}
	}

	#endregion

	#region Helpers

	/// <summary>
	/// 初期状態を SelectionService から同期する
	/// </summary>
	private void SyncInitialState()
	{
		UpdateModeButtons(Application.Model.Selection.CurrentMode);
		RebuildTree();
	}

	/// <summary>
	/// モード切替ボタンの押下状態を更新する
	/// </summary>
	/// <param name="mode">選択モード</param>
	private void UpdateModeButtons(ModelSelectionMode? mode)
	{
		_buttonSet.ButtonPressed = mode == ModelSelectionMode.Set;
		_buttonAdd.ButtonPressed = mode == ModelSelectionMode.Add;
		_buttonRemove.ButtonPressed = mode == ModelSelectionMode.Remove;
		_buttonToggle.ButtonPressed = mode == ModelSelectionMode.Toggle;
	}

	/// <summary>
	/// Tree の再構築を遅延キューへ積む
	/// </summary>
	private void QueueRebuildTree()
	{
		if (_isRebuildQueued)
		{
			return;
		}

		_isRebuildQueued = true;
		// Tree の選択シグナル処理中は、その Tree を変更できない。
		CallDeferred(MethodName.RebuildTreeDeferred);
	}

	/// <summary>
	/// 遅延呼び出しで Tree を再構築する
	/// </summary>
	private void RebuildTreeDeferred()
	{
		_isRebuildQueued = false;
		if (!IsInsideTree())
		{
			return;
		}

		RebuildTree();
	}

	/// <summary>
	/// 現在の選択モデル一覧で Tree を再構築する
	/// </summary>
	private void RebuildTree()
	{
		if (_tree == null || !GodotObject.IsInstanceValid(_tree)
			|| _selectionCountLabel == null || !GodotObject.IsInstanceValid(_selectionCountLabel))
		{
			return;
		}

		_isUpdatingTree = true;

		IReadOnlyCollection<Guid> selectedEntityIds = Application.Model.Entity.Selection.SelectedIds;
		IReadOnlyCollection<Guid> selectedPropertyIds = Application.Model.Property.Selection.SelectedIds;
		var selectedEntityIdSet = new HashSet<Guid>(selectedEntityIds);
		var selectedPropertyIdSet = new HashSet<Guid>(selectedPropertyIds);
		_selectionCountLabel.Text = $"Selected: {checked(selectedEntityIds.Count + selectedPropertyIds.Count)}";

		_tree.Clear();
		_entityIdToTreeItem.Clear();
		_propertyEntityIdToTreeItem.Clear();
		_propertyIdToTreeItem.Clear();
		TreeItem root = _tree.CreateItem();
		if (root != null)
		{
			Color selectedColor = ModelEntityRowBehavior.GetSelectionColor();
			Dictionary<Guid, int> propertySubtreeCounts = ModelEntityRowBehavior.BuildPropertySubtreeCounts(Application.Model.Entity.Registry.Root);
			foreach (ModelEntity modelEntity in GetEntitiesInCanonicalOrder())
			{
				Guid entityId = modelEntity.Id;
				if (!selectedEntityIdSet.Contains(entityId))
				{
					continue;
				}

				TreeItem item = _tree.CreateItem(root);
				item.SetText(0, modelEntity?.Name ?? entityId.ToString());
				ModelEntityRowBehavior.ApplySelectionHighlight(item, entityId, selectedColor);

				// ModelEntityに紐づくアイコンを表示する
				Texture2D icon = ModelEntityRowBehavior.GetIndentedIcon(modelEntity);
				item.SetIcon(0, icon);
				ModelEntityRowBehavior.RebuildButtons(item, modelEntity, ModelEntityRowBehavior.ResolvePropertyCount(modelEntity, propertySubtreeCounts));

				// 右クリックメニューはModelEntity単位で対象を扱う設計のため、TreeItemにEntityIdをmeta保持しておく
				item.SetMeta("EntityId", entityId.ToString());
				_entityIdToTreeItem.Add(entityId, item);
			}

			var entityItems = new Dictionary<Guid, TreeItem>();
			foreach (ModelEntity entity in GetEntitiesInCanonicalOrder())
			{
				bool hasSelectedProperty = HasSelectedProperty(entity, selectedPropertyIdSet);
				if (!hasSelectedProperty)
				{
					continue;
				}

				if (!entityItems.TryGetValue(entity.Id, out TreeItem entityItem))
				{
					entityItem = _tree.CreateItem(root);
					entityItem.SetText(0, entity.Name);
					entityItem.SetIcon(0, ModelEntityRowBehavior.GetIndentedIcon(entity));
					entityItems.Add(entity.Id, entityItem);
					_propertyEntityIdToTreeItem.Add(entity.Id, entityItem);
				}

				foreach (ModelProperty property in entity.Properties)
				{
					AddSelectedProperties(entityItem, property, selectedPropertyIdSet, selectedColor);
				}
			}
		}

		_isUpdatingTree = false;
	}

	/// <summary>Registryの親子順を保ったEntity一覧を深さ優先で取得する。</summary>
	private static List<ModelEntity> GetEntitiesInCanonicalOrder()
	{
		var entities = new List<ModelEntity>();
		var visited = new HashSet<Guid>();
		ModelEntity root = Application.Model.Entity.Registry.Root;
		if (root == null)
		{
			return entities;
		}

		foreach (ModelEntity child in root.Children)
		{
			CollectEntitiesInCanonicalOrder(child, entities, visited);
		}

		return entities;
	}

	/// <summary>Entity部分木を深さ優先で一覧へ追加する。</summary>
	private static void CollectEntitiesInCanonicalOrder(
		ModelEntity entity, List<ModelEntity> entities, HashSet<Guid> visited)
	{
		if (entity == null || !visited.Add(entity.Id))
		{
			return;
		}

		entities.Add(entity);
		foreach (ModelEntity child in entity.Children)
		{
			CollectEntitiesInCanonicalOrder(child, entities, visited);
		}
	}

	/// <summary>Entity部分木に選択中Propertyがあるかを判定する。</summary>
	private static bool HasSelectedProperty(ModelEntity entity, HashSet<Guid> selectedPropertyIds)
	{
		foreach (ModelProperty property in entity.Properties)
		{
			if (ContainsSelectedProperty(property, selectedPropertyIds, new HashSet<Guid>()))
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>Property部分木に選択中Propertyがあるかを判定する。</summary>
	private static bool ContainsSelectedProperty(
		ModelProperty property, HashSet<Guid> selectedPropertyIds, HashSet<Guid> visited)
	{
		if (property == null || !visited.Add(property.Id))
		{
			return false;
		}

		if (selectedPropertyIds.Contains(property.Id))
		{
			return true;
		}

		foreach (ModelProperty child in property.Children)
		{
			if (ContainsSelectedProperty(child, selectedPropertyIds, visited))
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>Property部分木の選択行を元の深さ優先順で追加する。</summary>
	private void AddSelectedProperties(
		TreeItem parentItem, ModelProperty property, HashSet<Guid> selectedPropertyIds, Color selectedColor)
	{
		AddSelectedPropertiesCore(parentItem, property, selectedPropertyIds, selectedColor, new HashSet<Guid>());
	}

	/// <summary>Property部分木を走査して選択行だけを追加する。</summary>
	private void AddSelectedPropertiesCore(
		TreeItem parentItem,
		ModelProperty property,
		HashSet<Guid> selectedPropertyIds,
		Color selectedColor,
		HashSet<Guid> visited)
	{
		if (property == null || !visited.Add(property.Id))
		{
			return;
		}

		if (selectedPropertyIds.Contains(property.Id))
		{
			TreeItem item = _tree.CreateItem(parentItem);
			ModelPropertyRowBehavior.Configure(item, property, true);
			ModelPropertyRowBehavior.ApplySelectionHighlight(item, property.Id, selectedColor);
			_propertyIdToTreeItem.Add(property.Id, item);
		}

		foreach (ModelProperty child in property.Children)
		{
			AddSelectedPropertiesCore(parentItem, child, selectedPropertyIds, selectedColor, visited);
		}
	}

	/// <summary>
	/// Property Tree上の2行の間にある表示中Property識別子を取得する。
	/// </summary>
	private Guid[] GetPropertyIdsInRange(Guid firstPropertyId, Guid lastPropertyId)
	{
		if (!_propertyIdToTreeItem.TryGetValue(firstPropertyId, out TreeItem first)
			|| !_propertyIdToTreeItem.TryGetValue(lastPropertyId, out TreeItem last))
		{
			return new[] { lastPropertyId };
		}

		var items = new List<TreeItem>();
		TreeItem root = _tree.GetRoot();
		if (root == null)
		{
			return new[] { lastPropertyId };
		}

		CollectVisiblePropertyItems(root, items);
		int firstIndex = items.IndexOf(first);
		int lastIndex = items.IndexOf(last);
		if (firstIndex < 0 || lastIndex < 0)
		{
			return new[] { lastPropertyId };
		}

		var ids = new List<Guid>();
		for (int i = Math.Min(firstIndex, lastIndex); i <= Math.Max(firstIndex, lastIndex); i++)
		{
			Guid propertyId = ModelPropertyRowBehavior.GetPropertyId(items[i]);
			if (propertyId != Guid.Empty)
			{
				ids.Add(propertyId);
			}
		}

		return ids.ToArray();
	}

	private void CollectVisiblePropertyItems(TreeItem item, List<TreeItem> items)
	{
		if (ModelPropertyRowBehavior.GetPropertyId(item) != Guid.Empty)
		{
			items.Add(item);
		}

		if (!item.Collapsed)
		{
			for (TreeItem child = item.GetFirstChild(); child != null; child = child.GetNext())
			{
				CollectVisiblePropertyItems(child, items);
			}
		}
	}

	/// <summary>
	/// TreeItem に紐づく ModelEntity の識別子を取得する
	/// </summary>
	/// <param name="item">対象の TreeItem</param>
	/// <returns>解決できた場合は ModelEntity の識別子、解決できない場合は <see cref="Guid.Empty"/></returns>
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

	#endregion
}
