using System.Net;
using System.Text;

namespace HttpTestServer;

public sealed class MainForm : Form
{
    private readonly TextBox _hostTextBox;
    private readonly NumericUpDown _portInput;
    private readonly TextBox _pathTextBox;
    private readonly Button _startStopButton;
    private readonly Label _statusLabel;
    private readonly TextBox _responseTemplateTextBox;
    private readonly TextBox _logTextBox;
    private readonly Button _clearLogButton;

    private CancellationTokenSource? _listenCancellation;
    private Task? _listenTask;
    private HttpListener? _listener;

    public MainForm()
    {
        Text = "HTTP Test Server";
        MinimumSize = new Size(780, 560);
        StartPosition = FormStartPosition.CenterScreen;

        var rootLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(12)
        };
        rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(rootLayout);

        var connectionLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 9,
            AutoSize = true
        };
        connectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        connectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
        connectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        connectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        connectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        connectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
        connectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        connectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        connectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        connectionLayout.Controls.Add(new Label
        {
            Text = "Host",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 7, 8, 0)
        }, 0, 0);

        _hostTextBox = new TextBox
        {
            Text = "localhost",
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 8, 0)
        };
        connectionLayout.Controls.Add(_hostTextBox, 1, 0);

        connectionLayout.Controls.Add(new Label
        {
            Text = "Port",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 7, 8, 0)
        }, 2, 0);

        _portInput = new NumericUpDown
        {
            Minimum = 1,
            Maximum = 65535,
            Value = 8088,
            Width = 100,
            Margin = new Padding(0, 0, 8, 0)
        };
        connectionLayout.Controls.Add(_portInput, 3, 0);

        connectionLayout.Controls.Add(new Label
        {
            Text = "Path",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 7, 8, 0)
        }, 4, 0);

        _pathTextBox = new TextBox
        {
            Text = "api/echo",
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 8, 0)
        };
        connectionLayout.Controls.Add(_pathTextBox, 5, 0);

        _startStopButton = new Button
        {
            Text = "Start Server",
            AutoSize = true,
            Margin = new Padding(0, 0, 8, 0)
        };
        _startStopButton.Click += StartStopButton_Click;
        connectionLayout.Controls.Add(_startStopButton, 6, 0);

        _statusLabel = new Label
        {
            Text = "Stopped",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            ForeColor = Color.Firebrick,
            Margin = new Padding(0, 7, 8, 0)
        };
        connectionLayout.Controls.Add(_statusLabel, 7, 0);

        rootLayout.Controls.Add(connectionLayout, 0, 0);

        var responseGroup = new GroupBox
        {
            Text = "Response template",
            Dock = DockStyle.Top,
            Padding = new Padding(10),
            Height = 120
        };
        _responseTemplateTextBox = new TextBox
        {
            Multiline = true,
            Dock = DockStyle.Fill,
            ScrollBars = ScrollBars.Vertical,
            Text = "RECEIVED: {request}"
        };
        responseGroup.Controls.Add(_responseTemplateTextBox);
        rootLayout.Controls.Add(responseGroup, 0, 1);

        var logGroup = new GroupBox
        {
            Text = "Request / Response log",
            Dock = DockStyle.Fill,
            Padding = new Padding(10)
        };
        var logLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2
        };
        logLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        logLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        logGroup.Controls.Add(logLayout);

        _logTextBox = new TextBox
        {
            Multiline = true,
            Dock = DockStyle.Fill,
            ReadOnly = true,
            ScrollBars = ScrollBars.Both,
            WordWrap = false
        };
        logLayout.Controls.Add(_logTextBox, 0, 0);

        _clearLogButton = new Button
        {
            Text = "Clear log",
            AutoSize = true,
            Anchor = AnchorStyles.Right
        };
        _clearLogButton.Click += (_, _) => _logTextBox.Clear();
        logLayout.Controls.Add(_clearLogButton, 0, 1);
        rootLayout.Controls.Add(logGroup, 0, 2);

        FormClosing += MainForm_FormClosing;
    }

    private bool IsServerRunning => _listenCancellation is { IsCancellationRequested: false };

    private async void StartStopButton_Click(object? sender, EventArgs e)
    {
        if (IsServerRunning)
        {
            await StopServerAsync();
            return;
        }

        string host = _hostTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(host))
        {
            MessageBox.Show(this, "Host を入力してください。", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        string path = NormalizePath(_pathTextBox.Text);
        string prefix = BuildPrefix(host, (int)_portInput.Value, path);

        _startStopButton.Enabled = false;
        SetStatus("Starting...", Color.DarkGoldenrod);

        try
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add(prefix);
            _listener.Start();

            _listenCancellation = new CancellationTokenSource();
            _listenTask = ListenAsync(_listener, _listenCancellation.Token);

            _hostTextBox.Enabled = false;
            _portInput.Enabled = false;
            _pathTextBox.Enabled = false;
            _startStopButton.Text = "Stop Server";
            SetStatus("Running", Color.ForestGreen);

            AppendLog($"Server started: {prefix}");
            AppendLog("Tip: LAN待受にする場合は Host に '+' かPC名/IPを設定。必要なら URL ACL を追加してください。");
        }
        catch (Exception exception)
        {
            AppendLog($"Server start error: {exception.Message}");
            _listener?.Close();
            _listener = null;
            _listenCancellation?.Dispose();
            _listenCancellation = null;
            SetStoppedUi();
        }
        finally
        {
            _startStopButton.Enabled = true;
        }
    }

    private async Task ListenAsync(HttpListener listener, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                HttpListenerContext context = await listener.GetContextAsync().WaitAsync(cancellationToken);
                _ = HandleRequestAsync(context, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (HttpListenerException)
        {
        }
        catch (Exception exception)
        {
            AppendLog($"Listen error: {exception.Message}");
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext context, CancellationToken cancellationToken)
    {
        string requestBody;
        using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding ?? Encoding.UTF8))
        {
            requestBody = await reader.ReadToEndAsync(cancellationToken);
        }

        AppendLog($"REQ {context.Request.HttpMethod} {context.Request.RawUrl} from {context.Request.RemoteEndPoint}");
        AppendLog($"REQ BODY: {requestBody}");

        string responseBody = BuildResponseBody(requestBody);
        byte[] payload = Encoding.UTF8.GetBytes(responseBody);

        context.Response.StatusCode = (int)HttpStatusCode.OK;
        context.Response.ContentType = "text/plain; charset=utf-8";
        context.Response.ContentLength64 = payload.Length;

        await context.Response.OutputStream.WriteAsync(payload, cancellationToken);
        context.Response.OutputStream.Close();

        AppendLog($"RES 200 len={payload.Length}");
        AppendLog($"RES BODY: {responseBody}");
    }

    private string BuildResponseBody(string requestBody)
    {
        string template = _responseTemplateTextBox.Text;
        if (string.IsNullOrEmpty(template))
        {
            template = "RECEIVED: {request}";
        }

        return template.Replace("{request}", requestBody, StringComparison.Ordinal);
    }

    private static string BuildPrefix(string host, int port, string path)
    {
        string normalizedHost = host.Trim();
        string normalizedPath = NormalizePath(path);
        return $"http://{normalizedHost}:{port}/{normalizedPath}";
    }

    private static string NormalizePath(string path)
    {
        string normalized = path.Trim();
        normalized = normalized.Trim('/');
        if (string.IsNullOrEmpty(normalized))
        {
            return string.Empty;
        }

        return normalized + "/";
    }

    private async Task StopServerAsync()
    {
        _listenCancellation?.Cancel();

        _listener?.Close();
        _listener = null;

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

        _listenCancellation?.Dispose();
        _listenCancellation = null;

        SetStoppedUi();
        AppendLog("Server stopped");
    }

    private void SetStoppedUi()
    {
        if (InvokeRequired)
        {
            BeginInvoke(SetStoppedUi);
            return;
        }

        _hostTextBox.Enabled = true;
        _portInput.Enabled = true;
        _pathTextBox.Enabled = true;
        _startStopButton.Text = "Start Server";
        SetStatus("Stopped", Color.Firebrick);
    }

    private void SetStatus(string text, Color color)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => SetStatus(text, color));
            return;
        }

        _statusLabel.Text = text;
        _statusLabel.ForeColor = color;
    }

    private void AppendLog(string text)
    {
        string line = $"[{DateTime.Now:HH:mm:ss}] {text}{Environment.NewLine}";

        if (InvokeRequired)
        {
            BeginInvoke(() => _logTextBox.AppendText(line));
            return;
        }

        _logTextBox.AppendText(line);
    }

    private async void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        await StopServerAsync();
    }
}
