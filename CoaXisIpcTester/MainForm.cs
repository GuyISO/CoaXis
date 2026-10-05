using CoaXis.Protocol.Viewer;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CoaXis.IpcTester;

/// <summary>
/// 入力ファイルを読み込み、LoadModel要求とViewerの応答を表示する。
/// </summary>
public sealed class MainForm : Form
{
    private readonly TextBox _pipeName = new() { Text = "CoaXisViewerPipe", Width = 220 };
    private readonly TextBox _entityPath = new() { ReadOnly = true, Dock = DockStyle.Fill };
    private readonly TextBox _propertyPath = new() { ReadOnly = true, Dock = DockStyle.Fill };
    private readonly Button _sendButton = new() { Text = "LoadModelを送信", AutoSize = true };
    private readonly ListBox _history = new() { Dock = DockStyle.Fill, IntegralHeight = false };
    private readonly TextBox _requestJson = CreateJsonView();
    private readonly TextBox _responseJson = CreateJsonView();
    private readonly ToolStripStatusLabel _status = new("Viewer未接続");
    private readonly List<ExchangeRecord> _records = new();

    /// <summary>
    /// IPC接続先、ファイル選択、メッセージ履歴を配置する。
    /// </summary>
    public MainForm()
    {
        Text = "CoaXis IPC Tester";
        MinimumSize = new Size(860, 580);
        Size = new Size(1120, 760);
        StartPosition = FormStartPosition.CenterScreen;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(12)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(root);

        root.Controls.Add(BuildConnectionPanel(), 0, 0);
        root.Controls.Add(BuildFilePanel(), 0, 1);
        root.Controls.Add(BuildExchangePanel(), 0, 2);

        var statusStrip = new StatusStrip();
        statusStrip.Items.Add(_status);
        Controls.Add(statusStrip);
        statusStrip.Dock = DockStyle.Bottom;

        _sendButton.Click += async (_, _) => await SendLoadModelAsync();
        _history.SelectedIndexChanged += (_, _) => ShowSelectedExchange();
    }

