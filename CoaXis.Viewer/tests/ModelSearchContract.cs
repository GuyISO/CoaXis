using System;
using System.Collections.Generic;
using System.Linq;
using CoaXis.Protocol.Viewer;
using Godot;

/// <summary>EntityとPropertyの検索結果契約を検証する。</summary>
public partial class ModelSearchContract : Node
{
    /// <summary>検索結果の契約を検証する。</summary>
    public bool Run()
    {
        try
        {
            RunChecks();
            return true;
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            return false;
        }
    }

    private void RunChecks()
    {
        Guid doorAId = Guid.NewGuid();
        Guid doorBId = Guid.NewGuid();
        Guid windowId = Guid.NewGuid();
        Guid materialAId = Guid.NewGuid();
        Guid gradeAId = Guid.NewGuid();
        Guid materialBId = Guid.NewGuid();
        Guid materialWindowId = Guid.NewGuid();
        Guid[] entityIds = { doorAId, doorBId, windowId };

        Application.Model.ModelSet.Add(
            new[]
            {
                new ModelEntityDto { Id = doorAId, Type = "Part", Name = "Door A" },
                new ModelEntityDto { Id = doorBId, Type = "Part", Name = "Door B" },
                new ModelEntityDto { Id = windowId, Type = "Part", Name = "Window" }
            },
            new[]
            {
                new ModelPropertyDto
                {
                    Id = materialAId, ParentId = doorAId, PropertyType = "Material",
                    ValueType = "string", Value = "Steel"
                },
                new ModelPropertyDto
                {
                    Id = gradeAId, ParentId = materialAId, PropertyType = "Grade",
                    ValueType = "string", Value = "A"
                },
                new ModelPropertyDto
                {
                    Id = materialBId, ParentId = doorBId, PropertyType = "Material",
                    ValueType = "string", Value = "Wood"
                },
                new ModelPropertyDto
                {
                    Id = materialWindowId, ParentId = windowId, PropertyType = "Material",
                    ValueType = "string", Value = "Steel"
                }
            });

        try
        {
            IReadOnlyList<Guid> entityResults = Application.Model.Search.Find(
                "Part", "Door", "Grade", "string", "a");
            IReadOnlyList<Guid> propertyResults = Application.Model.Search.FindProperties(
                "Part", "Door", "Grade", "string", "a");
            Check(entityResults.SequenceEqual(new[] { doorAId }), "検索条件に一致するEntity結果が不正");
            Check(propertyResults.SequenceEqual(new[] { gradeAId }), "階層下の一致Property結果が不正");

            IReadOnlyList<Guid> unmatchedPropertyResults = Application.Model.Search.FindProperties(
                "Part", "Door", "Material", "string", "a");
            Check(unmatchedPropertyResults.Count == 0, "異なるPropertyに分かれた条件を一致扱いしている");

            IReadOnlyList<Guid> entityFilteredProperties = Application.Model.Search.FindProperties(
                "Part", "Door", string.Empty, string.Empty, string.Empty);
            Check(entityFilteredProperties.SequenceEqual(new[] { materialAId, gradeAId, materialBId }),
                "Entity条件のみ指定したProperty結果が不正");

            var searchUiScene = GD.Load<PackedScene>("res://scenes/ui/search_ui.tscn");
            var searchUi = searchUiScene.Instantiate<SearchUi>();
            AddChild(searchUi);
            Tree results = searchUi.GetNode<Tree>("MarginContainer/VBoxContainer/Results");
            TreeItem doorAItem = FindEntityItem(results.GetRoot(), doorAId);
            TreeItem gradeItem = FindPropertyItem(results.GetRoot(), gradeAId);
            TreeItem materialItem = FindPropertyItem(results.GetRoot(), materialAId);
            Check(gradeItem != null, "Search UIに検索対象Propertyが表示されない");
            Check(materialItem != null, "Search UIに階層上のPropertyが表示されない");
            Check(doorAItem != null, "Search UIに検索対象Entityが表示されない");
            Check(gradeItem.GetButtonById(0, ModelPropertyRowBehavior.VisibilityButtonId) >= 0,
                "Search UIのProperty行に共通Visibilityボタンが設定されない");
            Texture2D materialIcon = materialItem?.GetIcon(0);
            Texture2D gradeIcon = gradeItem?.GetIcon(0);
            Check(materialIcon is AtlasTexture materialAtlas
                && gradeIcon is AtlasTexture gradeAtlas
                && doorAItem.GetIcon(0) is AtlasTexture doorAAtlas
                && materialAtlas.Region.Size.X > doorAAtlas.Region.Size.X
                && gradeAtlas.Region.Size.X > materialAtlas.Region.Size.X,
                "Search UIのEntityとProperty階層がアイコンオフセットに反映されない");
            Check(materialItem.GetParent() == results.GetRoot()
                && gradeItem.GetParent() == results.GetRoot()
                && doorAItem.GetParent() == results.GetRoot(),
                "検索結果行がTree階層なしでルート直下に配置されない");
            Check(materialItem.GetNext() == gradeItem, "検索結果のProperty表示順が階層順にならない");

            Application.Model.Selection.SetMode(ModelSelectionMode.Set);
            materialItem.Select(0);
            Application.Model.Selection.SetMode(ModelSelectionMode.Add);
            gradeItem.Select(0);
            Check(Application.Model.Property.Selection.Contains(materialAId)
                && Application.Model.Property.Selection.Contains(gradeAId),
                "PropertyのAdd範囲選択が平坦化後に複数選択されない");
            Application.Model.Selection.SetMode(ModelSelectionMode.Remove);
            materialItem.Select(0);
            Check(Application.Model.Property.Selection.Count == 0,
                "PropertyのRemove範囲選択が平坦化後に複数解除されない");

            Application.Model.Selection.SetMode(ModelSelectionMode.Set);
            doorAItem.Select(0);
            TreeItem doorBItem = FindEntityItem(results.GetRoot(), doorBId);
            Application.Model.Selection.SetMode(ModelSelectionMode.Add);
            doorBItem.Select(0);
            Check(Application.Model.Entity.Selection.Contains(doorAId)
                && Application.Model.Entity.Selection.Contains(doorBId),
                "EntityのAdd範囲選択が平坦化後に複数選択されない");

            Button entityCount = searchUi.GetNode<Button>("MarginContainer/VBoxContainer/EntityResultCount");
            Button propertyCount = searchUi.GetNode<Button>("MarginContainer/VBoxContainer/PropertyResultCount");
            Check(entityCount.Text.StartsWith("Entity | "), "Entity件数ボタンが表示されない");
            Check(propertyCount.Text.StartsWith("Property | "), "Property件数ボタンが表示されない");
            Check(entityCount.ToggleMode && propertyCount.ToggleMode, "件数ボタンがトグルとして設定されない");

            entityCount.ButtonPressed = false;
            entityCount.EmitSignal(Button.SignalName.Pressed);
            Check(FindEntityItem(results.GetRoot(), doorAId) == null,
                "Entity表示OFF後も検索結果にEntityが残っている");
            Check(FindPropertyItem(results.GetRoot(), gradeAId) != null,
                "Entity表示OFF後に検索結果Propertyが消えた");

            entityCount.ButtonPressed = true;
            entityCount.EmitSignal(Button.SignalName.Pressed);
            propertyCount.ButtonPressed = false;
            propertyCount.EmitSignal(Button.SignalName.Pressed);
            Check(FindEntityItem(results.GetRoot(), doorAId) != null,
                "Property表示OFF後に検索結果Entityが消えた");
            Check(FindPropertyItem(results.GetRoot(), gradeAId) == null,
                "Property表示OFF後も検索結果にPropertyが残っている");

            Button selectAllProperties =
                searchUi.GetNode<Button>("MarginContainer/VBoxContainer/SelectAll/SelectAllProperties");
            selectAllProperties.EmitSignal(Button.SignalName.Pressed);
            Check(Application.Model.Property.Selection.SelectedIds.Contains(gradeAId),
                "Property用Select Allが検索結果Propertyを選択しない");
            Button selectAllEntities =
                searchUi.GetNode<Button>("MarginContainer/VBoxContainer/SelectAll/SelectAllEntities");
            selectAllEntities.EmitSignal(Button.SignalName.Pressed);
            Check(Application.Model.Entity.Selection.Contains(doorAId)
                && Application.Model.Property.Selection.Count == 0,
                "Entity用Select AllがProperty選択を排他的に解除しない");
            searchUi.Free();
        }
        finally
        {
            Application.Model.ModelSet.Remove(entityIds, Array.Empty<Guid>());
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static TreeItem FindPropertyItem(TreeItem item, Guid propertyId)
    {
        for (TreeItem current = item; current != null; current = current.GetNext())
        {
            if (ModelPropertyRowBehavior.GetPropertyId(current) == propertyId)
            {
                return current;
            }

            TreeItem descendant = FindPropertyItem(current.GetFirstChild(), propertyId);
            if (descendant != null)
            {
                return descendant;
            }
        }

        return null;
    }

    private static TreeItem FindEntityItem(TreeItem item, Guid entityId)
    {
        for (TreeItem current = item; current != null; current = current.GetNext())
        {
            if (Guid.TryParse(current.GetMetadata(0).AsString(), out Guid id) && id == entityId)
            {
                return current;
            }

            TreeItem descendant = FindEntityItem(current.GetFirstChild(), entityId);
            if (descendant != null)
            {
                return descendant;
            }
        }

        return null;
    }
}
