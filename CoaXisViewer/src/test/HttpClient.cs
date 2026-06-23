#nullable enable

using Godot;
using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Viewer から HTTP テストサーバーへメッセージ送信を行うクライアントです。
/// </summary>
public partial class HttpClient : Node
{
	#region Signals

	[Signal] public delegate void MessageReceivedEventHandler(string message);
	[Signal] public delegate void ClientErrorEventHandler(string errorMessage);

	#endregion

	#region Fields

	[ExportGroup("HTTP")]
	[Export] private string _serverBaseUrl = "http://localhost:8088/";
	[Export] private string _endpointPath = "api/echo";
	[Export] private int _requestTimeoutMilliseconds = 5000;

	private global::System.Net.Http.HttpClient? _httpClient;

	#endregion

	#region Lifecycle

	/// <summary>
	/// ノード初期化時に HTTP クライアントを生成します。
	/// </summary>
	public override void _Ready()
	{
		_httpClient = new global::System.Net.Http.HttpClient
		{
			Timeout = TimeSpan.FromMilliseconds(Math.Max(1, _requestTimeoutMilliseconds))
		};
	}

	/// <summary>
	/// ノード破棄時に HTTP クライアントを破棄します。
	/// </summary>
	public override void _ExitTree()
	{
		_httpClient?.Dispose();
		_httpClient = null;
	}

	#endregion

	#region Public API

	/// <summary>
	/// テストサーバーへメッセージを送信し、応答を受信します。
	/// </summary>
	/// <param name="message">送信する本文文字列。</param>
	/// <param name="cancellationToken">キャンセル用トークン。</param>
	/// <returns>送信と応答受信が成功した場合は <see langword="true"/>。</returns>
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
			LogHub.Debug($"[HTTP][Send] {message}");
			LogHub.Debug($"[HTTP][Recv] {responseBody}");
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

	/// <summary>
	/// 文字列メッセージを非同期送信します（fire-and-forget）。
	/// </summary>
	/// <param name="message">送信する本文文字列。</param>
	public void SendMessage(string message)
	{
		_ = SendMessageAsync(message);
	}

	#endregion

	#region Internal Helpers

	// ベースURLとエンドポイントを安全に連結し、末尾/先頭スラッシュ差異による誤URL生成を防ぐ。
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

	// Godot メインスレッド側でシグナル通知するため、受信通知は dispatch メソッドへ集約する。
	private void DispatchReceivedMessage(string message)
	{
		EmitSignal(SignalName.MessageReceived, message);
	}

	// エラー通知の経路を統一し、ログ表示とシグナル発火の順序を固定する。
	private void DispatchClientError(string errorMessage)
	{
		GD.PushError($"HTTP client error: {errorMessage}");
		EmitSignal(SignalName.ClientError, errorMessage);
	}

	#endregion
}



