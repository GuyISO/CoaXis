namespace HttpTestServer;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;
    private TextBox _hostTextBox = null!;
    private NumericUpDown _portInput = null!;
    private TextBox _pathTextBox = null!;
    private Button _startStopButton = null!;
    private Label _statusLabel = null!;
    private TextBox _responseTemplateTextBox = null!;
    private TextBox _logTextBox = null!;
    private Button _clearLogButton = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        TableLayoutPanel rootLayout;
        TableLayoutPanel connectionLayout;
        GroupBox responseGroup;
        GroupBox logGroup;
        TableLayoutPanel logLayout;
        Label hostLabel;
        Label portLabel;
        Label pathLabel;

        SuspendLayout();

        Text = "HTTP Test Server";
        MinimumSize = new Size(780, 560);
        StartPosition = FormStartPosition.CenterScreen;
        Name = "MainForm";

        rootLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(12),
            Name = "rootLayout"
        };
        rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        connectionLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 9,
            AutoSize = true,
            Name = "connectionLayout"
        };
        connectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        connectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
        connectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        connectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        connectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        connectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
        connectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        connectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        connectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        hostLabel = new Label
        {
            Text = "Host",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 7, 8, 0),
            Name = "hostLabel"
        };
        connectionLayout.Controls.Add(hostLabel, 0, 0);

        _hostTextBox = new TextBox
        {
            Text = "localhost",
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 8, 0),
            Name = "_hostTextBox"
        };
        connectionLayout.Controls.Add(_hostTextBox, 1, 0);

        portLabel = new Label
        {
            Text = "Port",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 7, 8, 0),
            Name = "portLabel"
        };
        connectionLayout.Controls.Add(portLabel, 2, 0);

        _portInput = new NumericUpDown
        {
            Minimum = 1,
            Maximum = 65535,
            Value = 8088,
            Width = 100,
            Margin = new Padding(0, 0, 8, 0),
            Name = "_portInput"
        };
        connectionLayout.Controls.Add(_portInput, 3, 0);

        pathLabel = new Label
        {
            Text = "Path",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 7, 8, 0),
            Name = "pathLabel"
        };
        connectionLayout.Controls.Add(pathLabel, 4, 0);

        _pathTextBox = new TextBox
        {
            Text = "api/echo",
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 8, 0),
            Name = "_pathTextBox"
        };
        connectionLayout.Controls.Add(_pathTextBox, 5, 0);

        _startStopButton = new Button
        {
            Text = "Start Server",
            AutoSize = true,
            Margin = new Padding(0, 0, 8, 0),
            Name = "_startStopButton"
        };
        _startStopButton.Click += StartStopButton_Click;
        connectionLayout.Controls.Add(_startStopButton, 6, 0);

        _statusLabel = new Label
        {
            Text = "Stopped",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            ForeColor = Color.Firebrick,
            Margin = new Padding(0, 7, 8, 0),
            Name = "_statusLabel"
        };
        connectionLayout.Controls.Add(_statusLabel, 7, 0);
        rootLayout.Controls.Add(connectionLayout, 0, 0);

        responseGroup = new GroupBox
        {
            Text = "Response template",
            Dock = DockStyle.Top,
            Padding = new Padding(10),
            Height = 120,
            Name = "responseGroup"
        };
        _responseTemplateTextBox = new TextBox
        {
            Multiline = true,
            Dock = DockStyle.Fill,
            ScrollBars = ScrollBars.Vertical,
            Text = "RECEIVED: {request}",
            Name = "_responseTemplateTextBox"
        };
        responseGroup.Controls.Add(_responseTemplateTextBox);
        rootLayout.Controls.Add(responseGroup, 0, 1);

        logGroup = new GroupBox
        {
            Text = "Request / Response log",
            Dock = DockStyle.Fill,
            Padding = new Padding(10),
            Name = "logGroup"
        };
        logLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Name = "logLayout"
        };
        logLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        logLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        logGroup.Controls.Add(logLayout);

        _logTextBox = new TextBox
        {
            Multiline = true,
            Dock = DockStyle.Fill,
            ReadOnly = true,
            ScrollBars = ScrollBars.Both,
            WordWrap = false,
            Name = "_logTextBox"
        };
        logLayout.Controls.Add(_logTextBox, 0, 0);

        _clearLogButton = new Button
        {
            Text = "Clear log",
            AutoSize = true,
            Anchor = AnchorStyles.Right,
            Name = "_clearLogButton"
        };
        _clearLogButton.Click += (_, _) => _logTextBox.Clear();
        logLayout.Controls.Add(_clearLogButton, 0, 1);
        rootLayout.Controls.Add(logGroup, 0, 2);

        Controls.Add(rootLayout);
        FormClosing += MainForm_FormClosing;
        ResumeLayout(false);
    }
}
