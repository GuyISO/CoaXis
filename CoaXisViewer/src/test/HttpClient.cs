#nullable enable

using Godot;
using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public partial class HttpClient : Node
{
	[Signal] public delegate void MessageReceivedEventHandler(string message);
	[Signal] public delegate void ClientErrorEventHandler(string errorMessage);

	[ExportGroup("HTTP")]
	[Export] private string _serverBaseUrl = "http://localhost:8088/";
	[Export] private string _endpointPath = "api/echo";
	[Export] private int _requestTimeoutMilliseconds = 5000;

	private global::System.Net.Http.HttpClient? _httpClient;

	public override void _Ready()
	{
		_httpClient = new global::System.Net.Http.HttpClient
		{
			Timeout = TimeSpan.FromMilliseconds(Math.Max(1, _requestTimeoutMilliseconds))
		};
	}

	public override void _ExitTree()
	{
		_httpClient?.Dispose();
		_httpClient = null;
	}

	public async Task<bool> SendMessageAsync(string message, CancellationToken cancellationToken = default)
	{
		if (_httpClient == null)
		{
			CallDeferred(nameof(DispatchClientError), "HTTP client is not initialized.");
			return false;
		}

		if (string.IsNullOrWhiteSpace(_serverBaseUrl))
		{
			CallDeferred(nameof(DispatchClientError), "Server base URL is empty.");
			return false;
		}

		try
		{
			Uri endpoint = BuildEndpointUri();
			using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
			{
				Content = new StringContent(message, Encoding.UTF8, "text/plain")
			};
			request.Headers.Add("X-CoaXis-Client", "GodotHttpPoC");

			using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
			string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

			if (!response.IsSuccessStatusCode)
			{
				CallDeferred(nameof(DispatchClientError), $"HTTP {(int)response.StatusCode}: {responseBody}");
				return false;
			}

			CallDeferred(nameof(DispatchReceivedMessage), responseBody);
			DebugConsole.Log($"[HTTP][Send] {message}");
			DebugConsole.Log($"[HTTP][Recv] {responseBody}");
			return true;
		}
		catch (OperationCanceledException)
		{
			return false;
		}
		catch (Exception exception)
		{
			CallDeferred(nameof(DispatchClientError), exception.Message);
			return false;
		}
	}

	public void SendMessage(string message)
	{
		_ = SendMessageAsync(message);
	}

	private Uri BuildEndpointUri()
	{
		string baseUrl = _serverBaseUrl.Trim();
		if (!baseUrl.EndsWith("/"))
		{
			baseUrl += "/";
		}

		string endpoint = _endpointPath.TrimStart('/');
		return new Uri(new Uri(baseUrl), endpoint);
	}

	private void DispatchReceivedMessage(string message)
	{
		EmitSignal(SignalName.MessageReceived, message);
	}

	private void DispatchClientError(string errorMessage)
	{
		GD.PushError($"HTTP client error: {errorMessage}");
		EmitSignal(SignalName.ClientError, errorMessage);
	}
}
