using Godot;
using System;
using System.Collections.Generic;

public partial class SpecificationTree : Tree
{

    private Node3D _models = null;
    private Dictionary<Node3D, TreeItem> _mapNodeToTreeItem = new Dictionary<Node3D, TreeItem>();

    public override void _Ready()
    {
        
        ItemSelected += OnItemSelected;

        _models = GetNode<Node3D>("/root/Main/Models");
        
        AddChildToTree(_models);
    }

    private void AddChildToTree(Node3D node, TreeItem parentTreeItem = null)
    {
        // モデルアイテム
        TreeItem treeItem = CreateItem(parentTreeItem);
        treeItem.SetText(0, node.Name);
        treeItem.SetMetadata(0, node);

        _mapNodeToTreeItem[node] = treeItem;

        // 子ノードを再帰的に追加
        foreach (Node3D childNode in node.GetChildren())
        {
            // 純粋なNode3Dのみをツリーに追加する
            if (childNode.GetType() == typeof(Node3D))
            {
                AddChildToTree(childNode, treeItem);
            }
        }
    }

    private void OnItemSelected()
    {
        TreeItem selectedItem = GetSelected();
        if (selectedItem != null)
        {
            Node3D node = selectedItem.GetMetadata(0).As<Node3D>();
            if (node != null)
            {
                GD.Print($"Selected: {node.Name}");
                // ここで選択されたノードに対して何らかの操作を行うことができます。
            }
        }
    }
    private void OnStaticBodyClicked(StaticBody3D body)
    {
        // StaticBodyの親はNode3Dという構成
        Node3D node = body.GetParent<Node3D>();

        // 辞書のKeyに入っているNode3Dを探して、対応するTreeItemを取得
        if (_mapNodeToTreeItem.TryGetValue(node, out var treeItem))
        {
            SelectTreeItem(treeItem);
        }
    }
    private void SelectTreeItem(TreeItem item)
    {
        DeselectAll();
        item.Select(0);
        ScrollToItem(item);
    }

}
