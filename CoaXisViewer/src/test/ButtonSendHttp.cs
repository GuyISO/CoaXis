using Godot;
using System;
using System.Threading.Tasks;

public partial class ButtonSendHttp : Button
{
	[Export] private NodePath _httpClientPath = "../../../../HttpClient";
	[Export] private string _testMessage = "HTTP_TEST_FROM_BUTTON";

	private HttpClient _httpClient = null!;

	public override void _Ready()
	{
		_httpClient = ResolveHttpClient();
		Pressed += OnPressed;
	}

	public override void _ExitTree()
	{
		Pressed -= OnPressed;
	}

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
}
