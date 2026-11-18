using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// Entityと所属Propertyの検索条件と結果を表示するパネル。
/// </summary>
public partial class SearchUi : PanelContainer
{
	#region Fields

	private OptionButton _entityType = null!;
	private LineEdit _entityName = null!;
	private OptionButton _propertyType = null!;
	private OptionButton _propertyValueType = null!;
	private LineEdit _propertyValue = null!;
	private Button _entityResultCount = null!;
	private Button _propertyResultCount = null!;
	private Tree _results = null!;
	private Button _selectAllEntities = null!;
	private Button _selectAllProperties = null!;
	private UiWindow _window;
	private bool _restoreWindowUnfocusable;
	private readonly List<Guid> _entityResultIds = new();
	private readonly List<Guid> _propertyResultIds = new();
	private readonly Dictionary<Guid, TreeItem> _entityResultItems = new();
	private readonly Dictionary<Guid, TreeItem> _propertyResultItems = new();
	private TreeItem _lastSelectedEntityItem;
	private TreeItem _lastSelectedPropertyItem;

	#endregion

	#region Lifecycle

	public override void _Ready()
	{
		_entityType = GetNode<OptionButton>("MarginContainer/VBoxContainer/Filters/EntityType");
		_entityName = GetNode<LineEdit>("MarginContainer/VBoxContainer/Filters/EntityName");
		_propertyType = GetNode<OptionButton>("MarginContainer/VBoxContainer/Filters/PropertyType");
		_propertyValueType = GetNode<OptionButton>("MarginContainer/VBoxContainer/Filters/PropertyValueType");
		_propertyValue = GetNode<LineEdit>("MarginContainer/VBoxContainer/Filters/PropertyValue");
		_entityResultCount = GetNode<Button>("MarginContainer/VBoxContainer/EntityResultCount");
		_propertyResultCount = GetNode<Button>("MarginContainer/VBoxContainer/PropertyResultCount");
		_results = GetNode<Tree>("MarginContainer/VBoxContainer/Results");
		_selectAllEntities = GetNode<Button>("MarginContainer/VBoxContainer/SelectAll/SelectAllEntities");
		_selectAllProperties = GetNode<Button>("MarginContainer/VBoxContainer/SelectAll/SelectAllProperties");
		_window = GetWindow() as UiWindow;
		SubscribeEvents();
		RefreshTypesAndResults();
	}

	public override void _ExitTree()
	{
		UnsubscribeEvents();
		base._ExitTree();
	}

	#endregion

	#region Events

	private void SubscribeEvents()
	{
		_entityType.ItemSelected += OnTypeSelected;
		_propertyType.ItemSelected += OnTypeSelected;
		_propertyValueType.ItemSelected += OnTypeSelected;
		_entityName.TextChanged += OnTextChanged;
		_propertyValue.TextChanged += OnTextChanged;
		_entityName.GuiInput += OnTextInputGuiInput;
		_propertyValue.GuiInput += OnTextInputGuiInput;
		if (_window != null)
		{
			_window.FocusExited += OnWindowFocusExited;
		}

		_entityResultCount.Pressed += OnVisibilityFilterPressed;
		_propertyResultCount.Pressed += OnVisibilityFilterPressed;
		_results.ItemSelected += OnResultSelected;
		_results.ItemActivated += OnResultActivated;
		_results.ButtonClicked += OnResultButtonClicked;
		_results.GuiInput += OnResultsGuiInput;
		_selectAllEntities.Pressed += OnSelectAllEntities;
		_selectAllProperties.Pressed += OnSelectAllProperties;
		Application.Model.Entity.Registry.Registered += OnRegistryChanged;
		Application.Model.Entity.Registry.Cleared += OnRegistryChanged;
		Application.Model.Property.Registry.Changed += OnRegistryChanged;
		Application.Model.Property.State.ValueNotified += OnPropertyValueChanged;
		Application.Model.Property.Visual.VisibilityResolved += OnPropertyVisibilityResolved;
		Application.Model.Entity.Selection.SelectionChanged += OnSelectionChanged;
		Application.Model.Entity.Selection.Cleared += OnSelectionCleared;
		Application.Model.Property.Selection.SelectionChanged += OnPropertySelectionChanged;
		Application.Model.Property.Selection.Cleared += OnPropertySelectionCleared;
		Application.Model.Entity.Visual.VisibilityResolved += OnEntityVisibilityResolved;
		Application.Model.Entity.State.PropertyTreeVisibleNotified += OnEntityButtonsChanged;
		Application.Model.Entity.State.Collapsed += OnEntityButtonsChanged;
		Application.Ui.Theme.ThemeChanged += OnThemeChanged;
	}

