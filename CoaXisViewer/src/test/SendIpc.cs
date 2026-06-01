using Godot;
using System;

/// <summary>
/// テスト用に IPC メッセージを送信するボタンコンポーネントです。
/// </summary>
public partial class SendIpc : Button
{
	#region Fields

	[Export] private NodePath _ipcClientPath = "../../IpcClient";
	[Export] private string _testMessage = "TEST_FROM_BUTTON";

	private IpcClient _ipcClient = null!;

	#endregion

	#region Lifecycle

	/// <summary>
	/// 初期化時に IPC クライアント参照を解決し、クリックイベントを購読します。
	/// </summary>
	public override void _Ready()
	{
		_ipcClient = ResolveIpcClient();
		Pressed += OnPressed;
	}

	/// <summary>
	/// 終了時にクリックイベント購読を解除します。
	/// </summary>
	public override void _ExitTree()
	{
		Pressed -= OnPressed;
	}

	#endregion

	#region Events

	// ボタン押下時は接続の再解決を試み、送信失敗時は原因切り分けしやすい警告を残す。
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

	#endregion

	#region Internal Helpers

	// シーン構成差分に耐えるため、Export 指定パスと Main 直下の両方で IpcClient を探索する。
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

	#endregion
}



