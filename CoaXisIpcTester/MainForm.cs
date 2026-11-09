using CoaXis.Protocol.Viewer;
using CoaXis.Ui;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CoaXis.IpcTester;

/// <summary>
/// 入力ファイルを読み込み、モデル集合のIPC要求とViewerの応答を表示する。
/// </summary>
public sealed partial class MainForm : CoaXisForm
{
    private readonly List<ExchangeRecord> _records = new();

    /// <summary>
    /// IPC接続先、ファイル選択、メッセージ履歴を配置する。
    /// </summary>
    public MainForm()
    {
        InitializeComponent();
        _entityPath.Text = FindSamplePath("modelentity.csv");
        _propertyPath.Text = FindSamplePath("modelproperty.csv");
        _testDataPath.Text = FindSamplePath("modelset-test-data.json");
        _requestType.Items.AddRange(new object[]
        {
            new RequestOption(IpcEventType.ToViewer.ClearModelSet, "ClearModelSet（全消去）"),
            new RequestOption(IpcEventType.ToViewer.AddModelSet, "AddModelSet（追加）"),
            new RequestOption(IpcEventType.ToViewer.RemoveModelSet, "RemoveModelSet（部分削除）"),
            new RequestOption(IpcEventType.ToViewer.ModifyModelSet, "ModifyModelSet（部分変更）"),
            new RequestOption(IpcEventType.ToViewer.ReadModelSet, "ReadModelSet（ID指定読み出し）")
        });
        _requestType.SelectedIndex = 0;
        _demoGrid.Rows.Add("名前", "CoaXis");
        _demoGrid.Rows.Add("状態", "準備完了");
        _demoTree.ExpandAll();
        _demoMessageButton.Click += (_, _) => CoaXisMessageBox.Show(
            this, "CoaXisのメッセージボックスです。", "メッセージ",
            MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
        _navigationRail.AddPage("IPC操作", CoaXisUiAssets.IpcIconResourceName, _ipcPage);
        _navigationRail.AddPage("開発中1", CoaXisUiAssets.DevelopmentOneIconResourceName, _developmentOnePage);
        _navigationRail.AddPage("開発中2", CoaXisUiAssets.DevelopmentTwoIconResourceName, _developmentTwoPage);
        _navigationRail.DesignerTabs = null;
        Controls.Remove(_designerTabs);
        _designerTabs.Dispose();
        _navigationRail.Dock = DockStyle.Fill;
        Header.SendToBack();

        _sendButton.Click += async (_, _) => await SendReplaceModelSetAsync();
        _sendRequestButton.Click += async (_, _) => await SendSelectedRequestAsync();
        _selectEntityButton.Click += SelectEntityFile;
        _selectPropertyButton.Click += SelectPropertyFile;
        _selectTestDataButton.Click += SelectTestDataFile;
        _history.SelectedIndexChanged += (_, _) => ShowSelectedExchange();
        _requestTabButton.Click += (_, _) => SelectExchangeTab(true);
        _responseTabButton.Click += (_, _) => SelectExchangeTab(false);
        _requestTabButton.KeyDown += OnExchangeTabKeyDown;
        _responseTabButton.KeyDown += OnExchangeTabKeyDown;
        Shown += OnMainFormShown;
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == (Keys.Control | Keys.Tab) || keyData == (Keys.Control | Keys.Shift | Keys.Tab))
        {
            bool request = _requestTabButton.Tag is not string tag || tag != "Primary";
            SelectExchangeTab(request);
            (request ? _requestTabButton : _responseTabButton).Focus();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    private static string FindSamplePath(string fileName)
    {
        DirectoryInfo directory = new(AppContext.BaseDirectory);
        while (directory != null)
        {
            string candidate = Path.Combine(directory.FullName, "samples", fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return string.Empty;
    }

    private void OnMainFormShown(object sender, EventArgs e)
    {
        int availableWidth = _exchangeSplit.ClientSize.Width - _exchangeSplit.SplitterWidth - 420;
        if (availableWidth < 220)
        {
            throw new InvalidOperationException("通信履歴領域を表示するためのフォーム幅が不足しています。");
        }

        _exchangeSplit.Panel1MinSize = 220;
        _exchangeSplit.Panel2MinSize = 420;
        _exchangeSplit.SplitterDistance = Math.Min(310, availableWidth);
    }

    private void OnExchangeTabKeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Left && e.KeyCode != Keys.Right)
        {
            return;
        }

        bool request = sender == _responseTabButton;
        SelectExchangeTab(request);
        (request ? _requestTabButton : _responseTabButton).Focus();
        e.Handled = true;
        e.SuppressKeyPress = true;
    }

    private void SelectExchangeTab(bool request)
    {
        _requestJson.Visible = request;
        _responseJson.Visible = !request;
        _requestTabButton.Tag = request ? "Primary" : null;
        _responseTabButton.Tag = request ? null : "Primary";
        _requestTabButton.AccessibleDescription = request ? "選択中" : null;
        _responseTabButton.AccessibleDescription = request ? null : "選択中";
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

    private void SelectTestDataFile(object sender, EventArgs args)
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "モデル集合操作テストデータ (*.json)|*.json|すべてのファイル (*.*)|*.*"
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _testDataPath.Text = dialog.FileName;
        }
    }

    private async Task SendReplaceModelSetAsync()
    {
        if (string.IsNullOrWhiteSpace(_entityPath.Text))
        {
            MessageBox.Show(this, "モデル実体ファイルを選択してください。", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        await ExecuteRequestAsync(
            IpcEventType.ToViewer.ReplaceModelSet,
            "ReplaceModelSetを送信中",
            () =>
            {
                List<ModelEntityDto> entities = ModelFileLoader.LoadEntities(_entityPath.Text);
                List<ModelPropertyDto> properties = ModelFileLoader.LoadProperties(_propertyPath.Text);
                return ModelSetPayload.Create(entities, properties);
            });
    }

    private async Task SendSelectedRequestAsync()
    {
        if (_requestType.SelectedItem is not RequestOption option)
        {
            MessageBox.Show(this, "送信する要求を選択してください。", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (option.EventType == IpcEventType.ToViewer.ClearModelSet &&
            MessageBox.Show(
                this,
                "ルート以外のすべてのEntityと全Propertyを削除します。実行しますか？",
                Text,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) != DialogResult.Yes)
        {
            return;
        }

        await ExecuteRequestAsync(
            option.EventType,
            $"{option.EventType}を送信中",
            () =>
            {
                if (option.EventType == IpcEventType.ToViewer.ClearModelSet)
                {
                    return new IpcEmptyPayload();
                }

                if (string.IsNullOrWhiteSpace(_testDataPath.Text))
                {
                    throw new InvalidDataException("操作テストデータJSONを選択してください。");
                }

                ModelSetTestData testData = ModelFileLoader.LoadTestData(_testDataPath.Text);
                return option.EventType switch
                {
                    IpcEventType.ToViewer.AddModelSet => testData.Add,
                    IpcEventType.ToViewer.RemoveModelSet => testData.Remove,
                    IpcEventType.ToViewer.ModifyModelSet => testData.Modify,
                    IpcEventType.ToViewer.ReadModelSet => testData.Read,
                    _ => throw new InvalidOperationException($"未対応の要求です: {option.EventType}")
                };
            });
    }

    private async Task ExecuteRequestAsync(string eventType, string progressMessage, Func<object> createPayload)
    {
        _sendButton.Enabled = false;
        _sendRequestButton.Enabled = false;
        _requestJson.Clear();
        _responseJson.Clear();
        try
        {
            object payload = createPayload();
            IpcEnvelope request = IpcEnvelopeBuilder.Create(eventType, "CoaXisIpcTester", payload);
            string requestJson = JsonSerializer.Serialize(request, IpcJsonOptions.Default);
            _requestJson.Text = JsonSerializer.Serialize(
                request,
                new JsonSerializerOptions(IpcJsonOptions.Default) { WriteIndented = true });
            _status.Text = progressMessage;

            string responseJson = await IpcClient.SendAsync(
                _pipeName.Text.Trim(),
                request,
                RecordNotification);
            if (string.IsNullOrWhiteSpace(responseJson))
            {
                throw new IOException("Viewerから応答がありませんでした。");
            }

            string formattedResponse = FormatJson(responseJson);
            _responseJson.Text = formattedResponse;
            (string result, string message) = ReadResult(responseJson);
            AddExchange(eventType, result, requestJson, formattedResponse, message);
            _status.Text = $"{result}: {message}";
        }
        catch (Exception exception)
        {
            string errorJson = JsonSerializer.Serialize(
                new { error = exception.Message },
                new JsonSerializerOptions { WriteIndented = true });
            _responseJson.Text = errorJson;
            AddExchange(eventType, "失敗", _requestJson.Text, errorJson, exception.Message);
            _status.Text = $"失敗: {exception.Message}";
        }
        finally
        {
            _sendButton.Enabled = true;
            _sendRequestButton.Enabled = true;
        }
    }

    private void AddExchange(string eventType, string result, string requestJson, string responseJson, string detail)
    {
        var record = new ExchangeRecord(DateTime.Now, eventType, result, requestJson, responseJson, detail);
        _records.Add(record);
        _history.Items.Add(record);
        _history.SelectedIndex = _history.Items.Count - 1;
    }

    private void RecordNotification(string notificationJson)
    {
        IpcEnvelope notification = JsonSerializer.Deserialize<IpcEnvelope>(
            notificationJson,
            IpcJsonOptions.Default);
        if (notification == null)
        {
            throw new InvalidDataException("Viewerから空の非同期通知を受信しました。");
        }

        string detail = notification.EventType;
        if (notification.EventType == IpcEventType.FromViewer.ModelEntitySelected &&
            notification.Payload.TryGetProperty("entityIds", out JsonElement entityIds) &&
            entityIds.ValueKind == JsonValueKind.Array)
        {
            detail = $"選択中のModelEntity: {entityIds.GetArrayLength()}件";
        }

        AddExchange(
            notification.EventType,
            "通知",
            string.Empty,
            FormatJson(notificationJson),
            detail);
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

    private sealed record RequestOption(string EventType, string DisplayName)
    {
        public override string ToString()
        {
            return DisplayName;
        }
    }
}