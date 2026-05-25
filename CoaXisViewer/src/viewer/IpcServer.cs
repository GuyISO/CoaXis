#nullable enable

using Godot;
using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public partial class IpcServer : Node
{
	[Signal] public delegate void MessageReceivedEventHandler(string message);
	[Signal] public delegate void ServerErrorEventHandler(string errorMessage);

	[ExportGroup("IPC")]
	[Export] private string _pipeName = "CoaXisViewer";
	[Export] private int _maxServerInstances = 1;
	[Export] private bool _autoStartOnReady = true;

	private readonly SemaphoreSlim _sendLock = new(1, 1);
	private readonly object _connectionLock = new();
	private CancellationTokenSource? _serverCancellation;
	private Task? _listenTask;
	private NamedPipeServerStream? _connectedPipe;
	private Action<string>? _receiveHandler;

	public bool IsRunning => _serverCancellation is { IsCancellationRequested: false };
	public bool IsClientConnected
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
			StartServer();
		}
	}

	public override void _ExitTree()
	{
		StopServer();
	}

	public void SetReceiveHandler(Action<string>? handler)
	{
		_receiveHandler = handler;
	}

	public void StartServer()
	{
		if (IsRunning)
		{
			return;
		}

		_serverCancellation = new CancellationTokenSource();
		_listenTask = ListenAsync(_serverCancellation.Token);
	}

	public void StopServer()
	{
		if (_serverCancellation == null)
		{
			return;
		}

		_serverCancellation.Cancel();
		_serverCancellation.Dispose();
		_serverCancellation = null;

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
		NamedPipeServerStream? pipe;
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
			GD.Print($"[IPC][Send] {message}");
			return true;
		}
		catch (OperationCanceledException)
		{
			return false;
		}
		catch (Exception exception)
		{
			CallDeferred(nameof(DispatchServerError), exception.Message);
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
			NamedPipeServerStream? server = null;
			try
			{
				server = new NamedPipeServerStream(
					_pipeName,
					PipeDirection.InOut,
					_maxServerInstances,
					PipeTransmissionMode.Byte,
					PipeOptions.Asynchronous);

				await server.WaitForConnectionAsync(cancellationToken);

				lock (_connectionLock)
				{
					_connectedPipe = server;
				}

				while (!cancellationToken.IsCancellationRequested && server.IsConnected)
				{
					string? message = await ReadMessageAsync(server, cancellationToken);
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
			catch (Exception exception)
			{
				CallDeferred(nameof(DispatchServerError), exception.Message);
			}
			finally
			{
				lock (_connectionLock)
				{
					if (ReferenceEquals(_connectedPipe, server))
					{
						_connectedPipe = null;
					}
				}

				server?.Dispose();
			}
		}
	}

	private void DispatchReceivedMessage(string message)
	{
		GD.Print($"[IPC][Recv] {message}");
		_receiveHandler?.Invoke(message);
		EmitSignal(SignalName.MessageReceived, message);
	}

	private void DispatchServerError(string errorMessage)
	{
		GD.PushError($"IPC server error: {errorMessage}");
		EmitSignal(SignalName.ServerError, errorMessage);
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
