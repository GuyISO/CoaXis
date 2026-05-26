using Godot;
using System;
using System.Threading.Tasks;

public partial class SendIpc : Button
{
	[Export] private NodePath _ipcClientPath = "../../IpcClient";
	[Export] private string _testMessage = "TEST_FROM_BUTTON";

	private IpcClient _ipcClient = null!;

	public override void _Ready()
	{
		_ipcClient = ResolveIpcClient();
		Pressed += OnPressed;
	}

	public override void _ExitTree()
	{
		Pressed -= OnPressed;
	}

	private async void OnPressed()
	{
		if (_ipcClient == null)
		{
			_ipcClient = ResolveIpcClient();
		}

		if (_ipcClient == null)
		{
			GD.PushWarning($"SendIpc: IpcClient not found. path='{_ipcClientPath}'");
			return;
		}

		string message = $"{_testMessage} [{DateTime.Now:yyyy-MM-dd HH:mm:ss}]";
		bool sent = await _ipcClient.SendMessageAsync(message);
		if (!sent)
		{
			GD.PushWarning("SendIpc: send failed. Check server connection state.");
		}
	}

	private IpcClient ResolveIpcClient()
	{
		IpcClient client = GetNodeOrNull<IpcClient>(_ipcClientPath);
		if (client != null)
		{
			return client;
		}

		Node root = GetTree().Root;
		return root.GetNodeOrNull<IpcClient>("Main/IpcClient");
	}
}
