using Godot;

/// <summary>
/// IPC NamedPipe の状態を1単語で表示するラベル。
/// </summary>
public partial class PipeUi : Label
{
	#region Lifecycle

	public override void _Ready()
	{
		base._Ready();

		IpcHub ipc = Application.Ipc;
		ipc.PipeServerStatus += OnPipeServerStatus;
		ipc.ClientStatus += OnClientStatus;

		Refresh();
	}

	public override void _ExitTree()
	{
		IpcHub ipc = Application.Ipc;
		if (ipc != null)
		{
			ipc.PipeServerStatus -= OnPipeServerStatus;
			ipc.ClientStatus -= OnClientStatus;
		}

		base._ExitTree();
	}

	#endregion

	#region Events

	/// <summary>サーバー稼働状態の変化に追従して表示を更新する。</summary>
	private void OnPipeServerStatus(bool isRunning) => Refresh();

	/// <summary>クライアント接続状態の変化に追従して表示を更新する。</summary>
	private void OnClientStatus(bool isConnected) => Refresh();

	#endregion

	#region Helpers

	/// <summary>IpcHubの現在状態から表示文字列を決定する。</summary>
	private void Refresh()
	{
		IpcHub ipc = Application.Ipc;
		Text = !ipc.IsPipeServerRunning
			? "Pipe: Stopped"
			: ipc.IsClientConnected ? "Pipe: Connected" : "Pipe: Waiting";
	}

	#endregion
}