    private Control BuildConnectionPanel()
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = false,
            Padding = new Padding(0, 0, 0, 8)
        };
        panel.Controls.Add(new Label { Text = "Pipe名", AutoSize = true, Margin = new Padding(0, 7, 8, 0) });
        panel.Controls.Add(_pipeName);
        panel.Controls.Add(new Label { Text = "Viewer起動後に送信", AutoSize = true, ForeColor = SystemColors.GrayText, Margin = new Padding(12, 7, 0, 0) });
        return panel;
    }

    private Control BuildFilePanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 3,
            RowCount = 3,
            Padding = new Padding(0, 0, 0, 10)
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        AddFileRow(panel, 0, "実体 (JSON/CSV)", _entityPath, "選択...", SelectEntityFile);
        AddFileRow(panel, 1, "属性 (CSV・任意)", _propertyPath, "選択...", SelectPropertyFile);
        panel.Controls.Add(_sendButton, 2, 2);
        panel.SetColumnSpan(_sendButton, 1);
        panel.Margin = new Padding(0, 4, 0, 0);
        return panel;
    }

    private static void AddFileRow(TableLayoutPanel panel, int row, string label, TextBox path, string buttonText, EventHandler onClick)
    {
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 7, 10, 7) }, 0, row);
        panel.Controls.Add(path, 1, row);
        var button = new Button { Text = buttonText, AutoSize = true };
        button.Click += onClick;
        panel.Controls.Add(button, 2, row);
    }

    private Control BuildExchangePanel()
    {
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical
        };
        Shown += (_, _) =>
        {
            split.SplitterDistance = 310;
            split.Panel1MinSize = 220;
            split.Panel2MinSize = 420;
        };
        split.Panel1.Controls.Add(_history);
        split.Panel1.Controls.Add(new Label { Text = "通信履歴", Dock = DockStyle.Top, Height = 26, TextAlign = ContentAlignment.MiddleLeft });

        var tabs = new TabControl { Dock = DockStyle.Fill };
        var requestTab = new TabPage("送信JSON");
        var responseTab = new TabPage("応答JSON");
        requestTab.Controls.Add(_requestJson);
        responseTab.Controls.Add(_responseJson);
        tabs.TabPages.Add(requestTab);
        tabs.TabPages.Add(responseTab);
        split.Panel2.Controls.Add(tabs);
        return split;
    }

    private static TextBox CreateJsonView()
    {
        return new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Both,
            WordWrap = false,
            Font = new Font(FontFamily.GenericMonospace, 9)
        };
    }

    private void SelectEntityFile(object sender, EventArgs args)
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "モデル実体 (*.json;*.csv)|*.json;*.csv|JSON (*.json)|*.json|CSV (*.csv)|*.csv|すべてのファイル (*.*)|*.*"
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _entityPath.Text = dialog.FileName;
        }
    }

    private void SelectPropertyFile(object sender, EventArgs args)
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "モデル属性 CSV (*.csv)|*.csv|すべてのファイル (*.*)|*.*"
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _propertyPath.Text = dialog.FileName;
        }
    }

    private async Task SendLoadModelAsync()
    {
        if (string.IsNullOrWhiteSpace(_entityPath.Text))
        {
            MessageBox.Show(this, "モデル実体ファイルを選択してください。", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _sendButton.Enabled = false;
        _requestJson.Clear();
        try
        {
            List<ModelEntityDto> entities = ModelFileLoader.LoadEntities(_entityPath.Text);
            List<ModelPropertyDto> properties = ModelFileLoader.LoadProperties(_propertyPath.Text);
            ModelSetPayload payload = ModelSetPayload.Create(entities, properties);
            IpcEnvelope request = IpcEnvelopeBuilder.Create(IpcEventType.ToViewer.LoadModel, "CoaXisIpcTester", payload);
            string requestJson = JsonSerializer.Serialize(request, IpcJsonOptions.Default);
            _requestJson.Text = JsonSerializer.Serialize(request, new JsonSerializerOptions(IpcJsonOptions.Default) { WriteIndented = true });
            _responseJson.Clear();
            _status.Text = $"送信中: {entities.Count} models / {properties.Count} properties";

            string responseJson = await IpcClient.SendAsync(_pipeName.Text.Trim(), request);
            if (string.IsNullOrWhiteSpace(responseJson))
            {
                throw new IOException("Viewerから応答がありませんでした。");
            }

            string formattedResponse = FormatJson(responseJson);
            _responseJson.Text = formattedResponse;
            (string result, string message) = ReadResult(responseJson);
            AddExchange(request.EventType, result, requestJson, formattedResponse, message);
            _status.Text = $"{result}: {message}";
        }
        catch (Exception exception)
        {
            string errorJson = JsonSerializer.Serialize(new { error = exception.Message }, new JsonSerializerOptions { WriteIndented = true });
            _responseJson.Text = errorJson;
            AddExchange(IpcEventType.ToViewer.LoadModel, "失敗", _requestJson.Text, errorJson, exception.Message);
            _status.Text = $"失敗: {exception.Message}";
        }
        finally
        {
            _sendButton.Enabled = true;
        }
    }

    private void AddExchange(string eventType, string result, string requestJson, string responseJson, string detail)
    {
        var record = new ExchangeRecord(DateTime.Now, eventType, result, requestJson, responseJson, detail);
        _records.Add(record);
        _history.Items.Add(record);
        _history.SelectedIndex = _history.Items.Count - 1;
    }

    private void ShowSelectedExchange()
    {
        if (_history.SelectedIndex < 0)
        {
            return;
        }

        ExchangeRecord record = _records[_history.SelectedIndex];
        _requestJson.Text = record.RequestJson;
        _responseJson.Text = record.ResponseJson;
    }

    private static string FormatJson(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        return JsonSerializer.Serialize(document.RootElement, new JsonSerializerOptions { WriteIndented = true });
    }

    private static (string Result, string Message) ReadResult(string responseJson)
    {
        using JsonDocument document = JsonDocument.Parse(responseJson);
        if (!document.RootElement.TryGetProperty("payload", out JsonElement payload) ||
            !payload.TryGetProperty("ok", out JsonElement okElement))
        {
            return ("応答不明", "Result payloadにokがありません。");
        }

        bool succeeded = okElement.GetBoolean();
        string message = payload.TryGetProperty("message", out JsonElement messageElement)
            ? messageElement.GetString() ?? string.Empty
            : string.Empty;
        string errorCode = payload.TryGetProperty("errorCode", out JsonElement errorCodeElement)
            ? errorCodeElement.GetString() ?? string.Empty
            : string.Empty;

        if (!succeeded && !string.IsNullOrWhiteSpace(errorCode))
        {
            message = $"{errorCode}: {message}";
        }

        return (succeeded ? "成功" : "失敗", message);
    }

    private sealed record ExchangeRecord(DateTime Timestamp, string EventType, string Result, string RequestJson, string ResponseJson, string Detail)
    {
        public override string ToString()
        {
            return $"{Timestamp:HH:mm:ss}  {EventType}  {Result}  {Detail}";
        }
    }
}