	private void UnsubscribeEvents()
	{
		_entityType.ItemSelected -= OnTypeSelected;
		_propertyType.ItemSelected -= OnTypeSelected;
		_propertyValueType.ItemSelected -= OnTypeSelected;
		_entityName.TextChanged -= OnTextChanged;
		_propertyValue.TextChanged -= OnTextChanged;
		_entityName.GuiInput -= OnTextInputGuiInput;
		_propertyValue.GuiInput -= OnTextInputGuiInput;
		if (_window != null)
		{
			_window.FocusExited -= OnWindowFocusExited;
			OnWindowFocusExited();
		}

		_entityResultCount.Pressed -= OnVisibilityFilterPressed;
		_propertyResultCount.Pressed -= OnVisibilityFilterPressed;
		_results.ItemSelected -= OnResultSelected;
		_results.ItemActivated -= OnResultActivated;
		_results.ButtonClicked -= OnResultButtonClicked;
		_results.GuiInput -= OnResultsGuiInput;
		_selectAllEntities.Pressed -= OnSelectAllEntities;
		_selectAllProperties.Pressed -= OnSelectAllProperties;
		Application.Model.Entity.Registry.Registered -= OnRegistryChanged;
		Application.Model.Entity.Registry.Cleared -= OnRegistryChanged;
		Application.Model.Property.Registry.Changed -= OnRegistryChanged;
		Application.Model.Property.State.ValueNotified -= OnPropertyValueChanged;
		Application.Model.Property.Visual.VisibilityResolved -= OnPropertyVisibilityResolved;
		Application.Model.Entity.Selection.SelectionChanged -= OnSelectionChanged;
		Application.Model.Entity.Selection.Cleared -= OnSelectionCleared;
		Application.Model.Property.Selection.SelectionChanged -= OnPropertySelectionChanged;
		Application.Model.Property.Selection.Cleared -= OnPropertySelectionCleared;
		Application.Model.Entity.Visual.VisibilityResolved -= OnEntityVisibilityResolved;
		Application.Model.Entity.State.PropertyTreeVisibleNotified -= OnEntityButtonsChanged;
		Application.Model.Entity.State.Collapsed -= OnEntityButtonsChanged;
		Application.Ui.Theme.ThemeChanged -= OnThemeChanged;
	}

	private void OnTypeSelected(long index) => RefreshResults();

	private void OnTextChanged(string text) => RefreshResults();

	private void OnVisibilityFilterPressed() => RefreshResults();

	private void OnTextInputGuiInput(InputEvent @event)
	{
		if (_window != null && _window.Unfocusable
			&& @event is InputEventMouseButton mouseButton && mouseButton.Pressed
			&& (mouseButton.ButtonIndex == MouseButton.Left || mouseButton.ButtonIndex == MouseButton.Right))
		{
			_restoreWindowUnfocusable = true;
			_window.Unfocusable = false;
			_window.GrabFocus();
		}
	}

	private void OnWindowFocusExited()
	{
		if (_restoreWindowUnfocusable)
		{
			_restoreWindowUnfocusable = false;
			_window.Unfocusable = true;
		}
	}

	private void OnRegistryChanged() => RefreshTypesAndResults();

	private void OnPropertyValueChanged(string propertyId) => RefreshResults();

	private void OnSelectionChanged() => UpdateSelectionIndicators();

	private void OnThemeChanged()
	{
		Dictionary<Guid, int> counts = ModelEntityRowBehavior.BuildPropertySubtreeCounts(Application.Model.Entity.Registry.Root);
		foreach (KeyValuePair<Guid, TreeItem> pair in _entityResultItems)
		{
			ModelEntity entity = Application.Model.Entity.Registry.Get(pair.Key);
			if (entity != null)
			{
				ModelEntityRowBehavior.UpdateThemeIcons(
					pair.Value,
					entity,
					counts.GetValueOrDefault(entity.Id, entity.PropertyCountRecursive),
					true);
			}
		}

		foreach (KeyValuePair<Guid, TreeItem> pair in _propertyResultItems)
		{
			ModelProperty property = Application.Model.Property.Registry.Get(pair.Key);
			if (property != null)
			{
				ModelEntity entity = Application.Model.Property.Registry.GetOwningEntity(property.Id);
				int ownerDepth = ModelEntityRowBehavior.GetHierarchyDepth(entity);
				pair.Value.SetIcon(0, ModelPropertyRowBehavior.GetIndentedIcon(property, checked(ownerDepth + 1)));
				ModelPropertyRowBehavior.UpdateVisibilityButton(pair.Value, property);
			}
		}

		UpdateSelectionIndicators();
		UpdatePropertySelectionIndicators();
	}

