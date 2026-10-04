using Godot;

/// <summary>
/// コマンド履歴の実行状態を表す列挙型
/// </summary>
public enum CommandExecutionState
{
    Do,
    Undo,
}

/// <summary>
/// コマンド履歴表示と操作用のパネル
/// </summary>
public partial class CommandUi : PanelContainer
{
    
    /// <summary>
    /// CommandUi の列を表す列挙型
    /// </summary>
    public enum TreeColumn
    {
        No,
        Name,
        Description,
        State,
    }

    #region Fields

    private bool _isUpdatingTree = false;
    private bool _isRequestingCursorMove = false;
    private bool _isRebuildQueued = false;

    // 関連ノードをキャッシュ
    private Tree _tree = null!;

    #endregion

    #region Lifecycle

    public override void _Ready()
    {
        EnsureChildNodes();
        SubscribeUiEvents();
        SubscribeApplicationEvents();

        QueueRebuildTimelineTree();
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
        _tree = (Tree)FindChild("Tree");
        TreeColumn[] columns = System.Enum.GetValues<TreeColumn>();
        _tree.Columns = columns.Length;

        foreach (TreeColumn column in columns)
        {
            int columnIndex = (int)column;
            _tree.SetColumnTitle(columnIndex, column.ToString());

            bool isExpand = column != TreeColumn.No && column != TreeColumn.State;
            _tree.SetColumnExpand(columnIndex, isExpand);
        }

        // 固定幅にして運用する列の幅を指定する
        _tree.SetColumnCustomMinimumWidth((int)TreeColumn.No, Constant.Ui.Tree.CommandNoColumnMinWidth);
        _tree.SetColumnCustomMinimumWidth((int)TreeColumn.State, Constant.Ui.Tree.CommandStateColumnMinWidth);
    }
    
    /// <summary>
    /// UIイベントの購読を開始する
    /// </summary>
    private void SubscribeUiEvents()
    {
        _tree.ItemSelected += OnTreeItemSelected;
        _tree.ItemActivated += OnTreeItemActivated;
    }

    /// <summary>
    /// UIイベントの購読を解除する
    /// </summary>
    private void UnsubscribeUiEvents()
    {
        _tree.ItemSelected -= OnTreeItemSelected;
        _tree.ItemActivated -= OnTreeItemActivated;
    }

    /// <summary>
    /// Applicationイベントの購読を開始する
    /// </summary>
    private void SubscribeApplicationEvents()
    {
        Application.Command.Executed += OnCommandExecuted;
    }

    /// <summary>
    /// Applicationイベントの購読を解除する
    /// </summary>
    private void UnsubscribeApplicationEvents()
    {
        Application.Command.Executed -= OnCommandExecuted;
    }

    /// <summary>
    /// コマンド実行状態の変更通知を受け取り、Hubの最新状態でツリーを更新する
    /// </summary>
    private void OnCommandExecuted()
    {
        _isRequestingCursorMove = false;
        QueueRebuildTimelineTree();
    }

    /// <summary>
    /// 履歴ツリーの選択変更時に呼び出されるイベントハンドラ
    /// </summary>
    private void OnTreeItemSelected()
    {
        RequestCursorMoveFromSelection();
    }

    /// <summary>
    /// 履歴ツリーのアイテムが確定されたときに呼び出されるイベントハンドラ
    /// </summary>
    private void OnTreeItemActivated()
    {
        RequestCursorMoveFromSelection();
    }

    /// <summary>
    /// 現在選択されている履歴行へのカーソル移動をリクエストする
    /// </summary>
    private void RequestCursorMoveFromSelection()
    {
        if (_isUpdatingTree || _isRequestingCursorMove)
        {
            return;
        }

        TreeItem selected = _tree.GetSelected();
        if (selected == null)
        {
            return;
        }

        Variant metadata = selected.GetMetadata(0);
        if (metadata.VariantType != Variant.Type.Int)
        {
            return;
        }

        int nextCursor = (int)metadata + 1;
        if (nextCursor == Application.Command.Cursor)
        {
            return;
        }

        _isRequestingCursorMove = true;
        Application.Command.SetCursor(nextCursor);
    }

