using System.Buffers.Binary;
using System.IO.Pipes;
using System.Security.Cryptography;
using System.Text;

namespace NamedPipeTestServer;

public sealed class MainForm : Form
{
    private readonly TextBox _pipeNameTextBox;
    private readonly Button _connectButton;
    private readonly Label _statusLabel;
    private readonly TextBox _manualMessageTextBox;
    private readonly Button _sendManualButton;
    private readonly NumericUpDown _generatedLengthInput;
    private readonly Button _sendGeneratedButton;
    private readonly TextBox _receivedMessagesTextBox;
    private readonly Button _clearLogButton;

    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly object _pipeLock = new();
    private NamedPipeServerStream? _connectedPipe;
    private CancellationTokenSource? _listenCancellation;
    private Task? _listenTask;

    public MainForm()
    {
        Text = "NamedPipe Test Server";
        MinimumSize = new Size(720, 520);
        StartPosition = FormStartPosition.CenterScreen;

        var rootLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(12)
        };
        rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 38));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 20));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 42));
        Controls.Add(rootLayout);

        var connectionLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 4,
            AutoSize = true
        };
        connectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        connectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        connectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        connectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        connectionLayout.Controls.Add(new Label
        {
            Text = "Pipe Name",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 7, 8, 0)
        }, 0, 0);

        _pipeNameTextBox = new TextBox
        {
            Text = "CoaXisViewer",
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 8, 0)
        };
        connectionLayout.Controls.Add(_pipeNameTextBox, 1, 0);

        _connectButton = new Button
        {
            Text = "Start Server",
            AutoSize = true,
            Margin = new Padding(0, 0, 8, 0)
        };
        _connectButton.Click += StartStopButton_Click;
        connectionLayout.Controls.Add(_connectButton, 2, 0);

        _statusLabel = new Label
        {
            Text = "Server stopped",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            ForeColor = Color.Firebrick,
            Margin = new Padding(0, 7, 0, 0)
        };
        connectionLayout.Controls.Add(_statusLabel, 3, 0);
        rootLayout.Controls.Add(connectionLayout, 0, 0);

        var sendManualGroup = new GroupBox
        {
            Text = "Send specified text",
            Dock = DockStyle.Fill,
            MinimumSize = new Size(0, 140),
            Padding = new Padding(10)
        };
        var manualLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2
        };
        manualLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        manualLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        manualLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        manualLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        sendManualGroup.Controls.Add(manualLayout);

        _manualMessageTextBox = new TextBox
        {
            Multiline = true,
            Dock = DockStyle.Fill,
            ScrollBars = ScrollBars.Vertical
        };
        manualLayout.Controls.Add(_manualMessageTextBox, 0, 0);
        manualLayout.SetColumnSpan(_manualMessageTextBox, 2);

        _sendManualButton = new Button
        {
            Text = "Send",
            AutoSize = true,
            Anchor = AnchorStyles.Right,
            Enabled = false
        };
        _sendManualButton.Click += SendManualButton_Click;
        manualLayout.Controls.Add(_sendManualButton, 1, 1);
        rootLayout.Controls.Add(sendManualGroup, 0, 1);

        var generatedGroup = new GroupBox
        {
            Text = "Send auto-generated text",
            Dock = DockStyle.Fill,
            MinimumSize = new Size(0, 80),
            Padding = new Padding(10)
        };
        var generatedLayout = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true
        };
        generatedGroup.Controls.Add(generatedLayout);
        generatedLayout.Controls.Add(new Label
        {
            Text = "Length",
            AutoSize = true,
            Margin = new Padding(0, 8, 8, 0)
        });

        _generatedLengthInput = new NumericUpDown
        {
            Minimum = 1,
            Maximum = 100000,
            Value = 256,
            Width = 120
        };
        generatedLayout.Controls.Add(_generatedLengthInput);

        _sendGeneratedButton = new Button
        {
            Text = "Generate and Send",
            AutoSize = true,
            Enabled = false,
            Margin = new Padding(12, 3, 0, 0)
        };
        _sendGeneratedButton.Click += SendGeneratedButton_Click;
        generatedLayout.Controls.Add(_sendGeneratedButton);
        generatedLayout.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        rootLayout.Controls.Add(generatedGroup, 0, 2);

        var receiveGroup = new GroupBox
        {
            Text = "Received messages",
            Dock = DockStyle.Fill,
            Padding = new Padding(10)
        };
        var receiveLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2
        };
        receiveLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        receiveLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        receiveGroup.Controls.Add(receiveLayout);

        _receivedMessagesTextBox = new TextBox
        {
            Multiline = true,
            Dock = DockStyle.Fill,
            ReadOnly = true,
            ScrollBars = ScrollBars.Both,
            WordWrap = false
        };
        receiveLayout.Controls.Add(_receivedMessagesTextBox, 0, 0);

        _clearLogButton = new Button
        {
            Text = "Clear log",
            AutoSize = true,
            Anchor = AnchorStyles.Right
        };
        _clearLogButton.Click += (_, _) => _receivedMessagesTextBox.Clear();
        receiveLayout.Controls.Add(_clearLogButton, 0, 1);
        rootLayout.Controls.Add(receiveGroup, 0, 3);

        FormClosing += MainForm_FormClosing;
    }

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

    private async void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        await StopServerAsync();
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
}
