using System;
using System.Linq;
using CoaXis.Protocol.Viewer;
using Godot;

/// <summary>EntityとPropertyの選択契約を検証する。</summary>
public partial class ModelSelectionContract : Node
{
    /// <summary>選択とTree表示の契約を検証する。</summary>
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
        Guid entityId = Guid.NewGuid();
        Guid secondEntityId = Guid.NewGuid();
        Guid thirdEntityId = Guid.NewGuid();
        Guid firstId = Guid.NewGuid();
        Guid childId = Guid.NewGuid();
        Guid lastId = Guid.NewGuid();
        Application.Model.ModelSet.Add(
            new[]
            {
                new ModelEntityDto { Id = entityId, Name = "Selection test" },
                new ModelEntityDto { Id = secondEntityId, Name = "Selection test 2" },
                new ModelEntityDto { Id = thirdEntityId, Name = "Selection test 3" },
            },
            new[]
            {
                new ModelPropertyDto { Id = firstId, ParentId = entityId, PropertyType = "First" },
                new ModelPropertyDto { Id = childId, ParentId = firstId, PropertyType = "Child" },
                new ModelPropertyDto { Id = lastId, ParentId = entityId, PropertyType = "Last" },
            });

        var entity = Application.Model.Entity.Selection;
        var property = Application.Model.Property.Selection;
        var selection = Application.Model.Selection;
        void CheckExclusive()
        {
            Check(entity.Count == 0 || property.Count == 0, "通知中にEntityとPropertyが同時選択された");
        }
        void CheckEntityNotification(string _) => CheckExclusive();
        void CheckPropertyNotification(string _) => CheckExclusive();
        entity.SelectionChanged += CheckExclusive;
        property.SelectionChanged += CheckExclusive;
        entity.Selected += CheckEntityNotification;
        property.Selected += CheckPropertyNotification;

        entity.Set(entityId);
        property.Remove(firstId);
        Check(entity.Contains(entityId), "PropertyのRemoveがEntity選択を解除した");
        property.Set(Guid.Empty);
        Check(entity.Contains(entityId), "無効なPropertyがEntity選択を解除した");
        property.Set(firstId);
        Check(entity.Count == 0 && property.Contains(firstId), "Property選択でEntityが解除されない");
        entity.Add(Guid.Empty);
        Check(property.Contains(firstId), "無効なEntityがProperty選択を解除した");
        entity.Set(Guid.NewGuid());
        entity.Add(Guid.NewGuid());
        Check(property.Contains(firstId), "未登録EntityがProperty選択を解除した");
        entity.Add(entityId);
        Check(property.Count == 0 && entity.Contains(entityId), "Entity選択でPropertyが解除されない");

        var packed = GD.Load<PackedScene>("res://scenes/ui/model_property_tree.tscn");
        var tree = packed.Instantiate<ModelPropertyTree>();
        var secondTree = packed.Instantiate<ModelPropertyTree>();
        AddChild(tree);
        AddChild(secondTree);
        tree.Show(entityId);
        secondTree.Show(entityId);
        TreeItem root = tree.GetRoot();
        TreeItem first = root.GetFirstChild();
        TreeItem child = first.GetFirstChild();
        TreeItem last = first.GetNext();
        TreeItem secondFirst = secondTree.GetRoot().GetFirstChild();
        Check(first.GetButtonById(0, ModelPropertyRowBehavior.VisibilityButtonId) >= 0,
            "PropertyTree行に共通Visibilityボタンが設定されない");

        Application.Model.Entity.Pick.SetHandlingMode(PickHandlingMode.Measurement);
        selection.SetMode(ModelSelectionMode.Set);
        Click(first);
        Check(property.Contains(firstId) && entity.Count == 0, "計測モード中のProperty選択に失敗した");
        Check(secondFirst.GetCustomBgColor(0) == Application.Ui.Theme.PrimaryColor, "別Treeへ選択色が同期されない");