    #endregion

    #region Internal Helpers

    /// <summary>
    /// タイムラインツリー再構築を遅延キューへ積む
    /// </summary>
    private void QueueRebuildTimelineTree()
    {
        if (_isRebuildQueued)
        {
            return;
        }

        _isRebuildQueued = true;
        CallDeferred(MethodName.RebuildTimelineTreeDeferred);
    }

    /// <summary>
    /// 遅延呼び出しでタイムラインツリーを再構築する
    /// </summary>
    private void RebuildTimelineTreeDeferred()
    {
        _isRebuildQueued = false;
        if (_tree == null || !GodotObject.IsInstanceValid(_tree))
        {
            return;
        }

        RebuildTimelineTree();
    }

    /// <summary>
    /// タイムラインツリーを現在の履歴状態で再構築する
    /// </summary>
    private void RebuildTimelineTree()
    {
        if (_tree == null || !GodotObject.IsInstanceValid(_tree))
        {
            return;
        }

        _isUpdatingTree = true;
        BaseCommand[] history = Application.Command.History;
        int cursor = Application.Command.Cursor;

        try
        {
            _tree.Clear();
            TreeItem root = _tree.CreateItem();
            if (root == null)
            {
                return;
            }

            for (int i = 0; i < history.Length; i++)
            {
                BaseCommand command = history[i];
                TreeItem item = _tree.CreateItem(root);
                if (item == null)
                {
                    continue;
                }

                item.SetMetadata((int)TreeColumn.No, i);
                item.SetText((int)TreeColumn.No, i.ToString());
                item.SetText((int)TreeColumn.Name, command?.GetType().Name ?? "(null)");
                item.SetText((int)TreeColumn.Description, command?.Description ?? string.Empty);

                CommandExecutionState state = ResolveState(i, cursor);
                Color color = ResolveStateColor(i, cursor);
                item.SetText((int)TreeColumn.State, state.ToString());

                for (int column = 0; column < _tree.Columns; column++)
                {
                    item.SetCustomColor(column, color);
                }

                if (i == cursor - 1)
                {
                    item.Select((int)TreeColumn.No);
                }
            }
        }
        finally
        {
            _isUpdatingTree = false;
        }

        // 履歴が空の状態ではスクロール対象が存在しない
        TreeItem lastItem = GetLastTreeItem();
        if (lastItem != null)
        {
            _tree.ScrollToItem(lastItem);
        }
    }

    /// <summary>
    /// タイムラインツリーの最終行のアイテムを返す
    /// </summary>
    /// <returns>最終行の TreeItem、存在しない場合は null</returns>
    /// <remarks>単一階層のツリーを想定している</remarks>
    private TreeItem GetLastTreeItem()
    {
        var root = _tree.GetRoot();
        if (root == null) return null;

        var item = root.GetFirstChild();
        if (item == null) return null;

        while (item.GetNext() != null)
        {
            item = item.GetNext();
        }

        return item;
    }

    /// <summary>
    /// 履歴インデックスに対応する状態文字列を返す
    /// </summary>
    private static CommandExecutionState ResolveState(int index, int cursor)
    {
        if (index < cursor)
        {
            return CommandExecutionState.Do;
        }

        return CommandExecutionState.Undo;
    }

    /// <summary>
    /// 履歴インデックスに対応する表示色を返す
    /// </summary>
    private Color ResolveStateColor(int index, int cursor)
    {
        if (index < cursor)
        {
            return Color.FromHtml(Constant.Color.CommandDoColor);
        }

        return Color.FromHtml(Constant.Color.CommandUndoColor);
    }

    #endregion
}
