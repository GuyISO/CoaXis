namespace NamedPipeTestServer;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;
    private TextBox _pipeNameTextBox = null!;
    private Button _connectButton = null!;
    private Label _statusLabel = null!;
    private TextBox _manualMessageTextBox = null!;
    private Button _sendManualButton = null!;
    private NumericUpDown _generatedLengthInput = null!;
    private Button _sendGeneratedButton = null!;
    private TextBox _receivedMessagesTextBox = null!;
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
        GroupBox sendManualGroup;
        TableLayoutPanel manualLayout;
        GroupBox generatedGroup;
        FlowLayoutPanel generatedLayout;
        GroupBox receiveGroup;
        TableLayoutPanel receiveLayout;
        Label pipeNameLabel;
        Label generatedLengthLabel;

        SuspendLayout();

        Text = "NamedPipe Test Server";
        MinimumSize = new Size(720, 520);
        StartPosition = FormStartPosition.CenterScreen;
        Name = "MainForm";

        rootLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(12),
            Name = "rootLayout"
        };
        rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 38F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 42F));

        connectionLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 4,
            AutoSize = true,
            Name = "connectionLayout"
        };
        connectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        connectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        connectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        connectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        pipeNameLabel = new Label
        {
            Text = "Pipe Name",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 7, 8, 0),
            Name = "pipeNameLabel"
        };
        connectionLayout.Controls.Add(pipeNameLabel, 0, 0);

        _pipeNameTextBox = new TextBox
        {
            Text = "CoaXisViewer",
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 8, 0),
            Name = "_pipeNameTextBox"
        };
        connectionLayout.Controls.Add(_pipeNameTextBox, 1, 0);

        _connectButton = new Button
        {
            Text = "Start Server",
            AutoSize = true,
            Margin = new Padding(0, 0, 8, 0),
            Name = "_connectButton"
        };
        _connectButton.Click += StartStopButton_Click;
        connectionLayout.Controls.Add(_connectButton, 2, 0);

        _statusLabel = new Label
        {
            Text = "Server stopped",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            ForeColor = Color.Firebrick,
            Margin = new Padding(0, 7, 0, 0),
            Name = "_statusLabel"
        };
        connectionLayout.Controls.Add(_statusLabel, 3, 0);
        rootLayout.Controls.Add(connectionLayout, 0, 0);

        sendManualGroup = new GroupBox
        {
            Text = "Send specified text",
            Dock = DockStyle.Fill,
            MinimumSize = new Size(0, 140),
            Padding = new Padding(10),
            Name = "sendManualGroup"
        };
        manualLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Name = "manualLayout"
        };
        manualLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        manualLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        manualLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        manualLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        sendManualGroup.Controls.Add(manualLayout);

        _manualMessageTextBox = new TextBox
        {
            Multiline = true,
            Dock = DockStyle.Fill,
            ScrollBars = ScrollBars.Vertical,
            Name = "_manualMessageTextBox"
        };
        manualLayout.Controls.Add(_manualMessageTextBox, 0, 0);
        manualLayout.SetColumnSpan(_manualMessageTextBox, 2);

        _sendManualButton = new Button
        {
            Text = "Send",
            AutoSize = true,
            Anchor = AnchorStyles.Right,
            Enabled = false,
            Name = "_sendManualButton"
        };
        _sendManualButton.Click += SendManualButton_Click;
        manualLayout.Controls.Add(_sendManualButton, 1, 1);
        rootLayout.Controls.Add(sendManualGroup, 0, 1);

        generatedGroup = new GroupBox
        {
            Text = "Send auto-generated text",
            Dock = DockStyle.Fill,
            MinimumSize = new Size(0, 80),
            Padding = new Padding(10),
            Name = "generatedGroup"
        };
        generatedLayout = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            Name = "generatedLayout"
        };
        generatedGroup.Controls.Add(generatedLayout);

        generatedLengthLabel = new Label
        {
            Text = "Length",
            AutoSize = true,
            Margin = new Padding(0, 8, 8, 0),
            Name = "generatedLengthLabel"
        };
        generatedLayout.Controls.Add(generatedLengthLabel);

        _generatedLengthInput = new NumericUpDown
        {
            Minimum = 1,
            Maximum = 100000,
            Value = 256,
            Width = 120,
            Name = "_generatedLengthInput"
        };
        generatedLayout.Controls.Add(_generatedLengthInput);

        _sendGeneratedButton = new Button
        {
            Text = "Generate and Send",
            AutoSize = true,
            Enabled = false,
            Margin = new Padding(12, 3, 0, 0),
            Name = "_sendGeneratedButton"
        };
        _sendGeneratedButton.Click += SendGeneratedButton_Click;
        generatedLayout.Controls.Add(_sendGeneratedButton);
        generatedLayout.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        rootLayout.Controls.Add(generatedGroup, 0, 2);

        receiveGroup = new GroupBox
        {
            Text = "Received messages",
            Dock = DockStyle.Fill,
            Padding = new Padding(10),
            Name = "receiveGroup"
        };
        receiveLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Name = "receiveLayout"
        };
        receiveLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        receiveLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        receiveGroup.Controls.Add(receiveLayout);

        _receivedMessagesTextBox = new TextBox
        {
            Multiline = true,
            Dock = DockStyle.Fill,
            ReadOnly = true,
            ScrollBars = ScrollBars.Both,
            WordWrap = false,
            Name = "_receivedMessagesTextBox"
        };
        receiveLayout.Controls.Add(_receivedMessagesTextBox, 0, 0);

        _clearLogButton = new Button
        {
            Text = "Clear log",
            AutoSize = true,
            Anchor = AnchorStyles.Right,
            Name = "_clearLogButton"
        };
        _clearLogButton.Click += (_, _) => _receivedMessagesTextBox.Clear();
        receiveLayout.Controls.Add(_clearLogButton, 0, 1);
        rootLayout.Controls.Add(receiveGroup, 0, 3);

        Controls.Add(rootLayout);
        FormClosing += MainForm_FormClosing;
        ResumeLayout(false);
    }
}
