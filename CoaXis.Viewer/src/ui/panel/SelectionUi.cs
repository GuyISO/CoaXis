using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// モデル選択状態表示と操作用のパネル
/// </summary>
public partial class SelectionUi : PanelContainer
{
	#region Fields

	private readonly List<Guid> _selectedEntityIds = new();
	private readonly Dictionary<Guid, TreeItem> _entityIdToTreeItem = new();
	private bool _isUpdatingTree = false;
	private bool _isRebuildQueued = false;

	// 関連ノードのキャッシュ
	private Tree _tree = null!;
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
		Application.Model.Entity.Selection.Selected += OnSelected;
		Application.Model.Entity.Registry.Registered += OnEntityRegistryRegistered;
		Application.Model.Entity.Visual.VisibilityResolved += OnEntityVisibilityResolved;
		Application.Model.Entity.State.PropertyTreeVisibleNotified += OnEntityButtonsChanged;
		Application.Model.Entity.State.Collapsed += OnEntityButtonsChanged;
		Application.Model.Property.Registry.Changed += OnPropertyRegistryChanged;
		Application.Ui.Theme.ThemeChanged += OnThemeChanged;
	}

	/// <summary>
	/// Applicationイベントの購読を解除する
	/// </summary>
	private void UnsubscribeApplicationEvents()
	{
		Application.Model.Entity.Pick.HandlingModeNotified -= OnPickHandlingModeNotified;
		Application.Model.Selection.ModeNotified -= OnModeNotified;
		Application.Model.Entity.Selection.Selected -= OnSelected;
		Application.Model.Entity.Registry.Registered -= OnEntityRegistryRegistered;
		Application.Model.Entity.Visual.VisibilityResolved -= OnEntityVisibilityResolved;
		Application.Model.Entity.State.PropertyTreeVisibleNotified -= OnEntityButtonsChanged;
		Application.Model.Entity.State.Collapsed -= OnEntityButtonsChanged;
		Application.Model.Property.Registry.Changed -= OnPropertyRegistryChanged;
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
		ModelEntityRowBehavior.Manipulate(TryGetEntityId(_tree.GetSelected()));
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

		ModelEntityRowBehavior.HandleButtonClick(TryGetEntityId(item), buttonId);
	}

	/// <summary>
	/// Tree の項目が選択されたとき、対応する ModelEntity の Pick を通知する
	/// </summary>
	private void OnTreeItemSelected()
	{
		if (_isUpdatingTree)
		{
			return;
		}

		Guid entityId = TryGetEntityId(_tree.GetSelected());
		if (entityId == Guid.Empty)
		{
			return;
		}

		Application.Model.Entity.Pick.NotifyResult(PickUtility.PickByEntityId(entityId));
	}

	/// <summary>
	/// 選択モード通知を受け取ったときに呼び出されるイベントハンドラ
	/// </summary>
	private void OnModeNotified()
	{
		UpdateModeButtons(Application.Model.Selection.CurrentMode);
	}

	/// <summary>
	/// モデル選択状態通知を受け取ったときに呼び出されるイベントハンドラ
	/// </summary>
	/// <param name="entityId">選択状態が変化した ModelEntity の識別子</param>
	private void OnSelected(string entityId)
	{
		if (!Guid.TryParse(entityId, out Guid parsedEntityId) || parsedEntityId == Guid.Empty)
		{
			return;
		}

		int index = _selectedEntityIds.IndexOf(parsedEntityId);
		if (Application.Model.Entity.Selection.Contains(parsedEntityId))
		{
			if (index < 0)
			{
				_selectedEntityIds.Add(parsedEntityId);
			}
		}
		else if (index >= 0)
		{
			_selectedEntityIds.RemoveAt(index);
		}

		QueueRebuildTree();
	}

	/// <summary>
	/// Entity集合の更新後に一覧を再構築する。
	/// </summary>
	private void OnEntityRegistryRegistered()
	{
		if (_selectedEntityIds.Count > 0)
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
		if (_entityIdToTreeItem.Count == 0)
		{
			return;
		}

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

	#endregion

	#region Helpers

	/// <summary>
	/// 初期状態を SelectionService から同期する
	/// </summary>
	private void SyncInitialState()
	{
		UpdateModeButtons(Application.Model.Selection.CurrentMode);

		_selectedEntityIds.Clear();
		IReadOnlyCollection<Guid> selectedEntityIds = Application.Model.Entity.Selection.SelectedIds;
		if (selectedEntityIds != null && selectedEntityIds.Count > 0)
		{
			_selectedEntityIds.AddRange(selectedEntityIds);
		}

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
		if (_tree == null || !GodotObject.IsInstanceValid(_tree))
		{
			return;
		}

		_isUpdatingTree = true;

		_selectedEntityIds.RemoveAll(entityId => entityId == Guid.Empty || Application.Model.Entity.Registry.Get(entityId) == null);

		_tree.Clear();
		_entityIdToTreeItem.Clear();
		TreeItem root = _tree.CreateItem();
		if (root == null)
		{
			_isUpdatingTree = false;
			return;
		}

		if (_selectedEntityIds.Count == 0)
		{
			_isUpdatingTree = false;
			return;
		}

		Color selectedColor = ModelEntityRowBehavior.GetSelectionColor();
		Dictionary<Guid, int> propertySubtreeCounts = ModelEntityRowBehavior.BuildPropertySubtreeCounts(Application.Model.Entity.Registry.Root);
		for (int i = 0; i < _selectedEntityIds.Count; i++)
		{
			Guid entityId = _selectedEntityIds[i];
			ModelEntity modelEntity = Application.Model.Entity.Registry.Get(entityId);
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

		_isUpdatingTree = false;
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
