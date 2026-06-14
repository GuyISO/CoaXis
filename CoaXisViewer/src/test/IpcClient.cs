#nullable enable

using Godot;
using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Editor 側 NamedPipe サーバーへ接続する Viewer 側 IPC クライアントです。
/// 送受信は length-prefix 形式（先頭 4byte に本文サイズ）で扱います。
/// </summary>
public partial class IpcClient : Node
{
	#region Signals

	[Signal] public delegate void MessageReceivedEventHandler(string message);
	[Signal] public delegate void ClientErrorEventHandler(string errorMessage);

	#endregion

	#region Fields

	[ExportGroup("IPC")]
	[Export] private string _pipeName = "CoaXisViewer";
	[Export] private int _connectTimeoutMilliseconds = 3000;
	[Export] private int _reconnectDelayMilliseconds = 1000;
	[Export] private bool _autoStartOnReady = true;

	private readonly SemaphoreSlim _sendLock = new(1, 1);
	private readonly object _connectionLock = new();
	private CancellationTokenSource? _clientCancellation;
	private Task? _listenTask;
	private NamedPipeClientStream? _connectedPipe;
	private Action<string>? _receiveHandler;

	#endregion

	#region Properties

	/// <summary>
	/// クライアントの受信ループが稼働中かを返します。
	/// </summary>
	public bool IsRunning => _clientCancellation is { IsCancellationRequested: false };

	/// <summary>
	/// NamedPipe サーバーとの接続が確立しているかを返します。
	/// </summary>
	public bool IsServerConnected
	{
		get
		{
			lock (_connectionLock)
			{
				return _connectedPipe?.IsConnected == true;
			}
		}
	}

	#endregion

	#region Lifecycle

	/// <summary>
	/// ノード初期化時に設定に応じて自動接続を開始します。
	/// </summary>
	public override void _Ready()
	{
		if (_autoStartOnReady)
		{
			StartClient();
		}
	}

	/// <summary>
	/// ノード破棄時に接続を停止し、リソースを解放します。
	/// </summary>
	public override void _ExitTree()
	{
		StopClient();
	}

	#endregion

	#region Public API

	/// <summary>
	/// 受信メッセージのハンドラを設定します。
	/// </summary>
	/// <param name="handler">受信時に呼び出す処理。不要な場合は <see langword="null"/>。</param>
	public void SetReceiveHandler(Action<string>? handler)
	{
		_receiveHandler = handler;
	}

	/// <summary>
	/// NamedPipe サーバーへの接続ループを開始します。
	/// </summary>
	public void StartClient()
	{
		if (IsRunning)
		{
			return;
		}

		_clientCancellation = new CancellationTokenSource();
		_listenTask = ListenAsync(_clientCancellation.Token);
	}

