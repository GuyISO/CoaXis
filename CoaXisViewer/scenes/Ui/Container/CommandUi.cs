using Godot;
using System.Collections.Generic;

/// <summary>
/// コマンド履歴表示と操作用のパネル
/// </summary>
public partial class CommandUi : PanelContainer
{
    #region Fields

    private bool _isInitialized = false; // 初回状態通知を受けたかだけを保持する
    private bool _isUpdatingTree = false;
    private bool _isRequestingCursorMove = false;
    private bool _isRebuildQueued = false;
    private int _cursor = 0;
    private readonly List<CommandBase> _history = new();

    // 関連ノードをキャッシュ
    private Tree _tree = null!;

    #endregion

    #region Lifecycle

    public override void _Ready()
    {
        EnsureChildNodes();
        SubscribeUiEvents();
        SubscribeApplicationEvents();
    }

    public override void _ExitTree()
    {
        UnsubscribeUiEvents();
        UnsubscribeApplicationEvents();

        base._ExitTree();
    }

    public override void _Process(double delta)
    {
        if (!_isInitialized)
        {
            Application.Command.Event.AskState();
        }
    }

    #endregion

    #region Events

    /// <summary>
    /// 子ノードを解決し、フィールドに保持する
    /// </summary>
    private void EnsureChildNodes()
    {
        _tree = (Tree)FindChild("Tree");
        CommandTreeColumn[] columns = System.Enum.GetValues<CommandTreeColumn>();
        _tree.Columns = columns.Length;

        foreach (CommandTreeColumn column in columns)
        {
            int columnIndex = (int)column;
            _tree.SetColumnTitle(columnIndex, column.ToString());

            bool isExpand = column != CommandTreeColumn.No && column != CommandTreeColumn.State;
            _tree.SetColumnExpand(columnIndex, isExpand);

            if (column == CommandTreeColumn.No)
            {
                _tree.SetColumnCustomMinimumWidth(columnIndex, Constant.Ui.Tree.CommandNoColumnMinWidth);
            }
            else if (column == CommandTreeColumn.State)
            {
                _tree.SetColumnCustomMinimumWidth(columnIndex, Constant.Ui.Tree.CommandStateColumnMinWidth);
            }
        }
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
        Application.Command.Event.StateNotified += OnStateNotified;
    }

    /// <summary>
    /// Applicationイベントの購読を解除する
    /// </summary>
    private void UnsubscribeApplicationEvents()
    {
        Application.Command.Event.StateNotified -= OnStateNotified;
    }

    /// <summary>
    /// コマンド履歴状態の通知を受け取ったときに呼び出されるイベントハンドラ
    /// </summary>
    /// <param name="history">通知された履歴配列</param>
    /// <param name="cursor">通知されたカーソル位置</param>
    private void OnStateNotified(CommandBase[] history, int cursor)
    {
        _history.Clear();
        if (history != null)
        {
            _history.AddRange(history);
        }

        _cursor = cursor;
        _isInitialized = true;
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
        if (nextCursor == _cursor)
        {
            return;
        }

        _isRequestingCursorMove = true;
        Application.Command.Event.SetCursor(nextCursor);
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

        try
        {
            _tree.Clear();
            TreeItem root = _tree.CreateItem();
            if (root == null)
            {
                return;
            }

            for (int i = 0; i < _history.Count; i++)
            {
                CommandBase command = _history[i];
                TreeItem item = _tree.CreateItem(root);
                if (item == null)
                {
                    continue;
                }

                item.SetMetadata((int)CommandTreeColumn.No, i);
                item.SetText((int)CommandTreeColumn.No, i.ToString());
                item.SetText((int)CommandTreeColumn.Name, command?.GetType().Name ?? "(null)");
                item.SetText((int)CommandTreeColumn.Description, command?.Description ?? string.Empty);

                CommandExecutionState state = ResolveState(i, _cursor);
                Color color = ResolveStateColor(i, _cursor);
                item.SetText((int)CommandTreeColumn.State, state.ToString());

                for (int column = 0; column < _tree.Columns; column++)
                {
                    item.SetCustomColor(column, color);
                }

                if (i == _cursor - 1)
                {
                    item.Select((int)CommandTreeColumn.No);
                }
            }
        }
        finally
        {
            _isUpdatingTree = false;
        }
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
