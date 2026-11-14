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
    private ItemList _results = null!;
    private Button _selectAll = null!;
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
        _results = GetNode<ItemList>("MarginContainer/VBoxContainer/Results");
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
        _results.GuiInput += OnResultsGuiInput;
        _selectAll.Pressed += OnSelectAll;
        Application.Model.Entity.Registry.Registered += OnRegistryChanged;
        Application.Model.Entity.Registry.Cleared += OnRegistryChanged;
        Application.Model.Property.Registry.Changed += OnRegistryChanged;
        Application.Model.Property.State.ValueNotified += OnPropertyValueChanged;
        Application.Model.Entity.Selection.SelectionChanged += OnSelectionChanged;
        Application.Model.Entity.Selection.Cleared += OnSelectionCleared;
    }

    private void UnsubscribeEvents()
    {
        _entityType.ItemSelected -= OnTypeSelected;
        _propertyType.ItemSelected -= OnTypeSelected;
        _propertyValueType.ItemSelected -= OnTypeSelected;
        _entityName.TextChanged -= OnTextChanged;
        _propertyValue.TextChanged -= OnTextChanged;
        _results.ItemSelected -= OnResultSelected;
        _results.GuiInput -= OnResultsGuiInput;
        _selectAll.Pressed -= OnSelectAll;
        Application.Model.Entity.Registry.Registered -= OnRegistryChanged;
        Application.Model.Entity.Registry.Cleared -= OnRegistryChanged;
        Application.Model.Property.Registry.Changed -= OnRegistryChanged;
        Application.Model.Property.State.ValueNotified -= OnPropertyValueChanged;
        Application.Model.Entity.Selection.SelectionChanged -= OnSelectionChanged;
        Application.Model.Entity.Selection.Cleared -= OnSelectionCleared;
    }

    private void OnTypeSelected(long index) => RefreshResults();

    private void OnTextChanged(string text) => RefreshResults();

    private void OnRegistryChanged() => RefreshTypesAndResults();

    private void OnPropertyValueChanged(string propertyId) => RefreshResults();

    private void OnSelectionChanged() => UpdateSelectionIndicators();

    private void OnSelectionCleared() => _lastSelectedIndex = -1;

    private void OnResultSelected(long index)
    {
        if (TryGetResultId((int)index, out Guid entityId))
        {
            ModelEntitySelectionMode mode = Application.Model.Entity.Selection.Mode;
            if ((mode == ModelEntitySelectionMode.Add || mode == ModelEntitySelectionMode.Remove)
                && _lastSelectedIndex >= 0 && _lastSelectedIndex < _results.ItemCount)
            {
                var ids = new List<Guid>();
                int start = Math.Min(_lastSelectedIndex, (int)index);
                int end = Math.Max(_lastSelectedIndex, (int)index);
                for (int i = start; i <= end; i++)
                {
                    if (TryGetResultId(i, out Guid rangeEntityId))
                    {
                        ids.Add(rangeEntityId);
                    }
                }

                Application.Model.Entity.Pick.NotifyResults(PickUtility.PickByEntityIds(ids));
            }
            else
            {
                Application.Model.Entity.Pick.NotifyResult(PickUtility.PickByEntityId(entityId));
            }

            _lastSelectedIndex = (int)index;
        }

        _results.DeselectAll();
    }

    private void OnResultsGuiInput(InputEvent @event)
    {
        if (@event is not InputEventMouseButton mouseButton
            || mouseButton.ButtonIndex != MouseButton.Right
            || !mouseButton.Pressed)
        {
            return;
        }

        int index = _results.GetItemAtPosition(mouseButton.Position, true);
        if (index >= 0 && TryGetResultId(index, out Guid entityId))
        {
            Application.Ui.Menu.ShowModelEntityMenu(entityId);
        }
    }

    private void OnSelectAll()
    {
        var ids = new List<Guid>();
        for (int i = 0; i < _results.ItemCount; i++)
        {
            if (TryGetResultId(i, out Guid entityId))
            {
                ids.Add(entityId);
            }
        }

        Application.Model.Entity.Selection.Set(ids.ToArray());
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
        foreach (Guid id in ids)
        {
            ModelEntity entity = Application.Model.Entity.Registry.Get(id);
            if (entity == null)
            {
                Application.Log.Warn($"SearchUi: Entity {id} no longer exists.");
                continue;
            }

            Texture2D icon = Application.Asset.Icon.GetModelIcon(entity.IconId, Constant.Ui.Tree.HierarchyVisibleIconSize);
            int index = _results.AddItem(entity.Name, icon);
            _results.SetItemMetadata(index, id.ToString());
        }

        _resultCount.Text = $"Results: {_results.ItemCount}";
        _selectAll.Disabled = _results.ItemCount == 0;
        UpdateSelectionIndicators();
    }

    private void UpdateSelectionIndicators()
    {
        Color selectedColor = Color.FromHtml(Application.Setting.Current.Color.HierarchySelectedColor);
        for (int i = 0; i < _results.ItemCount; i++)
        {
            if (!TryGetResultId(i, out Guid id))
            {
                continue;
            }

            _results.SetItemCustomBgColor(i, Application.Model.Entity.Selection.Contains(id)
                ? selectedColor
                : Colors.Transparent);
        }
    }

    private bool TryGetResultId(int index, out Guid entityId)
    {
        if (Guid.TryParse(_results.GetItemMetadata(index).AsString(), out entityId))
        {
            return true;
        }

        Application.Log.Warn($"SearchUi: Invalid result identifier at index {index}.");
        return false;
    }

    #endregion
}