	/// <summary>
	/// 接続ループを停止し、保持中の接続を破棄します。
	/// </summary>
	public void StopClient()
	{
		if (_clientCancellation == null)
		{
			return;
		}

		_clientCancellation.Cancel();
		_clientCancellation.Dispose();
		_clientCancellation = null;

		lock (_connectionLock)
		{
			_connectedPipe?.Dispose();
			_connectedPipe = null;
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

	/// <summary>
	/// 文字列メッセージを length-prefix 形式で送信します。
	/// </summary>
	/// <param name="message">送信する本文文字列。</param>
	/// <param name="cancellationToken">キャンセル用トークン。</param>
	/// <returns>送信成功時は <see langword="true"/>、失敗時は <see langword="false"/>。</returns>
	public async Task<bool> SendMessageAsync(string message, CancellationToken cancellationToken = default)
	{
		NamedPipeClientStream? pipe;
		lock (_connectionLock)
		{
			pipe = _connectedPipe;
		}

		if (pipe == null || !pipe.IsConnected)
		{
			return false;
		}

		await _sendLock.WaitAsync(cancellationToken);
		try
		{
			if (!pipe.IsConnected)
			{
				return false;
			}

			await WriteMessageAsync(pipe, message, cancellationToken);
			LogHub.I.Info($"[IPC][Send] {message}");
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
		finally
		{
			_sendLock.Release();
		}
	}

	/// <summary>
	/// command/payload 形式の JSON エンベロープを作成して送信します。
	/// </summary>
	/// <param name="command">IPC コマンドまたはイベント名。</param>
	/// <param name="payload">任意のペイロード。<see langword="null"/> 可。</param>
	/// <param name="cancellationToken">キャンセル用トークン。</param>
	/// <returns>送信成功時は <see langword="true"/>、失敗時は <see langword="false"/>。</returns>
	public Task<bool> SendEnvelopeAsync(string command, object? payload = null, CancellationToken cancellationToken = default)
	{
		string envelope = JsonSerializer.Serialize(new
		{
			command,
			payload
		});

		return SendMessageAsync(envelope, cancellationToken);
	}

	#endregion

	#region Internal Helpers

	// 接続断・タイムアウトを許容しつつ、キャンセルされるまで再接続ループを継続する。
	private async Task ListenAsync(CancellationToken cancellationToken)
	{
		while (!cancellationToken.IsCancellationRequested)
		{
			NamedPipeClientStream? client = null;
			try
			{
				client = new NamedPipeClientStream(
					".",
					_pipeName,
					PipeDirection.InOut,
					PipeOptions.Asynchronous);

				using (CancellationTokenSource timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
				{
					timeoutCts.CancelAfter(Math.Max(1, _connectTimeoutMilliseconds));
					await client.ConnectAsync(timeoutCts.Token);
				}

				lock (_connectionLock)
				{
					_connectedPipe = client;
				}

				LogHub.I.Info($"[IPC] Connected to server: {_pipeName}");

				while (!cancellationToken.IsCancellationRequested && client.IsConnected)
				{
					string? message = await ReadMessageAsync(client, cancellationToken);
					if (message == null)
					{
						break;
					}

					CallDeferred(nameof(DispatchReceivedMessage), message);
				}
			}
			catch (OperationCanceledException)
			{
				break;
			}
			catch (TimeoutException)
			{
				// Retry until the host app starts listening.
			}
			catch (Exception exception)
			{
				CallDeferred(nameof(DispatchClientError), exception.Message);
			}
			finally
			{
				lock (_connectionLock)
				{
					if (ReferenceEquals(_connectedPipe, client))
					{
						_connectedPipe = null;
					}
				}

				client?.Dispose();
			}

			if (!cancellationToken.IsCancellationRequested)
			{
				try
				{
					await Task.Delay(Math.Max(1, _reconnectDelayMilliseconds), cancellationToken);
				}
				catch (OperationCanceledException)
				{
					break;
				}
			}
		}
	}

	// 受信時の共通入口。ログ・コールバック・シグナル通知の順を固定して観測性を維持する。
	private void DispatchReceivedMessage(string message)
	{
		LogHub.I.Info($"[IPC][Recv] {message}");
		_receiveHandler?.Invoke(message);
		EmitSignal(SignalName.MessageReceived, message);
	}

	// エラー通知の経路を一本化し、UIとログの不整合を防ぐ。
	private void DispatchClientError(string errorMessage)
	{
		LogHub.I.Error($"IPC client error: {errorMessage}");
		EmitSignal(SignalName.ClientError, errorMessage);
	}

	// length-prefix から本文を復元する。ストリーム終端時は null を返して上位で再接続へ遷移する。
	private static async Task<string?> ReadMessageAsync(PipeStream pipe, CancellationToken cancellationToken)
	{
		byte[] lengthBuffer = new byte[sizeof(int)];
		bool hasMessage = await TryReadExactAsync(pipe, lengthBuffer, cancellationToken);
		if (!hasMessage)
		{
			return null;
		}

		int messageLength = BinaryPrimitives.ReadInt32LittleEndian(lengthBuffer);
		if (messageLength < 0)
		{
			throw new InvalidDataException("Received negative message length.");
		}

		if (messageLength == 0)
		{
			return string.Empty;
		}

		byte[] messageBuffer = new byte[messageLength];
		await ReadExactAsync(pipe, messageBuffer, cancellationToken);
		return Encoding.UTF8.GetString(messageBuffer);
	}

	// 先頭4byteに本文長を付けて書き込み、受信側が境界を判定できるようにする。
	private static async Task WriteMessageAsync(PipeStream pipe, string message, CancellationToken cancellationToken)
	{
		byte[] payload = Encoding.UTF8.GetBytes(message);
		byte[] lengthBuffer = new byte[sizeof(int)];
		BinaryPrimitives.WriteInt32LittleEndian(lengthBuffer, payload.Length);

		await pipe.WriteAsync(lengthBuffer, cancellationToken);
		if (payload.Length > 0)
		{
			await pipe.WriteAsync(payload, cancellationToken);
		}

		await pipe.FlushAsync(cancellationToken);
	}

	// 指定サイズの読み取りを完了するまで待機する。先頭で EOF の場合のみ「メッセージなし」として扱う。
	private static async Task<bool> TryReadExactAsync(Stream stream, byte[] buffer, CancellationToken cancellationToken)
	{
		int offset = 0;
		while (offset < buffer.Length)
		{
			int readLength = await stream.ReadAsync(buffer.AsMemory(offset, buffer.Length - offset), cancellationToken);
			if (readLength == 0)
			{
				if (offset == 0)
				{
					return false;
				}

				throw new EndOfStreamException("Pipe closed while reading a message.");
			}

			offset += readLength;
		}

		return true;
	}

	// 途中 EOF を異常として扱い、上位へ明示的に伝播する。
	private static async Task ReadExactAsync(Stream stream, byte[] buffer, CancellationToken cancellationToken)
	{
		bool completed = await TryReadExactAsync(stream, buffer, cancellationToken);
		if (!completed)
		{
			throw new EndOfStreamException("Pipe closed before the full message was received.");
		}
	}

	#endregion
}



