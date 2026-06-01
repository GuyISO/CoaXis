using System.Net;
using System.Text;

namespace HttpTestServer;

public sealed partial class MainForm : Form
{
    private CancellationTokenSource? _listenCancellation;
    private Task? _listenTask;
    private HttpListener? _listener;

    #region Properties

    private bool IsServerRunning => _listenCancellation is { IsCancellationRequested: false };

    #endregion

    #region Lifecycle

    public MainForm()
    {
        InitializeComponent();
    }

    #endregion

    #region Events

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

    private async void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        await StopServerAsync();
    }

    #endregion

    #region Internal Helpers

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

    #endregion

    #region Internal Helpers

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

    #endregion

    #region Internal Helpers

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

    #endregion
}