	private void OnSelectionCleared() => _lastSelectedEntityItem = null;

	private void OnPropertySelectionChanged() => UpdatePropertySelectionIndicators();

	private void OnPropertySelectionCleared()
	{
		_lastSelectedPropertyItem = null;
		UpdatePropertySelectionIndicators();
	}

	private void OnPropertyVisibilityResolved(string propertyId)
	{
		if (Guid.TryParse(propertyId, out Guid id)
			&& _propertyResultItems.TryGetValue(id, out TreeItem item))
		{
			ModelProperty property = Application.Model.Property.Registry.Get(id);
			if (property != null)
			{
				ModelPropertyRowBehavior.UpdateVisibilityButton(item, property);
			}
		}
	}

	private void OnResultSelected()
	{
		TreeItem item = _results.GetSelected();
		if (ModelPropertyRowBehavior.GetPropertyId(item) != Guid.Empty)
		{
			SelectPropertyResult(item);
			return;
		}

		if (!TryGetEntityId(item, out Guid entityId))
		{
			return;
		}

		ModelSelectionMode mode = Application.Model.Selection.CurrentMode;
		if ((mode == ModelSelectionMode.Add || mode == ModelSelectionMode.Remove)
			&& _lastSelectedEntityItem != null)
		{
			Application.Model.Entity.Pick.NotifyResults(
				PickUtility.PickByEntityIds(GetEntityIdsInRange(_lastSelectedEntityItem, item)));
		}
		else
		{
			Application.Model.Entity.Pick.NotifyResult(PickUtility.PickByEntityId(entityId));
		}

		_lastSelectedEntityItem = item;
	}

	private void SelectPropertyResult(TreeItem item)
	{
		Guid propertyId = ModelPropertyRowBehavior.GetPropertyId(item);
		if (propertyId == Guid.Empty)
		{
			return;
		}

		ModelSelectionMode mode = Application.Model.Selection.CurrentMode;
		Guid[] ids = (mode == ModelSelectionMode.Add || mode == ModelSelectionMode.Remove)
			&& _lastSelectedPropertyItem != null
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
				Application.Log.Warn($"SearchUi: Unknown selection mode {mode}.");
				break;
		}