        Click(secondFirst.GetNext());
        selection.SetMode(ModelSelectionMode.Add);
        Click(child);
        Check(property.Contains(lastId) && property.Contains(childId) && !property.Contains(firstId),
            "別TreeでのSet後に古い範囲起点が残った");
        selection.SetMode(ModelSelectionMode.Set);
        Click(first);
        selection.SetMode(ModelSelectionMode.Add);
        Click(last);
        Check(property.Contains(firstId) && property.Contains(childId) && property.Contains(lastId), "表示順のAdd範囲選択に失敗した");
        selection.SetMode(ModelSelectionMode.Remove);
        Click(first);
        Check(property.Count == 0, "表示順のRemove範囲選択に失敗した");

        selection.SetMode(ModelSelectionMode.Toggle);
        Click(child);
        Check(property.Contains(childId), "Toggle選択に失敗した");
        first.Collapsed = true;
        tree.EmitSignal(Tree.SignalName.ItemCollapsed, first);
        Check(first.GetCustomBgColor(0) == Application.Ui.Theme.PrimaryColor, "折り畳み祖先に選択色が表示されない");
        first.Collapsed = false;
        tree.EmitSignal(Tree.SignalName.ItemCollapsed, first);
        Check(child.GetCustomBgColor(0) == Application.Ui.Theme.PrimaryColor, "展開後に選択色が戻らない");
        child.SetCustomBgColor(0, Colors.Red);
        Application.Ui.Theme.EmitSignal(UiThemeHub.SignalName.ThemeChanged);
        Check(child.GetCustomBgColor(0) == Application.Ui.Theme.PrimaryColor, "テーマ変更時に選択色が更新されない");
        tree.Refresh();
        root = tree.GetRoot();
        first = root.GetFirstChild();
        child = first.GetFirstChild();
        Check(child.GetCustomBgColor(0) == Application.Ui.Theme.PrimaryColor, "Tree再構築後に選択色が戻らない");

        selection.SetMode(ModelSelectionMode.Set);
        Click(first);
        first.Collapsed = true;
        tree.EmitSignal(Tree.SignalName.ItemCollapsed, first);
        selection.SetMode(ModelSelectionMode.Add);
        Click(first.GetNext());
        Check(property.Contains(firstId) && property.Contains(lastId) && !property.Contains(childId),
            "折り畳み中の範囲選択が非表示行を含んだ");

        Application.Model.Entity.Pick.SetHandlingMode(PickHandlingMode.Selection);
        selection.SetMode(ModelSelectionMode.Set);
        Application.Model.Entity.Pick.NotifyResult(null);
        Check(property.Count == 0, "空PickでProperty選択が解除されない");
        property.Set(lastId);
        property.Add(childId);
        property.Add(firstId);
        var selectionUiScene = GD.Load<PackedScene>("res://scenes/ui/selection_ui.tscn");
        var orderedPropertySelectionUi = selectionUiScene.Instantiate<SelectionUi>();
        AddChild(orderedPropertySelectionUi);
        Tree orderedPropertyTree = orderedPropertySelectionUi.GetNode<Tree>("MarginContainer/VBoxContainer/Tree");
        TreeItem propertyEntityItem = orderedPropertyTree.GetRoot().GetFirstChild();
        TreeItem orderedFirstProperty = propertyEntityItem.GetFirstChild();
        TreeItem orderedChildProperty = orderedFirstProperty.GetNext();
        TreeItem orderedLastProperty = orderedChildProperty.GetNext();
        Check(ModelPropertyRowBehavior.GetPropertyId(orderedFirstProperty) == firstId,
            "Selection UIのProperty表示順が階層順と一致しない: first");
        Check(ModelPropertyRowBehavior.GetPropertyId(orderedChildProperty) == childId,
            "Selection UIのProperty表示順が階層順と一致しない: child");
        Check(ModelPropertyRowBehavior.GetPropertyId(orderedLastProperty) == lastId,
            "Selection UIのProperty表示順が階層順と一致しない: last");
        orderedPropertySelectionUi.Free();

