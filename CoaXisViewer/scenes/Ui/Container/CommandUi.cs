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

    // 関連ノードのキャッシュ
    private Tree _tree = null!;

    private static readonly Color DoColor = Colors.White;
    private static readonly Color UndoColor = Colors.Gray;

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
        _tree.Columns = 4;
        _tree.SetColumnTitle(0, "No");
        _tree.SetColumnTitle(1, "Name");
        _tree.SetColumnTitle(2, "Description");
        _tree.SetColumnTitle(3, "State");
        _tree.SetColumnExpand(0, false);
        _tree.SetColumnExpand(1, true);
        _tree.SetColumnExpand(2, true);
        _tree.SetColumnExpand(3, false);
        _tree.SetColumnCustomMinimumWidth(0, 64);
        _tree.SetColumnCustomMinimumWidth(3, 64);
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

                item.SetMetadata(0, i);
                item.SetText(0, i.ToString());
                item.SetText(1, command?.GetType().Name ?? "(null)");
                item.SetText(2, command?.Description ?? string.Empty);

                string state = ResolveState(i, _cursor);
                Color color = ResolveStateColor(i, _cursor);
                item.SetText(3, state);

                for (int column = 0; column < 4; column++)
                {
                    item.SetCustomColor(column, color);
                }

                if (i == _cursor - 1)
                {
                    item.Select(0);
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
    private static string ResolveState(int index, int cursor)
    {
        if (index < cursor)
        {
            return "Do";
        }

        return "Undo";
    }

    /// <summary>
    /// 履歴インデックスに対応する表示色を返す
    /// </summary>
    private static Color ResolveStateColor(int index, int cursor)
    {
        if (index < cursor)
        {
            return DoColor;
        }

        return UndoColor;
    }

    #endregion
}
