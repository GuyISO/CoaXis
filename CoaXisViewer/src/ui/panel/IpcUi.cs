using Godot;

/// <summary>IPCのパイプ状態と送受信履歴を表示するパネル。</summary>
public partial class IpcUi : PanelContainer
{
    #region Fields

    private Label _pipeName = null!;
    private Label _status = null!;
    private Tree _history = null!;

    #endregion

    #region Lifecycle

    public override void _Ready()
    {
        EnsureChildNodes();
        SubscribeApplicationEvents();
        RefreshStatus();
        RefreshHistory();
    }

    public override void _ExitTree()
    {
        UnsubscribeApplicationEvents();

        base._ExitTree();
    }

    #endregion

    #region Events

    private void EnsureChildNodes()
    {
        _pipeName = GetNode<Label>("MarginContainer/VBoxContainer/PipeName");
        _status = GetNode<Label>("MarginContainer/VBoxContainer/Status");
        _history = GetNode<Tree>("MarginContainer/VBoxContainer/History");
        _history.Columns = 3;
        _history.SetColumnTitle(0, "Time");
        _history.SetColumnTitle(1, "Direction");
        _history.SetColumnTitle(2, "Event");
        _history.SetColumnExpand(0, false);
        _history.SetColumnExpand(1, false);
        _history.SetColumnCustomMinimumWidth(0, 100);
        _history.SetColumnCustomMinimumWidth(1, 80);
    }

    private void SubscribeApplicationEvents()
    {
        Application.Ipc.PipeServerStatus += OnPipeServerStatus;
        Application.Ipc.ClientStatus += OnClientStatus;
        Application.Ipc.HistoryNotified += OnHistoryNotified;
    }

    private void UnsubscribeApplicationEvents()
    {
        Application.Ipc.PipeServerStatus -= OnPipeServerStatus;
        Application.Ipc.ClientStatus -= OnClientStatus;
        Application.Ipc.HistoryNotified -= OnHistoryNotified;
    }

    private void OnPipeServerStatus(bool isRunning) => RefreshStatus();

    private void OnClientStatus(bool isConnected) => RefreshStatus();

    private void OnHistoryNotified() => RefreshHistory();

    #endregion

    #region Helpers

    private void RefreshStatus()
    {
        IpcHub ipc = Application.Ipc;
        _pipeName.Text = $"Pipe: {ipc.PipeName}";
        _status.Text = !ipc.IsPipeServerRunning
            ? "Status: Stopped"
            : ipc.IsClientConnected ? "Status: Connected" : "Status: Waiting";
    }

    private void RefreshHistory()
    {
        _history.Clear();
        TreeItem root = _history.CreateItem();
        TreeItem last = null;
        foreach (IpcHub.HistoryEntry entry in Application.Ipc.History)
        {
            last = _history.CreateItem(root);
            last.SetText(0, entry.Timestamp.ToString("HH:mm:ss.fff"));
            last.SetText(1, entry.IsSent ? "Send" : "Receive");
            last.SetText(2, string.IsNullOrWhiteSpace(entry.EventType) ? "(unknown)" : entry.EventType);
        }

        if (last != null)
        {
            _history.ScrollToItem(last);
        }
    }

    #endregion
}
