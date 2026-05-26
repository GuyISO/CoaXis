#nullable enable

using Godot;
using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public partial class IpcClient : Node
{
	[Signal] public delegate void MessageReceivedEventHandler(string message);
	[Signal] public delegate void ClientErrorEventHandler(string errorMessage);

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

	public bool IsRunning => _clientCancellation is { IsCancellationRequested: false };
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

	public override void _Ready()
	{
		if (_autoStartOnReady)
		{
			StartClient();
		}
	}

	public override void _ExitTree()
	{
		StopClient();
	}

	public void SetReceiveHandler(Action<string>? handler)
	{
		_receiveHandler = handler;
	}

	public void StartClient()
	{
		if (IsRunning)
		{
			return;
		}

		_clientCancellation = new CancellationTokenSource();
		_listenTask = ListenAsync(_clientCancellation.Token);
	}

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

	public void SendMessage(string message)
	{
		_ = SendMessageAsync(message);
	}

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
			DebugConsole.Log($"[IPC][Send] {message}");
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

				DebugConsole.Log($"[IPC] Connected to server: {_pipeName}");

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

	private void DispatchReceivedMessage(string message)
	{
		DebugConsole.Log($"[IPC][Recv] {message}");
		_receiveHandler?.Invoke(message);
		EmitSignal(SignalName.MessageReceived, message);
	}

	private void DispatchClientError(string errorMessage)
	{
		DebugConsole.Log($"IPC client error: {errorMessage}");
		EmitSignal(SignalName.ClientError, errorMessage);
	}

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

	private static async Task ReadExactAsync(Stream stream, byte[] buffer, CancellationToken cancellationToken)
	{
		bool completed = await TryReadExactAsync(stream, buffer, cancellationToken);
		if (!completed)
		{
			throw new EndOfStreamException("Pipe closed before the full message was received.");
		}
	}
}
