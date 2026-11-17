using System;
using System.Collections.Generic;
using Godot;

/// <summary>
/// ModelEntity に紐づく ModelProperty の階層を表示する UI コンポーネント
/// </summary>
public partial class ModelPropertyTree : Tree
{
	#region Fields

	/// <summary>
	/// このツリーが表示対象としている ModelEntity の識別子
	/// </summary>
	private Guid _entityId = Guid.Empty;

	/// <summary>
	/// ModelEntity を表すルートの TreeItem、右クリック対象の判定に使う
	/// </summary>
	private TreeItem _rootItem;

	private bool _isNotifyingRootPick;

	/// <summary>
	/// 表示切替ボタンのID
	/// </summary>
	private const int VisibilityButtonId = ModelEntityRowBehavior.VisibilityButtonId;

	/// <summary>
	/// Property OverlayTree表示切替ボタンのID（Root行のみ）
	/// </summary>
	private const int PropertyButtonId = ModelEntityRowBehavior.PropertyButtonId;

	/// <summary>
	/// ModelProperty識別子と、対応する TreeItem の対応表
	/// </summary>
	private readonly System.Collections.Generic.Dictionary<Guid, TreeItem> _propertyItems = new();

	private TreeItem _lastSelectedPropertyItem;
	private readonly HashSet<TreeItem> _highlightedItems = new();

	#endregion

	#region Lifecycle

	public override void _Ready()
	{
		base._Ready();
		SubscribeEvents();
	}

