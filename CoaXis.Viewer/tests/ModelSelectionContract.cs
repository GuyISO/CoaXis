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
        Guid firstId = Guid.NewGuid();
        Guid childId = Guid.NewGuid();
        Guid lastId = Guid.NewGuid();
        Application.Model.ModelSet.Add(
            new[] { new ModelEntityDto { Id = entityId, Name = "Selection test" } },
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
        property.Add(new[] { firstId, childId, lastId });
        Application.Model.ModelSet.Remove(Array.Empty<Guid>(), new[] { firstId });
        Check(property.SelectedIds.SequenceEqual(new[] { lastId }), "連鎖削除で選択が同期されない");
        selection.Clear();
        Check(property.Count == 0, "共通ClearでProperty選択が解除されない");

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

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
