using System;
using System.Collections.Generic;
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
    private Label _resultCount = null!;
    private Tree _results = null!;
    private Tree _propertyResults = null!;
    private Button _selectAll = null!;
    private UiWindow _window;
    private bool _restoreWindowUnfocusable;
    private readonly List<Guid> _resultIds = new();
    private readonly Dictionary<Guid, TreeItem> _resultItems = new();
    private readonly Dictionary<Guid, TreeItem> _propertyResultEntityItems = new();
    private readonly Dictionary<Guid, TreeItem> _propertyResultItems = new();
    private int _lastSelectedIndex = -1;
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
        _resultCount = GetNode<Label>("MarginContainer/VBoxContainer/ResultCount");
        _results = GetNode<Tree>("MarginContainer/VBoxContainer/Results");
        _propertyResults = GetNode<Tree>("MarginContainer/VBoxContainer/PropertyResults");
        _selectAll = GetNode<Button>("MarginContainer/VBoxContainer/SelectAll");
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
        _results.ItemSelected += OnResultSelected;
        _results.ItemActivated += OnResultActivated;
        _results.ButtonClicked += OnResultButtonClicked;
        _results.GuiInput += OnResultsGuiInput;
        _propertyResults.CellSelected += OnPropertyResultSelected;
        _propertyResults.ItemActivated += OnPropertyResultActivated;
        _propertyResults.GuiInput += OnPropertyResultsGuiInput;
        _selectAll.Pressed += OnSelectAll;
        Application.Model.Entity.Registry.Registered += OnRegistryChanged;
        Application.Model.Entity.Registry.Cleared += OnRegistryChanged;
        Application.Model.Property.Registry.Changed += OnRegistryChanged;
        Application.Model.Property.State.ValueNotified += OnPropertyValueChanged;
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
        _results.ItemSelected -= OnResultSelected;
        _results.ItemActivated -= OnResultActivated;
        _results.ButtonClicked -= OnResultButtonClicked;
        _results.GuiInput -= OnResultsGuiInput;
        _propertyResults.CellSelected -= OnPropertyResultSelected;
        _propertyResults.ItemActivated -= OnPropertyResultActivated;
        _propertyResults.GuiInput -= OnPropertyResultsGuiInput;
        _selectAll.Pressed -= OnSelectAll;
        Application.Model.Entity.Registry.Registered -= OnRegistryChanged;
        Application.Model.Entity.Registry.Cleared -= OnRegistryChanged;
        Application.Model.Property.Registry.Changed -= OnRegistryChanged;
        Application.Model.Property.State.ValueNotified -= OnPropertyValueChanged;
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
        foreach (KeyValuePair<Guid, TreeItem> pair in _resultItems)
        {
            ModelEntity entity = Application.Model.Entity.Registry.Get(pair.Key);
            if (entity != null)
            {
                ModelEntityRowBehavior.UpdateThemeIcons(pair.Value, entity,
                    ModelEntityRowBehavior.ResolvePropertyCount(entity, counts), true);
            }
        }

        foreach (KeyValuePair<Guid, TreeItem> pair in _propertyResultEntityItems)
        {
            ModelEntity entity = Application.Model.Entity.Registry.Get(pair.Key);
            if (entity != null)
            {
                pair.Value.SetIcon(0, ModelEntityRowBehavior.GetModelIcon(entity.IconId));
            }
        }

        foreach (KeyValuePair<Guid, TreeItem> pair in _propertyResultItems)
        {
            ModelProperty property = Application.Model.Property.Registry.Get(pair.Key);
            if (property != null && !string.IsNullOrEmpty(property.IconId))
            {
                Texture2D icon = ModelEntityRowBehavior.GetModelIcon(property.IconId);
                if (icon != null)
                {
                    pair.Value.SetIcon(0, icon);
                }
            }
        }

        UpdateSelectionIndicators();
        UpdatePropertySelectionIndicators();
    }

    private void OnSelectionCleared() => _lastSelectedIndex = -1;

    private void OnPropertySelectionChanged() => UpdatePropertySelectionIndicators();

    private void OnPropertySelectionCleared()
    {
        _lastSelectedPropertyItem = null;
        UpdatePropertySelectionIndicators();
    }

    private void OnResultSelected()
    {
        TreeItem item = _results.GetSelected();
        if (TryGetResultId(item, out Guid entityId))
        {
            int index = item.GetIndex();
            ModelSelectionMode mode = Application.Model.Selection.CurrentMode;
            if ((mode == ModelSelectionMode.Add || mode == ModelSelectionMode.Remove)
                && _lastSelectedIndex >= 0 && _lastSelectedIndex < _resultIds.Count)
            {
                var ids = new List<Guid>();
                int start = Math.Min(_lastSelectedIndex, index);
                int end = Math.Max(_lastSelectedIndex, index);
                for (int i = start; i <= end; i++)
                {
                    ids.Add(_resultIds[i]);
                }

                Application.Model.Entity.Pick.NotifyResults(PickUtility.PickByEntityIds(ids));
            }
            else
            {
                Application.Model.Entity.Pick.NotifyResult(PickUtility.PickByEntityId(entityId));
            }

            _lastSelectedIndex = index;
        }
    }

    private void OnPropertyResultSelected()
    {
        TreeItem item = _propertyResults.GetSelected();
        if (!TryGetPropertyResultId(item, out Guid propertyId))
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

    private void OnPropertyResultActivated()
    {
        if (TryGetPropertyResultId(_propertyResults.GetSelected(), out Guid propertyId))
        {
            Application.Model.Property.Manipulate(propertyId);
        }
    }

    private void OnResultActivated()
    {
        if (TryGetResultId(_results.GetSelected(), out Guid entityId))
        {
            ModelEntityRowBehavior.Manipulate(entityId);
        }
    }

    private void OnResultButtonClicked(TreeItem item, long column, long buttonId, long mouseButtonIndex)
    {
        if (column == 0 && TryGetResultId(item, out Guid entityId))
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
        if (item != null && TryGetResultId(item, out Guid entityId))
        {
            ModelEntityRowBehavior.ShowContextMenu(entityId);
        }
    }

    private void OnPropertyResultsGuiInput(InputEvent @event)
    {
        if (@event is not InputEventMouseButton mouseButton
            || mouseButton.ButtonIndex != MouseButton.Right
            || !mouseButton.Pressed)
        {
            return;
        }

        TreeItem item = _propertyResults.GetItemAtPosition(mouseButton.Position);
        if (TryGetPropertyResultId(item, out Guid propertyId))
        {
            Application.Ui.Menu.ShowModelPropertyMenu(
                item.GetMeta("PropertyValue", Variant.CreateFrom(string.Empty)).AsString());
        }
    }

    private void OnSelectAll()
    {
        Application.Model.Entity.Selection.Set(_resultIds.ToArray());
    }

    private void OnEntityVisibilityResolved(string entityId)
    {
        if (Guid.TryParse(entityId, out Guid id) && _resultItems.TryGetValue(id, out TreeItem item))
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
        if (Guid.TryParse(entityId, out Guid id) && _resultItems.TryGetValue(id, out TreeItem item))
        {
            ModelEntity entity = Application.Model.Entity.Registry.Get(id);
            if (entity != null)
            {
                Dictionary<Guid, int> counts = ModelEntityRowBehavior.BuildPropertySubtreeCounts(entity);
                ModelEntityRowBehavior.RebuildButtons(item, entity, ModelEntityRowBehavior.ResolvePropertyCount(entity, counts));
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
        IReadOnlyList<Guid> ids = Application.Model.Search.Find(
            entityType, entityName, propertyType, propertyValueType, propertyValue);
        IReadOnlyList<Guid> propertyIds = Application.Model.Search.FindProperties(
            entityType, entityName, propertyType, propertyValueType, propertyValue);
        _lastSelectedIndex = -1;
        _results.Clear();
        _resultIds.Clear();
        _resultItems.Clear();
        _propertyResults.Clear();
        _propertyResultEntityItems.Clear();
        _propertyResultItems.Clear();
        _lastSelectedPropertyItem = null;
        TreeItem root = _results.CreateItem();
        Dictionary<Guid, int> propertySubtreeCounts = ModelEntityRowBehavior.BuildPropertySubtreeCounts(Application.Model.Entity.Registry.Root);
        foreach (Guid id in ids)
        {
            ModelEntity entity = Application.Model.Entity.Registry.Get(id);
            if (entity == null)
            {
                Application.Log.Warn($"SearchUi: Entity {id} no longer exists.");
                continue;
            }

            Texture2D icon = ModelEntityRowBehavior.GetIndentedIcon(entity);
            TreeItem item = _results.CreateItem(root);
            item.SetText(0, entity.Name);
            item.SetIcon(0, icon);
            item.SetMetadata(0, id.ToString());
            ModelEntityRowBehavior.RebuildButtons(item, entity, ModelEntityRowBehavior.ResolvePropertyCount(entity, propertySubtreeCounts));
            _resultIds.Add(id);
            _resultItems.Add(id, item);
        }

        RefreshPropertyResults(propertyIds);
        _resultCount.Text = $"Entities: {_resultIds.Count} | Properties: {_propertyResultItems.Count}";
        _selectAll.Disabled = _resultIds.Count == 0;
        UpdateSelectionIndicators();
        UpdatePropertySelectionIndicators();
    }

    private void RefreshPropertyResults(IReadOnlyList<Guid> propertyIds)
    {
        TreeItem root = _propertyResults.CreateItem();
        var entityItems = new Dictionary<Guid, TreeItem>();
        foreach (Guid propertyId in propertyIds)
        {
            ModelProperty property = Application.Model.Property.Registry.Get(propertyId);
            ModelEntity entity = Application.Model.Property.Registry.GetOwningEntity(propertyId);
            if (property == null || entity == null)
            {
                Application.Log.Warn($"SearchUi: Property {propertyId} or its owning Entity no longer exists.");
                continue;
            }

            if (!entityItems.TryGetValue(entity.Id, out TreeItem entityItem))
            {
                entityItem = _propertyResults.CreateItem(root);
                entityItem.SetText(0, entity.Name);
                Texture2D entityIcon = ModelEntityRowBehavior.GetModelIcon(entity.IconId);
                if (entityIcon != null)
                {
                    entityItem.SetIcon(0, entityIcon);
                }

                entityItems.Add(entity.Id, entityItem);
                _propertyResultEntityItems.Add(entity.Id, entityItem);
            }

            TreeItem item = _propertyResults.CreateItem(entityItem);
            item.SetText(0, string.IsNullOrEmpty(property.Value)
                ? property.PropertyType
                : $"{property.PropertyType} : {property.Value}");
            if (!string.IsNullOrEmpty(property.IconId))
            {
                Texture2D propertyIcon = ModelEntityRowBehavior.GetModelIcon(property.IconId);
                if (propertyIcon != null)
                {
                    item.SetIcon(0, propertyIcon);
                }
            }

            item.SetMeta("PropertyId", Variant.CreateFrom(property.Id.ToString()));
            item.SetMeta("PropertyValue", Variant.CreateFrom(property.Value ?? string.Empty));
            _propertyResultItems.Add(property.Id, item);
        }
    }

    private void UpdateSelectionIndicators()
    {
        Color selectedColor = ModelEntityRowBehavior.GetSelectionColor();
        foreach (KeyValuePair<Guid, TreeItem> pair in _resultItems)
        {
            ModelEntityRowBehavior.ApplySelectionHighlight(pair.Value, pair.Key, selectedColor);
        }
    }

    private void UpdatePropertySelectionIndicators()
    {
        Color selectedColor = ModelEntityRowBehavior.GetSelectionColor();
        foreach (KeyValuePair<Guid, TreeItem> pair in _propertyResultItems)
        {
            if (Application.Model.Property.Selection.Contains(pair.Key))
            {
                pair.Value.SetCustomBgColor(0, selectedColor);
            }
            else
            {
                pair.Value.ClearCustomBgColor(0);
            }
        }
    }

    private Guid[] GetPropertyIdsInRange(TreeItem first, TreeItem last)
    {
        var items = new List<TreeItem>();
        TreeItem root = _propertyResults.GetRoot();
        if (root == null)
        {
            return TryGetPropertyResultId(last, out Guid id) ? new[] { id } : Array.Empty<Guid>();
        }

        CollectVisiblePropertyItems(root, items);
        int firstIndex = items.IndexOf(first);
        int lastIndex = items.IndexOf(last);
        if (firstIndex < 0 || lastIndex < 0)
        {
            return TryGetPropertyResultId(last, out Guid id) ? new[] { id } : Array.Empty<Guid>();
        }

        var ids = new List<Guid>();
        for (int i = Math.Min(firstIndex, lastIndex); i <= Math.Max(firstIndex, lastIndex); i++)
        {
            if (TryGetPropertyResultId(items[i], out Guid id))
            {
                ids.Add(id);
            }
        }

        return ids.ToArray();
    }

    private void CollectVisiblePropertyItems(TreeItem item, List<TreeItem> items)
    {
        if (TryGetPropertyResultId(item, out _))
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

    private static bool TryGetResultId(TreeItem item, out Guid entityId)
    {
        entityId = Guid.Empty;
        if (item == null)
        {
            return false;
        }

        if (Guid.TryParse(item.GetMetadata(0).AsString(), out entityId) && entityId != Guid.Empty)
        {
            return true;
        }

        Application.Log.Warn("SearchUi: Invalid result identifier.");
        return false;
    }

    private static bool TryGetPropertyResultId(TreeItem item, out Guid propertyId)
    {
        propertyId = Guid.Empty;
        return item != null
            && Guid.TryParse(item.GetMeta("PropertyId", Variant.CreateFrom(string.Empty)).AsString(), out propertyId)
            && propertyId != Guid.Empty;
    }

    #endregion
}