	public override void _ExitTree()
	{
		UnsubscribeEvents();
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

			// Root(ModelEntity)行はModelEntityMenu、下階層(ModelProperty)行はModelPropertyMenuを表示する
			if (targetItem == _rootItem)
			{
				if (_entityId == Guid.Empty)
				{
					return;
				}

				ModelEntityRowBehavior.ShowContextMenu(_entityId);
			}
			else
			{
				ModelPropertyRowBehavior.ShowContextMenu(targetItem);
			}
		}
	}

	#endregion

	#region Methods

	/// <summary>
	/// 表示対象の ModelEntity 識別子を設定し、紐づく ModelProperty をツリーに表示する
	/// </summary>
	/// <param name="entityId">表示対象の ModelEntity の識別子</param>
	public void Show(Guid entityId)
	{
		// 自身のPickを受けた埋め込みツリーが、操作中の行を再生成しないようにする。
		if (_isNotifyingRootPick && entityId == _entityId)
		{
			return;
		}

		_entityId = entityId;
		Clear();
		_rootItem = null;
		_propertyItems.Clear();
		_highlightedItems.Clear();
		_lastSelectedPropertyItem = null;

		ModelEntity entity = Application.Model.Entity.Registry.Get(_entityId);
		if (entity == null)
		{
			return;
		}

		TreeItem rootItem = CreateItem();
		rootItem.SetText(0, entity.Name);
		Texture2D entityIcon = ModelEntityRowBehavior.GetModelIcon(entity.IconId);
		rootItem.SetIcon(0, entityIcon);
		rootItem.AddButton(0, GetEntityPropertyIcon(entity), id: PropertyButtonId, disabled: entity.Properties.Count == 0);
		rootItem.AddButton(0, GetEntityVisibilityIcon(entity), id: VisibilityButtonId);
		_rootItem = rootItem;

		// Entity直下のプロパティを起点に、プロパティ階層を再帰表示する。
		foreach (ModelProperty property in entity.Properties)
		{
			AddPropertyToTree(property, rootItem);
		}

		RefreshSelectionHighlights();
	}

	/// <summary>
	/// 現在表示中のEntityに紐づくPropertyを再表示する。
	/// </summary>
	public void Refresh()
	{
		Show(_entityId);
	}

	#endregion

	#region Events

	private void SubscribeEvents()
	{
		ButtonClicked += OnButtonClicked;
		CellSelected += OnItemSelected;
		ItemCollapsed += OnItemCollapsed;
		ItemActivated += OnItemActivated;
		Application.Model.Property.Visual.VisibilityResolved += OnPropertyVisibilityResolved;
		Application.Model.Property.State.ValueNotified += OnPropertyValueNotified;
		Application.Model.Entity.Visual.VisibilityResolved += OnEntityVisibilityResolved;
		Application.Model.Entity.State.PropertyTreeVisibleNotified += OnEntityPropertyTreeVisibleNotified;
		Application.Model.Entity.Selection.SelectionChanged += OnSelectionChanged;
		Application.Model.Property.Selection.SelectionChanged += OnSelectionChanged;
		Application.Model.Property.Selection.Cleared += OnPropertySelectionCleared;
		Application.Ui.Theme.ThemeChanged += OnThemeChanged;
	}

	private void UnsubscribeEvents()
	{
		ButtonClicked -= OnButtonClicked;
		CellSelected -= OnItemSelected;
		ItemCollapsed -= OnItemCollapsed;
		ItemActivated -= OnItemActivated;
		Application.Model.Property.Visual.VisibilityResolved -= OnPropertyVisibilityResolved;
		Application.Model.Property.State.ValueNotified -= OnPropertyValueNotified;
		Application.Model.Entity.Visual.VisibilityResolved -= OnEntityVisibilityResolved;
		Application.Model.Entity.State.PropertyTreeVisibleNotified -= OnEntityPropertyTreeVisibleNotified;
		Application.Model.Entity.Selection.SelectionChanged -= OnSelectionChanged;
		Application.Model.Property.Selection.SelectionChanged -= OnSelectionChanged;
		Application.Model.Property.Selection.Cleared -= OnPropertySelectionCleared;
		Application.Ui.Theme.ThemeChanged -= OnThemeChanged;
	}

	/// <summary>
	/// 行が選択されたとき、EntityのPickまたはPropertyの選択を処理する。
	/// </summary>
	private void OnItemSelected()
	{
		TreeItem item = GetSelected();
		if (_rootItem == null || _entityId == Guid.Empty || item == null)
		{
			return;
		}

		if (item == _rootItem)
		{
			_lastSelectedPropertyItem = null;
			_isNotifyingRootPick = true;
			try
			{
				Application.Model.Entity.Pick.NotifyResult(PickUtility.PickByEntityId(_entityId));
			}
			finally
			{
				_isNotifyingRootPick = false;
			}
			return;
		}

		Guid propertyId = ModelPropertyRowBehavior.GetPropertyId(item);
		if (propertyId == Guid.Empty)
		{
			Application.Log.Warn("ModelPropertyTree: selected item has no associated property.");
			return;
		}

		ModelSelectionMode mode = Application.Model.Selection.CurrentMode;
		Guid[] ids = (mode == ModelSelectionMode.Add || mode == ModelSelectionMode.Remove) && _lastSelectedPropertyItem != null
			? GetPropertyIdsInRange(_lastSelectedPropertyItem, item)
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
				Application.Log.Warn($"ModelPropertyTree: Unknown selection mode {mode}.");
				break;
		}

		_lastSelectedPropertyItem = item;
	}

	/// <summary>
	/// TreeItem のボタンがクリックされたとき、表示切替ボタンならPropertyの表示設定を切り替える
	/// </summary>
	private void OnButtonClicked(TreeItem item, long column, long id, long mouseButtonIndex)
	{
		if (item == null)
		{
			return;
		}

		if (item == _rootItem)
		{
			ModelEntityRowBehavior.HandleButtonClick(_entityId, id);
			return;
		}

		ModelPropertyRowBehavior.HandleButtonClick(ModelPropertyRowBehavior.GetPropertyId(item), id);
	}

	/// <summary>
	/// EntityまたはPropertyの選択状態が変更されたとき、背景色を更新する。
	/// </summary>
	private void OnSelectionChanged()
	{
		RefreshSelectionHighlights();
	}

	/// <summary>Property選択の解除後、範囲選択の起点を破棄する。</summary>
	private void OnPropertySelectionCleared()
	{
		_lastSelectedPropertyItem = null;
	}

	/// <summary>折り畳みに応じて選択背景を更新する。</summary>
	private void OnItemCollapsed(TreeItem item)
	{
		RefreshSelectionHighlights();
	}

	/// <summary>
	/// テーマ切替後、背景色とParticle以外のアイコンを更新する。
	/// </summary>
	private void OnThemeChanged()
	{
		RefreshSelectionHighlights();
		ModelEntity entity = Application.Model.Entity.Registry.Get(_entityId);
		if (_rootItem != null && entity != null)
		{
			ModelEntityRowBehavior.UpdateThemeIcons(_rootItem, entity, entity.PropertyCountRecursive, false);
		}

		foreach (System.Collections.Generic.KeyValuePair<Guid, TreeItem> pair in _propertyItems)
		{
			ModelProperty property = Application.Model.Property.Registry.Get(pair.Key);
			if (property == null)
			{
				continue;
			}

			ModelPropertyRowBehavior.UpdateThemeIcons(pair.Value, property);
		}
	}

	/// <summary>
	/// 現在の選択と折り畳み状態から各行の背景色を更新する。
	/// </summary>
	private void RefreshSelectionHighlights()
	{
		if (_rootItem == null)
		{
			return;
		}

		var next = new HashSet<TreeItem>();
		if (Application.Model.Entity.Selection.Contains(_entityId))
		{
			next.Add(_rootItem);
		}

		foreach (Guid propertyId in Application.Model.Property.Selection.SelectedIds)
		{
			if (!_propertyItems.TryGetValue(propertyId, out TreeItem item))
			{
				continue;
			}

			TreeItem visible = item;
			for (TreeItem ancestor = item.GetParent(); ancestor != null; ancestor = ancestor.GetParent())
			{
				if (ancestor.Collapsed)
				{
					visible = ancestor;
				}
			}
			next.Add(visible);
		}

		foreach (TreeItem item in _highlightedItems)
		{
			if (!next.Contains(item))
			{
				item.ClearCustomBgColor(0);
			}
		}

		Color color = ModelEntityRowBehavior.GetSelectionColor();
		foreach (TreeItem item in next)
		{
			item.SetCustomBgColor(0, color);
		}
		_highlightedItems.Clear();
		_highlightedItems.UnionWith(next);
	}

	/// <summary>
	/// ルートの ModelEntity 行がアクティブ化されたとき、Manipulate 操作を通知する。
	/// </summary>
	private void OnItemActivated()
	{
		TreeItem selectedItem = GetSelected();
		if (_rootItem != null && _entityId != Guid.Empty && selectedItem == _rootItem)
		{
			ModelEntityRowBehavior.Manipulate(_entityId);
			return;
		}

		Guid propertyId = ModelPropertyRowBehavior.GetPropertyId(selectedItem);
		if (propertyId != Guid.Empty)
		{
			ModelPropertyRowBehavior.Manipulate(propertyId);
		}
	}

	/// <summary>
	/// Propertyの実効表示が解決されたとき、対応する行の表示ボタンを更新する
	/// </summary>
	/// <param name="propertyId">解決された ModelProperty の識別子</param>
	private void OnPropertyVisibilityResolved(string propertyId)
	{
		if (!Guid.TryParse(propertyId, out Guid parsedId) || !_propertyItems.TryGetValue(parsedId, out TreeItem item))
		{
			return;
		}

		ModelProperty property = Application.Model.Property.Registry.Get(parsedId);
		if (property != null)
		{
			ModelPropertyRowBehavior.UpdateVisibilityButton(item, property);
		}
	}

	/// <summary>
	/// Propertyの値が変更されたとき、対応する行の表示文字列と値メタデータを更新する
	/// </summary>
	/// <param name="propertyId">値が変更された ModelProperty の識別子</param>
	private void OnPropertyValueNotified(string propertyId)
	{
		if (!Guid.TryParse(propertyId, out Guid parsedId) || !_propertyItems.TryGetValue(parsedId, out TreeItem item))
		{
			return;
		}

		ModelProperty property = Application.Model.Property.Registry.Get(parsedId);
		if (property != null)
		{
			ModelPropertyRowBehavior.UpdateText(item, property);
		}
	}

	/// <summary>
	/// Entityの実効表示が解決されたとき、表示中Entityのルート行の表示ボタンを更新する
	/// </summary>
	/// <param name="entityId">解決された ModelEntity の識別子</param>
	private void OnEntityVisibilityResolved(string entityId)
	{
		if (_rootItem == null || !Guid.TryParse(entityId, out Guid parsedId) || parsedId != _entityId)
		{
			return;
		}

		ModelEntity entity = Application.Model.Entity.Registry.Get(parsedId);
		if (entity != null)
		{
			int index = _rootItem.GetButtonById(0, VisibilityButtonId);
			if (index >= 0)
			{
				_rootItem.SetButton(0, index, GetEntityVisibilityIcon(entity));
			}
		}
	}

	/// <summary>
	/// Property OverlayTreeの表示状態が通知されたとき、Root行のPropertyボタンを更新する
	/// </summary>
	/// <param name="entityId">表示状態が変更された ModelEntity の識別子</param>
	private void OnEntityPropertyTreeVisibleNotified(string entityId)
	{
		if (_rootItem == null || !Guid.TryParse(entityId, out Guid parsedId) || parsedId != _entityId)
		{
			return;
		}

		ModelEntity entity = Application.Model.Entity.Registry.Get(parsedId);
		int index = _rootItem.GetButtonById(0, PropertyButtonId);
		if (entity != null && index >= 0)
		{
			_rootItem.SetButton(0, index, GetEntityPropertyIcon(entity));
		}
	}

	#endregion

	#region Helpers

	/// <summary>表示順で2行の間にあるProperty識別子を取得する。</summary>
	private Guid[] GetPropertyIdsInRange(TreeItem first, TreeItem last)
	{
		var visible = new List<TreeItem>();
		CollectVisiblePropertyItems(_rootItem, visible);
		int firstIndex = visible.IndexOf(first);
		int lastIndex = visible.IndexOf(last);
		if (firstIndex < 0 || lastIndex < 0)
		{
			Guid propertyId = ModelPropertyRowBehavior.GetPropertyId(last);
			return propertyId != Guid.Empty
				? new[] { propertyId }
				: Array.Empty<Guid>();
		}

		var ids = new List<Guid>();
		for (int i = Math.Min(firstIndex, lastIndex); i <= Math.Max(firstIndex, lastIndex); i++)
		{
			Guid propertyId = ModelPropertyRowBehavior.GetPropertyId(visible[i]);
			if (propertyId != Guid.Empty)
			{
				ids.Add(propertyId);
			}
		}
		return ids.ToArray();
	}

	/// <summary>表示中のProperty行を階層順に収集する。</summary>
	private void CollectVisiblePropertyItems(TreeItem item, List<TreeItem> items)
	{
		if (item != _rootItem)
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
	/// Entityの表示設定と実効表示に対応するボタンアイコンを取得する
	/// </summary>
	private static Texture2D GetEntityVisibilityIcon(ModelEntity entity)
	{
		return ModelEntityRowBehavior.GetVisibilityIcon(entity.Visibility, entity.IsVisible);
	}

	/// <summary>
	/// EntityのProperty有無とOverlayTree表示状態に対応するボタンアイコンを取得する
	/// </summary>
	private static Texture2D GetEntityPropertyIcon(ModelEntity entity)
	{
		return ModelEntityRowBehavior.GetPropertyIcon(entity.PropertyCountRecursive, entity.IsPropertyTreeVisible);
	}

	/// <summary>
	/// ModelProperty とその子プロパティをツリーへ追加する
	/// </summary>
	/// <param name="property">追加対象の ModelProperty</param>
	/// <param name="parentItem">追加先の TreeItem</param>
	private void AddPropertyToTree(ModelProperty property, TreeItem parentItem)
	{
		if (property == null || parentItem == null)
		{
			return;
		}

		TreeItem propertyItem = parentItem.CreateChild();
		ModelPropertyRowBehavior.Configure(propertyItem, property);

		_propertyItems[property.Id] = propertyItem;

		foreach (ModelProperty childProperty in property.Children)
		{
			AddPropertyToTree(childProperty, propertyItem);
		}
	}

	#endregion
}