        Application.Model.ModelSet.Remove(Array.Empty<Guid>(), new[] { firstId });
        Check(property.SelectedIds.SequenceEqual(new[] { lastId }), "連鎖削除で選択が同期されない");

        var selectionUi = selectionUiScene.Instantiate<SelectionUi>();
        AddChild(selectionUi);
        Tree selectedTree = selectionUi.GetNode<Tree>("MarginContainer/VBoxContainer/Tree");
        Label selectionCountLabel = selectionUi.GetNode<Label>("MarginContainer/VBoxContainer/SelectionCountLabel");
        TreeItem selectedEntityItem = selectedTree.GetRoot().GetFirstChild();
        TreeItem selectedPropertyItem = selectedEntityItem.GetFirstChild();
        Check(ModelPropertyRowBehavior.GetPropertyId(selectedPropertyItem) == lastId,
            "Selection UIに選択中Propertyが表示されない");
        Check(selectedPropertyItem.GetButtonById(0, ModelPropertyRowBehavior.VisibilityButtonId) >= 0,
            "Selection UIのProperty行に共通Visibilityボタンが設定されない");
        Check(selectionCountLabel.Text == "Selected: 1", "Selection UIの総選択数が一致しない");
        selectionUi.Free();

        entity.Set(entityId);
        var entitySelectionUi = selectionUiScene.Instantiate<SelectionUi>();
        AddChild(entitySelectionUi);
        Tree entitySelectionTree = entitySelectionUi.GetNode<Tree>("MarginContainer/VBoxContainer/Tree");
        Label entitySelectionCountLabel = entitySelectionUi.GetNode<Label>("MarginContainer/VBoxContainer/SelectionCountLabel");
        TreeItem selectedEntity = entitySelectionTree.GetRoot().GetFirstChild();
        Check(selectedEntity.GetMeta("EntityId", Variant.CreateFrom(string.Empty)).AsString() == entityId.ToString(),
            "Selection UIに選択中Entityが表示されない");
        Check(entitySelectionCountLabel.Text == "Selected: 1", "Entity選択時のSelection UI総選択数が一致しない");
        entitySelectionUi.Free();

        entity.Set(new[] { thirdEntityId, entityId, secondEntityId });
        var orderedEntitySelectionUi = selectionUiScene.Instantiate<SelectionUi>();
        AddChild(orderedEntitySelectionUi);
        Tree orderedEntityTree = orderedEntitySelectionUi.GetNode<Tree>("MarginContainer/VBoxContainer/Tree");
        TreeItem firstSelectedEntity = orderedEntityTree.GetRoot().GetFirstChild();
        TreeItem secondSelectedEntity = firstSelectedEntity.GetNext();
        TreeItem thirdSelectedEntity = secondSelectedEntity.GetNext();
        Check(GetEntityId(firstSelectedEntity) == entityId,
            "Selection UIのEntity表示順がRegistry階層順と一致しない: first");
        Check(GetEntityId(secondSelectedEntity) == secondEntityId,
            "Selection UIのEntity表示順がRegistry階層順と一致しない: second");
        Check(GetEntityId(thirdSelectedEntity) == thirdEntityId,
            "Selection UIのEntity表示順がRegistry階層順と一致しない: third");
        orderedEntitySelectionUi.Free();

        selection.Clear();
        Check(entity.Count == 0 && property.Count == 0, "共通ClearでEntity選択が解除されない");

        entity.SelectionChanged -= CheckExclusive;
        property.SelectionChanged -= CheckExclusive;
        entity.Selected -= CheckEntityNotification;
        property.Selected -= CheckPropertyNotification;
        tree.Free();
        secondTree.Free();
        Application.Model.ModelSet.Clear();
    }

    private static void Click(TreeItem item)
    {
        item.Select(0);
    }

    private static Guid GetEntityId(TreeItem item)
    {
        string value = item.GetMeta("EntityId", Variant.CreateFrom(string.Empty)).AsString();
        return Guid.TryParse(value, out Guid entityId) ? entityId : Guid.Empty;
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