		_lastSelectedPropertyItem = item;
	}

	private void OnResultActivated()
	{
		TreeItem item = _results.GetSelected();
		Guid propertyId = ModelPropertyRowBehavior.GetPropertyId(item);
		if (propertyId != Guid.Empty)
		{
			ModelPropertyRowBehavior.Manipulate(propertyId);
		}
		else if (TryGetEntityId(item, out Guid entityId))
		{
			ModelEntityRowBehavior.Manipulate(entityId);
		}
	}

	private void OnResultButtonClicked(TreeItem item, long column, long buttonId, long mouseButtonIndex)
	{
		if (item == null || column != 0)
		{
			return;
		}

		Guid propertyId = ModelPropertyRowBehavior.GetPropertyId(item);
		if (propertyId != Guid.Empty)
		{
			ModelPropertyRowBehavior.HandleButtonClick(propertyId, buttonId);
		}
		else if (TryGetEntityId(item, out Guid entityId))
		{
			ModelEntityRowBehavior.HandleButtonClick(entityId, buttonId);
		}
	}

	private void OnResultsGuiInput(InputEvent @event)
	{
		if (@event is not InputEventMouseButton mouseButton
			|| mouseButton.ButtonIndex != MouseButton.Right
			|| !mouseButton.Pressed)
		{
			return;
		}

		TreeItem item = _results.GetItemAtPosition(mouseButton.Position);
		if (ModelPropertyRowBehavior.GetPropertyId(item) != Guid.Empty)
		{
			ModelPropertyRowBehavior.ShowContextMenu(item);
		}
		else if (TryGetEntityId(item, out Guid entityId))
		{
			ModelEntityRowBehavior.ShowContextMenu(entityId);
		}
	}

	private void OnSelectAllEntities()
	{
		Application.Model.Entity.Selection.Set(_entityResultIds.ToArray());
	}

	private void OnSelectAllProperties()
	{
		Application.Model.Property.Selection.Set(_propertyResultIds.ToArray());
	}

	private void OnEntityVisibilityResolved(string entityId)
	{
		if (Guid.TryParse(entityId, out Guid id) && _entityResultItems.TryGetValue(id, out TreeItem item))
		{
			ModelEntity entity = Application.Model.Entity.Registry.Get(id);
			if (entity != null)
			{
				ModelEntityRowBehavior.UpdateVisibilityButton(item, entity);
			}
		}
	}

	private void OnEntityButtonsChanged(string entityId)
	{
		if (Guid.TryParse(entityId, out Guid id) && _entityResultItems.TryGetValue(id, out TreeItem item))
		{
			ModelEntity entity = Application.Model.Entity.Registry.Get(id);
			if (entity != null)
			{
				Dictionary<Guid, int> counts = ModelEntityRowBehavior.BuildPropertySubtreeCounts(entity);
				ModelEntityRowBehavior.RebuildButtons(item, entity, counts.GetValueOrDefault(entity.Id, entity.PropertyCountRecursive));
			}
		}
	}

	#endregion

	#region Helpers

	private void RefreshTypesAndResults()
	{
		PopulateTypes(_entityType, Application.Model.Search.GetEntityTypes());
		PopulateTypes(_propertyType, Application.Model.Search.GetPropertyTypes());
		PopulateTypes(_propertyValueType, Application.Model.Search.GetPropertyValueTypes());
		RefreshResults();
	}

	private static void PopulateTypes(OptionButton option, IReadOnlyList<string> types)
	{
		string previous = GetSelectedType(option);
		option.Clear();
		option.AddItem("All");
		int selected = 0;
		foreach (string type in types)
		{
			option.AddItem(type);
			if (string.Equals(type, previous, StringComparison.Ordinal))
			{
				selected = option.ItemCount - 1;
			}
		}

		option.Select(selected);
	}

	private static string GetSelectedType(OptionButton option)
	{
		return option.Selected > 0 ? option.GetItemText(option.Selected) : string.Empty;
	}

	private void RefreshResults()
	{
		string entityType = GetSelectedType(_entityType);
		string entityName = _entityName.Text;
		string propertyType = GetSelectedType(_propertyType);
		string propertyValueType = GetSelectedType(_propertyValueType);
		string propertyValue = _propertyValue.Text;
		IReadOnlyList<Guid> entityIds = Application.Model.Search.Find(
			entityType, entityName, propertyType, propertyValueType, propertyValue);
		IReadOnlyList<Guid> propertyIds = Application.Model.Search.FindProperties(
			entityType, entityName, propertyType, propertyValueType, propertyValue);

		_lastSelectedEntityItem = null;
		_lastSelectedPropertyItem = null;
		_results.Clear();
		_entityResultIds.Clear();
		_propertyResultIds.Clear();
		_entityResultItems.Clear();
		_propertyResultItems.Clear();
		_entityResultIds.AddRange(entityIds);
		_propertyResultIds.AddRange(propertyIds);

		TreeItem root = _results.CreateItem();
		var matchingEntityIds = new HashSet<Guid>(entityIds);
		var matchingPropertyIds = new HashSet<Guid>(propertyIds);
		Dictionary<Guid, int> propertySubtreeCounts =
			ModelEntityRowBehavior.BuildPropertySubtreeCounts(Application.Model.Entity.Registry.Root);

		foreach (ModelEntity entity in Application.Model.Entity.Registry.Root.Children)
		{
			AddEntityResults(entity, root, matchingEntityIds, matchingPropertyIds, propertySubtreeCounts);
		}

		_entityResultCount.Text = $"Entity | {_entityResultIds.Count}";
		_propertyResultCount.Text = $"Property | {_propertyResultIds.Count}";
		_selectAllEntities.Disabled = _entityResultIds.Count == 0;
		_selectAllProperties.Disabled = _propertyResultIds.Count == 0;
		UpdateSelectionIndicators();
		UpdatePropertySelectionIndicators();
	}

	private void AddEntityResults(
		ModelEntity entity,
		TreeItem visibleParent,
		HashSet<Guid> matchingEntityIds,
		HashSet<Guid> matchingPropertyIds,
		IReadOnlyDictionary<Guid, int> propertySubtreeCounts)
	{
		bool entityVisible = _entityResultCount.ButtonPressed && matchingEntityIds.Contains(entity.Id);
		if (entityVisible)
		{
			TreeItem entityItem = _results.CreateItem(visibleParent);
			entityItem.SetText(0, entity.Name);
			entityItem.SetIcon(0, ModelEntityRowBehavior.GetIndentedIcon(entity));
			entityItem.SetMetadata(0, entity.Id.ToString());
			ModelEntityRowBehavior.RebuildButtons(
				entityItem,
				entity,
				propertySubtreeCounts.GetValueOrDefault(entity.Id, entity.PropertyCountRecursive));
			_entityResultItems.Add(entity.Id, entityItem);
		}

		if (_propertyResultCount.ButtonPressed && matchingEntityIds.Contains(entity.Id))
		{
			foreach (ModelProperty property in entity.Properties)
			{
				AddPropertyResults(property, visibleParent, matchingPropertyIds);
			}
		}

		foreach (ModelEntity child in entity.Children)
		{
			AddEntityResults(
				child,
				visibleParent,
				matchingEntityIds,
				matchingPropertyIds,
				propertySubtreeCounts);
		}
	}

	private void AddPropertyResults(
		ModelProperty property,
		TreeItem visibleParent,
		HashSet<Guid> matchingPropertyIds)
	{
		if (matchingPropertyIds.Contains(property.Id))
		{
			TreeItem item = _results.CreateItem(visibleParent);
			ModelPropertyRowBehavior.Configure(item, property);
			ModelEntity entity = Application.Model.Property.Registry.GetOwningEntity(property.Id);
			int ownerDepth = ModelEntityRowBehavior.GetHierarchyDepth(entity);
			item.SetIcon(0, ModelPropertyRowBehavior.GetIndentedIcon(property, checked(ownerDepth + 1)));
			_propertyResultItems.Add(property.Id, item);
		}

		foreach (ModelProperty child in property.Children)
		{
			AddPropertyResults(child, visibleParent, matchingPropertyIds);
		}
	}

	private void UpdateSelectionIndicators()
	{
		Color selectedColor = ModelEntityRowBehavior.GetSelectionColor();
		foreach (KeyValuePair<Guid, TreeItem> pair in _entityResultItems)
		{
			ModelEntityRowBehavior.ApplySelectionHighlight(pair.Value, pair.Key, selectedColor);
		}
	}

	private void UpdatePropertySelectionIndicators()
	{
		Color selectedColor = ModelEntityRowBehavior.GetSelectionColor();
		foreach (KeyValuePair<Guid, TreeItem> pair in _propertyResultItems)
		{
			ModelPropertyRowBehavior.ApplySelectionHighlight(pair.Value, pair.Key, selectedColor);
		}
	}

	private Guid[] GetEntityIdsInRange(TreeItem first, TreeItem last)
	{
		var items = new List<TreeItem>();
		CollectItems(_results.GetRoot()?.GetFirstChild(), items, item => TryGetEntityId(item, out _));
		return GetIdsInRange(items, first, last, item => TryGetEntityId(item, out Guid id) ? id : Guid.Empty);
	}

	private Guid[] GetPropertyIdsInRange(TreeItem first, TreeItem last)
	{
		var items = new List<TreeItem>();
		CollectItems(
			_results.GetRoot()?.GetFirstChild(),
			items,
			item => ModelPropertyRowBehavior.GetPropertyId(item) != Guid.Empty);
		return GetIdsInRange(items, first, last, ModelPropertyRowBehavior.GetPropertyId);
	}

	private static Guid[] GetIdsInRange(
		List<TreeItem> items,
		TreeItem first,
		TreeItem last,
		Func<TreeItem, Guid> getId)
	{
		int firstIndex = items.IndexOf(first);
		int lastIndex = items.IndexOf(last);
		if (firstIndex < 0 || lastIndex < 0)
		{
			Guid id = getId(last);
			return id != Guid.Empty ? new[] { id } : Array.Empty<Guid>();
		}

		var ids = new List<Guid>();
		for (int i = Math.Min(firstIndex, lastIndex); i <= Math.Max(firstIndex, lastIndex); i++)
		{
			Guid id = getId(items[i]);
			if (id != Guid.Empty)
			{
				ids.Add(id);
			}
		}

		return ids.ToArray();
	}

	private static void CollectItems(TreeItem item, List<TreeItem> items, Func<TreeItem, bool> predicate)
	{
		for (TreeItem current = item; current != null; current = current.GetNext())
		{
			if (predicate(current))
			{
				items.Add(current);
			}
		}
	}

	private static bool TryGetEntityId(TreeItem item, out Guid entityId)
	{
		entityId = Guid.Empty;
		return item != null
			&& Guid.TryParse(item.GetMetadata(0).AsString(), out entityId)
			&& entityId != Guid.Empty;
	}

	#endregion
}
