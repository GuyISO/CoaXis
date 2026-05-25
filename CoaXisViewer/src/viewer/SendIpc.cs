using Godot;
using System;
using System.Threading.Tasks;

public partial class SendIpc : Button
{
	[Export] private NodePath _ipcServerPath = "../../IpcServer";
	[Export] private string _testMessage = "TEST_FROM_BUTTON";

	private IpcServer _ipcServer = null!;

	public override void _Ready()
	{
		_ipcServer = ResolveIpcServer();
		Pressed += OnPressed;
	}

	public override void _ExitTree()
	{
		Pressed -= OnPressed;
	}

	private async void OnPressed()
	{
		if (_ipcServer == null)
		{
			_ipcServer = ResolveIpcServer();
		}

		if (_ipcServer == null)
		{
			GD.PushWarning($"SendIpc: IpcServer not found. path='{_ipcServerPath}'");
			return;
		}

		string message = $"{_testMessage} [{DateTime.Now:yyyy-MM-dd HH:mm:ss}]";
		bool sent = await _ipcServer.SendMessageAsync(message);
		if (!sent)
		{
			GD.PushWarning("SendIpc: send failed. Check Form connection state.");
		}
	}

	private IpcServer ResolveIpcServer()
	{
		IpcServer server = GetNodeOrNull<IpcServer>(_ipcServerPath);
		if (server != null)
		{
			return server;
		}

		Node root = GetTree().Root;
		return root.GetNodeOrNull<IpcServer>("Main/IpcServer");
	}
}
