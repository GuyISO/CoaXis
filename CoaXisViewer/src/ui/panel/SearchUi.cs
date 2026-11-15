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
    private Button _selectAll = null!;
    private readonly List<Guid> _resultIds = new();
    private readonly Dictionary<Guid, TreeItem> _resultItems = new();
    private int _lastSelectedIndex = -1;

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
        _selectAll = GetNode<Button>("MarginContainer/VBoxContainer/SelectAll");
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
        _results.ItemSelected += OnResultSelected;
        _results.ItemActivated += OnResultActivated;
        _results.ButtonClicked += OnResultButtonClicked;
        _results.GuiInput += OnResultsGuiInput;
        _selectAll.Pressed += OnSelectAll;
        Application.Model.Entity.Registry.Registered += OnRegistryChanged;
        Application.Model.Entity.Registry.Cleared += OnRegistryChanged;
        Application.Model.Property.Registry.Changed += OnRegistryChanged;
        Application.Model.Property.State.ValueNotified += OnPropertyValueChanged;
        Application.Model.Entity.Selection.SelectionChanged += OnSelectionChanged;
        Application.Model.Entity.Selection.Cleared += OnSelectionCleared;
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
        _results.ItemSelected -= OnResultSelected;
        _results.ItemActivated -= OnResultActivated;
        _results.ButtonClicked -= OnResultButtonClicked;
        _results.GuiInput -= OnResultsGuiInput;
        _selectAll.Pressed -= OnSelectAll;
        Application.Model.Entity.Registry.Registered -= OnRegistryChanged;
        Application.Model.Entity.Registry.Cleared -= OnRegistryChanged;
        Application.Model.Property.Registry.Changed -= OnRegistryChanged;
        Application.Model.Property.State.ValueNotified -= OnPropertyValueChanged;
        Application.Model.Entity.Selection.SelectionChanged -= OnSelectionChanged;
        Application.Model.Entity.Selection.Cleared -= OnSelectionCleared;
        Application.Model.Entity.Visual.VisibilityResolved -= OnEntityVisibilityResolved;
        Application.Model.Entity.State.PropertyTreeVisibleNotified -= OnEntityButtonsChanged;
        Application.Model.Entity.State.Collapsed -= OnEntityButtonsChanged;
        Application.Ui.Theme.ThemeChanged -= OnThemeChanged;
    }

    private void OnTypeSelected(long index) => RefreshResults();

    private void OnTextChanged(string text) => RefreshResults();

    private void OnRegistryChanged() => RefreshTypesAndResults();

    private void OnPropertyValueChanged(string propertyId) => RefreshResults();

    private void OnSelectionChanged() => UpdateSelectionIndicators();

    private void OnThemeChanged() => UpdateSelectionIndicators();

    private void OnSelectionCleared() => _lastSelectedIndex = -1;

    private void OnResultSelected()
    {
        TreeItem item = _results.GetSelected();
        if (TryGetResultId(item, out Guid entityId))
        {
            int index = item.GetIndex();
            ModelEntitySelectionMode mode = Application.Model.Entity.Selection.Mode;
            if ((mode == ModelEntitySelectionMode.Add || mode == ModelEntitySelectionMode.Remove)
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
        IReadOnlyList<Guid> ids = Application.Model.Search.Find(
            GetSelectedType(_entityType), _entityName.Text,
            GetSelectedType(_propertyType), GetSelectedType(_propertyValueType), _propertyValue.Text);
        _lastSelectedIndex = -1;
        _results.Clear();
        _resultIds.Clear();
        _resultItems.Clear();
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

        _resultCount.Text = $"Results: {_resultIds.Count}";
        _selectAll.Disabled = _resultIds.Count == 0;
        UpdateSelectionIndicators();
    }

    private void UpdateSelectionIndicators()
    {
        Color selectedColor = ModelEntityRowBehavior.GetSelectionColor();
        foreach (KeyValuePair<Guid, TreeItem> pair in _resultItems)
        {
            ModelEntityRowBehavior.ApplySelectionHighlight(pair.Value, pair.Key, selectedColor);
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

    #endregion
}
