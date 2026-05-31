using Godot;
using System;

/// <summary>
/// テスト用に HTTP メッセージを送信するボタンコンポーネントです。
/// </summary>
public partial class ButtonSendHttp : Button
{
	#region Fields

	[Export] private NodePath _httpClientPath = "../../../../HttpClient";
	[Export] private string _testMessage = "HTTP_TEST_FROM_BUTTON";

	private HttpClient _httpClient = null!;

	#endregion

	#region Lifecycle

	/// <summary>
	/// 初期化時に HTTP クライアント参照を解決し、クリックイベントを購読します。
	/// </summary>
	public override void _Ready()
	{
		_httpClient = ResolveHttpClient();
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

	#region Internal Helpers

	// ボタン押下時は接続の再解決を試み、通信失敗時に運用側が原因把握しやすい警告を出す。
	private async void OnPressed()
	{
		if (_httpClient == null)
		{
			_httpClient = ResolveHttpClient();
		}

		if (_httpClient == null)
		{
			GD.PushWarning($"ButtonSendHttp: HttpClient not found. path='{_httpClientPath}'");
			return;
		}

		string message = $"{_testMessage} [{DateTime.Now:yyyy-MM-dd HH:mm:ss}]";
		bool sent = await _httpClient.SendMessageAsync(message);
		if (!sent)
		{
			GD.PushWarning("ButtonSendHttp: send failed. Check HTTP server availability and endpoint settings.");
		}
	}

	#endregion

	#region Internal Helpers

	// シーン配置差分に対応するため、Export の NodePath と Main 直下のフォールバックで解決する。
	private HttpClient ResolveHttpClient()
	{
		HttpClient client = GetNodeOrNull<HttpClient>(_httpClientPath);
		if (client != null)
		{
			return client;
		}

		Node root = GetTree().Root;
		return root.GetNodeOrNull<HttpClient>("Main/HttpClient");
	}

	#endregion
}



