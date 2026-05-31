using System.Buffers.Binary;
using System.IO.Pipes;
using System.Security.Cryptography;
using System.Text;

namespace NamedPipeTestServer;

public sealed partial class MainForm : Form
{
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly object _pipeLock = new();
    private NamedPipeServerStream? _connectedPipe;
    private CancellationTokenSource? _listenCancellation;
    private Task? _listenTask;

    #region Lifecycle

    public MainForm()
    {
        InitializeComponent();
    }

    #endregion

    #region Properties

    private bool IsServerRunning => _listenCancellation is { IsCancellationRequested: false };

    private bool IsConnected
    {
        get
        {
            lock (_pipeLock)
            {
                return _connectedPipe?.IsConnected == true;
            }
        }
    }

    #endregion

    #region Event Handlers

    private async void StartStopButton_Click(object? sender, EventArgs e)
    {
        if (IsServerRunning)
        {
            await StopServerAsync();
            return;
        }

        string pipeName = _pipeNameTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(pipeName))
        {
            MessageBox.Show(this, "Pipe Name を入力してください。", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _connectButton.Enabled = false;
        SetStatus("Starting server...", Color.DarkGoldenrod);

        try
        {
            _listenCancellation = new CancellationTokenSource();
            _listenTask = ListenAsync(pipeName, _listenCancellation.Token);

            SetServerState(true, false);
            AppendLog($"Server started: {pipeName}");
        }
        catch (Exception exception)
        {
            AppendLog($"Server start error: {exception.Message}");
            _listenCancellation?.Dispose();
            _listenCancellation = null;
            SetServerState(false, false);
        }
        finally
        {
            _connectButton.Enabled = true;
        }
    }

    private async void SendManualButton_Click(object? sender, EventArgs e)
    {
        string message = _manualMessageTextBox.Text;
        if (message.Length == 0)
        {
            MessageBox.Show(this, "送信文字列を入力してください。", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        await SendAndReportAsync(message, "manual");
    }

    private async void SendGeneratedButton_Click(object? sender, EventArgs e)
    {
        string message = GenerateRandomText((int)_generatedLengthInput.Value);
        await SendAndReportAsync(message, $"generated({message.Length})");
    }

    private async void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        await StopServerAsync();
    }

    #endregion

    #region Pipe Operations

    private async Task SendAndReportAsync(string message, string category)
    {
        NamedPipeServerStream? pipe;
        lock (_pipeLock)
        {
            pipe = _connectedPipe;
        }

        if (pipe == null || !pipe.IsConnected)
        {
            AppendLog("Send skipped: no client connected.");
            return;
        }

        try
        {
            await _sendLock.WaitAsync();
            await WriteMessageAsync(pipe, message, CancellationToken.None);
            AppendLog($"Sent {category}: length={message.Length}");
        }
        catch (Exception exception)
        {
            AppendLog($"Send error: {exception.Message}");
        }
        finally
        {
            _sendLock.Release();
        }
    }

    private async Task ListenAsync(string pipeName, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                NamedPipeServerStream? server = null;
                try
                {
                    server = new NamedPipeServerStream(
                        pipeName,
                        PipeDirection.InOut,
                        1,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous);

                    lock (_pipeLock)
                    {
                        _connectedPipe = server;
                    }

                    AppendLog("Waiting for Godot client connection...");
                    UpdateConnectionState(true, false);
                    await server.WaitForConnectionAsync(cancellationToken);

                    AppendLog("Godot client connected.");
                    UpdateConnectionState(true, true);

                    while (!cancellationToken.IsCancellationRequested && server.IsConnected)
                    {
                        string? message = await ReadMessageAsync(server, cancellationToken);
                        if (message == null)
                        {
                            break;
                        }

                        AppendLog($"Received: length={message.Length}");
                        AppendLog(message);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception exception)
                {
                    AppendLog($"Receive error: {exception.Message}");
                }
                finally
                {
                    lock (_pipeLock)
                    {
                        if (ReferenceEquals(_connectedPipe, server))
                        {
                            _connectedPipe = null;
                        }
                    }

                    server?.Dispose();

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        AppendLog("Godot client disconnected.");
                        UpdateConnectionState(true, false);
                    }
                }

            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            AppendLog($"Receive error: {exception.Message}");
        }
        finally
        {
            UpdateConnectionState(false, false);
        }
    }

    private async Task StopServerAsync()
    {
        _listenCancellation?.Cancel();
        _listenCancellation?.Dispose();
        _listenCancellation = null;

        lock (_pipeLock)
        {
            _connectedPipe?.Dispose();
            _connectedPipe = null;
        }

        if (_listenTask != null)
        {
            try
            {
                await _listenTask;
            }
            catch
            {
            }
            _listenTask = null;
        }

        SetServerState(false, false);
        AppendLog("Server stopped");
    }

    #endregion

    #region UI Helpers

    private void UpdateConnectionState(bool serverRunning, bool connected)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => SetServerState(serverRunning, connected));
            return;
        }

        SetServerState(serverRunning, connected);
    }

    private void SetServerState(bool serverRunning, bool connected)
    {
        _connectButton.Text = serverRunning ? "Stop Server" : "Start Server";
        _sendManualButton.Enabled = connected;
        _sendGeneratedButton.Enabled = connected;
        _pipeNameTextBox.Enabled = !serverRunning;

        if (!serverRunning)
        {
            SetStatus("Server stopped", Color.Firebrick);
            return;
        }

        SetStatus(
            connected ? "Client connected" : "Waiting client",
            connected ? Color.ForestGreen : Color.DarkGoldenrod);
    }

    private void SetStatus(string text, Color color)
    {
        _statusLabel.Text = text;
        _statusLabel.ForeColor = color;
    }

    private void AppendLog(string text)
    {
        string line = $"[{DateTime.Now:HH:mm:ss}] {text}{Environment.NewLine}";

        if (InvokeRequired)
        {
            BeginInvoke(() => _receivedMessagesTextBox.AppendText(line));
            return;
        }

        _receivedMessagesTextBox.AppendText(line);
    }

    #endregion

    #region Static Helpers

    private static string GenerateRandomText(int length)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        char[] buffer = new char[length];
        byte[] randomBytes = RandomNumberGenerator.GetBytes(length);

        for (int index = 0; index < length; index++)
        {
            buffer[index] = alphabet[randomBytes[index] % alphabet.Length];
        }

        return new string(buffer);
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
            throw new InvalidDataException("Negative message length was received.");
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

                throw new EndOfStreamException("Pipe closed while reading the message.");
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
            throw new EndOfStreamException("Pipe closed before the full message was read.");
        }
    }

    #endregion
}
