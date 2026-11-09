namespace CoaXis.IpcTester;

public sealed partial class MainForm
{
    private System.ComponentModel.IContainer components;
    private CoaXis.Ui.CoaXisNavigationRail _navigationRail;
    private System.Windows.Forms.TabControl _designerTabs;
    private System.Windows.Forms.TabPage _ipcDesignerTab;
    private System.Windows.Forms.TabPage _developmentOneDesignerTab;
    private System.Windows.Forms.TabPage _developmentTwoDesignerTab;
    private CoaXis.Ui.CoaXisPanel _ipcPage;
    private CoaXis.Ui.CoaXisPanel _developmentOnePage;
    private CoaXis.Ui.CoaXisPanel _developmentTwoPage;
    private CoaXis.Ui.CoaXisTableLayoutPanel _ipcLayout;
    private CoaXis.Ui.CoaXisFlowLayoutPanel _connectionPanel;
    private CoaXis.Ui.CoaXisLabel _pipeNameLabel;
    private CoaXis.Ui.CoaXisTextBox _pipeName;
    private CoaXis.Ui.CoaXisLabel _connectionHintLabel;
    private CoaXis.Ui.CoaXisTableLayoutPanel _filePanel;
    private CoaXis.Ui.CoaXisLabel _entityLabel;
    private CoaXis.Ui.CoaXisTextBox _entityPath;
    private CoaXis.Ui.CoaXisButton _selectEntityButton;
    private CoaXis.Ui.CoaXisLabel _propertyLabel;
    private CoaXis.Ui.CoaXisTextBox _propertyPath;
    private CoaXis.Ui.CoaXisButton _selectPropertyButton;
    private CoaXis.Ui.CoaXisLabel _testDataLabel;
    private CoaXis.Ui.CoaXisTextBox _testDataPath;
    private CoaXis.Ui.CoaXisButton _selectTestDataButton;
    private CoaXis.Ui.CoaXisLabel _requestTypeLabel;
    private CoaXis.Ui.CoaXisComboBox _requestType;
    private CoaXis.Ui.CoaXisFlowLayoutPanel _sendButtonsPanel;
    private CoaXis.Ui.CoaXisButton _sendButton;
    private CoaXis.Ui.CoaXisButton _sendRequestButton;
    private CoaXis.Ui.CoaXisPanel _exchangeCard;
    private CoaXis.Ui.CoaXisSplitContainer _exchangeSplit;
    private CoaXis.Ui.CoaXisLabel _historyLabel;
    private CoaXis.Ui.CoaXisListBox _history;
    private CoaXis.Ui.CoaXisTableLayoutPanel _exchangeLayout;
    private CoaXis.Ui.CoaXisFlowLayoutPanel _exchangeTabBar;
    private CoaXis.Ui.CoaXisButton _requestTabButton;
    private CoaXis.Ui.CoaXisButton _responseTabButton;
    private CoaXis.Ui.CoaXisPanel _jsonPanel;
    private CoaXis.Ui.CoaXisDisplayTextBox _requestJson;
    private CoaXis.Ui.CoaXisDisplayTextBox _responseJson;
    private CoaXis.Ui.CoaXisLabel _developmentOneLabel;
    private CoaXis.Ui.CoaXisTableLayoutPanel _demoLayout;
    private CoaXis.Ui.CoaXisLabel _demoInputLabel;
    private CoaXis.Ui.CoaXisTextBox _demoTextBox;
    private CoaXis.Ui.CoaXisLabel _demoComboLabel;
    private CoaXis.Ui.CoaXisComboBox _demoComboBox;
    private CoaXis.Ui.CoaXisCheckBox _demoCheckBox;
    private CoaXis.Ui.CoaXisButton _demoMessageButton;
    private CoaXis.Ui.CoaXisLabel _demoGridLabel;
    private CoaXis.Ui.CoaXisLabel _demoTreeLabel;
    private CoaXis.Ui.CoaXisDataGridView _demoGrid;
    private CoaXis.Ui.CoaXisTreeView _demoTree;
    private System.Windows.Forms.DataGridViewTextBoxColumn _demoNameColumn;
    private System.Windows.Forms.DataGridViewTextBoxColumn _demoValueColumn;
    private CoaXis.Ui.CoaXisLabel _developmentTwoLabel;
    private CoaXis.Ui.CoaXisStatusStrip _statusStrip;
    private System.Windows.Forms.ToolStripStatusLabel _status;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null)
        {
            components.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        _navigationRail = new CoaXis.Ui.CoaXisNavigationRail();
        _designerTabs = new System.Windows.Forms.TabControl();
        _ipcDesignerTab = new System.Windows.Forms.TabPage();
        _developmentOneDesignerTab = new System.Windows.Forms.TabPage();
        _developmentTwoDesignerTab = new System.Windows.Forms.TabPage();
        _ipcPage = new CoaXis.Ui.CoaXisPanel();
        _ipcLayout = new CoaXis.Ui.CoaXisTableLayoutPanel();
        _connectionPanel = new CoaXis.Ui.CoaXisFlowLayoutPanel();
        _pipeNameLabel = new CoaXis.Ui.CoaXisLabel();
        _pipeName = new CoaXis.Ui.CoaXisTextBox();
        _connectionHintLabel = new CoaXis.Ui.CoaXisLabel();
        _filePanel = new CoaXis.Ui.CoaXisTableLayoutPanel();
        _entityLabel = new CoaXis.Ui.CoaXisLabel();
        _entityPath = new CoaXis.Ui.CoaXisTextBox();
        _selectEntityButton = new CoaXis.Ui.CoaXisButton();
        _propertyLabel = new CoaXis.Ui.CoaXisLabel();
        _propertyPath = new CoaXis.Ui.CoaXisTextBox();
        _selectPropertyButton = new CoaXis.Ui.CoaXisButton();
        _testDataLabel = new CoaXis.Ui.CoaXisLabel();
        _testDataPath = new CoaXis.Ui.CoaXisTextBox();
        _selectTestDataButton = new CoaXis.Ui.CoaXisButton();
        _requestTypeLabel = new CoaXis.Ui.CoaXisLabel();
        _requestType = new CoaXis.Ui.CoaXisComboBox();
        _sendButtonsPanel = new CoaXis.Ui.CoaXisFlowLayoutPanel();
        _sendButton = new CoaXis.Ui.CoaXisButton();
        _sendRequestButton = new CoaXis.Ui.CoaXisButton();
        _exchangeCard = new CoaXis.Ui.CoaXisPanel();
        _exchangeSplit = new CoaXis.Ui.CoaXisSplitContainer();
        _historyLabel = new CoaXis.Ui.CoaXisLabel();
        _history = new CoaXis.Ui.CoaXisListBox();
        _exchangeLayout = new CoaXis.Ui.CoaXisTableLayoutPanel();
        _exchangeTabBar = new CoaXis.Ui.CoaXisFlowLayoutPanel();
        _requestTabButton = new CoaXis.Ui.CoaXisButton();
        _responseTabButton = new CoaXis.Ui.CoaXisButton();
        _jsonPanel = new CoaXis.Ui.CoaXisPanel();
        _requestJson = new CoaXis.Ui.CoaXisDisplayTextBox();
        _responseJson = new CoaXis.Ui.CoaXisDisplayTextBox();
        _developmentOnePage = new CoaXis.Ui.CoaXisPanel();
        _developmentTwoPage = new CoaXis.Ui.CoaXisPanel();
        _developmentOneLabel = new CoaXis.Ui.CoaXisLabel();
        _demoLayout = new CoaXis.Ui.CoaXisTableLayoutPanel();
        _demoInputLabel = new CoaXis.Ui.CoaXisLabel();
        _demoTextBox = new CoaXis.Ui.CoaXisTextBox();
        _demoComboLabel = new CoaXis.Ui.CoaXisLabel();
        _demoComboBox = new CoaXis.Ui.CoaXisComboBox();
        _demoCheckBox = new CoaXis.Ui.CoaXisCheckBox();
        _demoMessageButton = new CoaXis.Ui.CoaXisButton();
        _demoGridLabel = new CoaXis.Ui.CoaXisLabel();
        _demoTreeLabel = new CoaXis.Ui.CoaXisLabel();
        _demoGrid = new CoaXis.Ui.CoaXisDataGridView();
        _demoTree = new CoaXis.Ui.CoaXisTreeView();
        _demoNameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        _demoValueColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
        _developmentTwoLabel = new CoaXis.Ui.CoaXisLabel();
        _statusStrip = new CoaXis.Ui.CoaXisStatusStrip();
        _status = new System.Windows.Forms.ToolStripStatusLabel();
        _designerTabs.SuspendLayout();
        _ipcDesignerTab.SuspendLayout();
        _developmentOneDesignerTab.SuspendLayout();
        _developmentTwoDesignerTab.SuspendLayout();
        _ipcPage.SuspendLayout();
        _ipcLayout.SuspendLayout();
        _connectionPanel.SuspendLayout();
        _filePanel.SuspendLayout();
        _sendButtonsPanel.SuspendLayout();
        _exchangeCard.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_exchangeSplit).BeginInit();
        _exchangeSplit.Panel1.SuspendLayout();
        _exchangeSplit.Panel2.SuspendLayout();
        _exchangeSplit.SuspendLayout();
        _exchangeLayout.SuspendLayout();
        _exchangeTabBar.SuspendLayout();
        _jsonPanel.SuspendLayout();
        _developmentOnePage.SuspendLayout();
        _demoLayout.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_demoGrid).BeginInit();
        _developmentTwoPage.SuspendLayout();
        _statusStrip.SuspendLayout();
        SuspendLayout();
        //
        // _navigationRail
        //
        _navigationRail.DesignerPreviewIcons = "CoaXis.Ui.Assets.Icons.ipc.svg;CoaXis.Ui.Assets.Icons.development-one.svg;CoaXis.Ui.Assets.Icons.development-two.svg";
        _navigationRail.DesignerTabs = _designerTabs;
        _navigationRail.Dock = System.Windows.Forms.DockStyle.Left;
        _navigationRail.Location = new System.Drawing.Point(0, 0);
        _navigationRail.Name = "_navigationRail";
        _navigationRail.Size = new System.Drawing.Size(60, 701);
        _navigationRail.TabIndex = 0;
        //
        // _designerTabs
        //
        _designerTabs.Controls.Add(_ipcDesignerTab);
        _designerTabs.Controls.Add(_developmentOneDesignerTab);
        _designerTabs.Controls.Add(_developmentTwoDesignerTab);
        _designerTabs.Dock = System.Windows.Forms.DockStyle.Fill;
        _designerTabs.Name = "_designerTabs";
        _designerTabs.SelectedIndex = 0;
        _designerTabs.TabIndex = 1;
        //
        // _ipcDesignerTab
        //
        _ipcDesignerTab.Controls.Add(_ipcPage);
        _ipcDesignerTab.Name = "_ipcDesignerTab";
        _ipcDesignerTab.Text = "IPC操作";
        //
        // _developmentOneDesignerTab
        //
        _developmentOneDesignerTab.Controls.Add(_developmentOnePage);
        _developmentOneDesignerTab.Name = "_developmentOneDesignerTab";
        _developmentOneDesignerTab.Text = "開発中1";
        //
        // _developmentTwoDesignerTab
        //
        _developmentTwoDesignerTab.Controls.Add(_developmentTwoPage);
        _developmentTwoDesignerTab.Name = "_developmentTwoDesignerTab";
        _developmentTwoDesignerTab.Text = "開発中2";
        //
        // _ipcPage
        //
        _ipcPage.Controls.Add(_ipcLayout);
        _ipcPage.Dock = System.Windows.Forms.DockStyle.Fill;
        _ipcPage.Location = new System.Drawing.Point(0, 0);
        _ipcPage.Name = "_ipcPage";
        _ipcPage.Padding = new System.Windows.Forms.Padding(20);
        _ipcPage.Size = new System.Drawing.Size(1104, 701);
        _ipcPage.SurfaceRole = CoaXis.Ui.CoaXisSurfaceRole.Background;
        _ipcPage.TabIndex = 1;
        //
        // _ipcLayout
        //
        _ipcLayout.ColumnCount = 1;
        _ipcLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
        _ipcLayout.Controls.Add(_connectionPanel, 0, 0);
        _ipcLayout.Controls.Add(_filePanel, 0, 1);
        _ipcLayout.Controls.Add(_exchangeCard, 0, 2);
        _ipcLayout.Dock = System.Windows.Forms.DockStyle.Fill;
        _ipcLayout.Location = new System.Drawing.Point(20, 20);
        _ipcLayout.Name = "_ipcLayout";
        _ipcLayout.RowCount = 3;
        _ipcLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        _ipcLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        _ipcLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
        _ipcLayout.Size = new System.Drawing.Size(1064, 661);
        _ipcLayout.TabIndex = 0;
        _ipcLayout.SurfaceRole = CoaXis.Ui.CoaXisSurfaceRole.Background;
        //
        // _connectionPanel
        //
        _connectionPanel.AutoSize = true;
        _connectionPanel.Controls.Add(_pipeNameLabel);
        _connectionPanel.Controls.Add(_pipeName);
        _connectionPanel.Controls.Add(_connectionHintLabel);
        _connectionPanel.Dock = System.Windows.Forms.DockStyle.Fill;
        _connectionPanel.Margin = new System.Windows.Forms.Padding(0, 0, 0, 12);
        _connectionPanel.Name = "_connectionPanel";
        _connectionPanel.Padding = new System.Windows.Forms.Padding(12);
        _connectionPanel.Size = new System.Drawing.Size(1064, 48);
        _connectionPanel.TabIndex = 0;
        _connectionPanel.WrapContents = false;
        //
        // _pipeNameLabel
        //
        _pipeNameLabel.AutoSize = true;
        _pipeNameLabel.Location = new System.Drawing.Point(12, 19);
        _pipeNameLabel.Margin = new System.Windows.Forms.Padding(0, 7, 8, 0);
        _pipeNameLabel.Name = "_pipeNameLabel";
        _pipeNameLabel.Size = new System.Drawing.Size(43, 15);
        _pipeNameLabel.TabIndex = 0;
        _pipeNameLabel.Text = "Pipe名";
        //
        // _pipeName
        //
        _pipeName.Location = new System.Drawing.Point(63, 12);
        _pipeName.Margin = new System.Windows.Forms.Padding(0);
        _pipeName.Name = "_pipeName";
        _pipeName.Size = new System.Drawing.Size(220, 23);
        _pipeName.TabIndex = 1;
        _pipeName.Text = "CoaXisViewerPipe";
        //
        // _connectionHintLabel
        //
        _connectionHintLabel.AutoSize = true;
        _connectionHintLabel.Location = new System.Drawing.Point(295, 19);
        _connectionHintLabel.Margin = new System.Windows.Forms.Padding(12, 7, 0, 0);
        _connectionHintLabel.Name = "_connectionHintLabel";
        _connectionHintLabel.Size = new System.Drawing.Size(128, 15);
        _connectionHintLabel.TabIndex = 2;
        _connectionHintLabel.Role = CoaXis.Ui.CoaXisLabelRole.Muted;
        _connectionHintLabel.Text = "Viewer起動後に送信";
        //
        // _filePanel
        //
        _filePanel.AutoSize = true;
        _filePanel.ColumnCount = 3;
        _filePanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
        _filePanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
        _filePanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
        _filePanel.Controls.Add(_entityLabel, 0, 0);
        _filePanel.Controls.Add(_entityPath, 1, 0);
        _filePanel.Controls.Add(_selectEntityButton, 2, 0);
        _filePanel.Controls.Add(_propertyLabel, 0, 1);
        _filePanel.Controls.Add(_propertyPath, 1, 1);
        _filePanel.Controls.Add(_selectPropertyButton, 2, 1);
        _filePanel.Controls.Add(_testDataLabel, 0, 2);
        _filePanel.Controls.Add(_testDataPath, 1, 2);
        _filePanel.Controls.Add(_selectTestDataButton, 2, 2);
        _filePanel.Controls.Add(_requestTypeLabel, 0, 3);
        _filePanel.Controls.Add(_requestType, 1, 3);
        _filePanel.Controls.Add(_sendButtonsPanel, 1, 4);
        _filePanel.Dock = System.Windows.Forms.DockStyle.Top;
        _filePanel.Margin = new System.Windows.Forms.Padding(0, 0, 0, 12);
        _filePanel.Name = "_filePanel";
        _filePanel.Padding = new System.Windows.Forms.Padding(12);
        _filePanel.RowCount = 5;
        _filePanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        _filePanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        _filePanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        _filePanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        _filePanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        _filePanel.Size = new System.Drawing.Size(1064, 237);
        _filePanel.TabIndex = 1;
        _filePanel.SetColumnSpan(_requestType, 2);
        _filePanel.SetColumnSpan(_sendButtonsPanel, 2);
        //
        // file selection controls
        //
        _entityLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        _entityLabel.AutoSize = true;
        _entityLabel.Margin = new System.Windows.Forms.Padding(0, 10, 10, 10);
        _entityLabel.Name = "_entityLabel";
        _entityLabel.Text = "実体 (JSON/CSV)";
        _entityPath.Dock = System.Windows.Forms.DockStyle.Fill;
        _entityPath.Margin = new System.Windows.Forms.Padding(0);
        _entityPath.Name = "_entityPath";
        _entityPath.ReadOnly = true;
        _selectEntityButton.AutoSize = true;
        _selectEntityButton.Margin = new System.Windows.Forms.Padding(8, 4, 0, 4);
        _selectEntityButton.Name = "_selectEntityButton";
        _selectEntityButton.Text = "選択...";
        _propertyLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        _propertyLabel.AutoSize = true;
        _propertyLabel.Margin = new System.Windows.Forms.Padding(0, 10, 10, 10);
        _propertyLabel.Name = "_propertyLabel";
        _propertyLabel.Text = "属性 (CSV・任意)";
        _propertyPath.Dock = System.Windows.Forms.DockStyle.Fill;
        _propertyPath.Margin = new System.Windows.Forms.Padding(0);
        _propertyPath.Name = "_propertyPath";
        _propertyPath.ReadOnly = true;
        _selectPropertyButton.AutoSize = true;
        _selectPropertyButton.Margin = new System.Windows.Forms.Padding(8, 4, 0, 4);
        _selectPropertyButton.Name = "_selectPropertyButton";
        _selectPropertyButton.Text = "選択...";
        _testDataLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        _testDataLabel.AutoSize = true;
        _testDataLabel.Margin = new System.Windows.Forms.Padding(0, 10, 10, 10);
        _testDataLabel.Name = "_testDataLabel";
        _testDataLabel.Text = "操作テストデータ (JSON)";
        _testDataPath.Dock = System.Windows.Forms.DockStyle.Fill;
        _testDataPath.Margin = new System.Windows.Forms.Padding(0);
        _testDataPath.Name = "_testDataPath";
        _testDataPath.ReadOnly = true;
        _selectTestDataButton.AutoSize = true;
        _selectTestDataButton.Margin = new System.Windows.Forms.Padding(8, 4, 0, 4);
        _selectTestDataButton.Name = "_selectTestDataButton";
        _selectTestDataButton.Text = "選択...";
        _requestTypeLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        _requestTypeLabel.AutoSize = true;
        _requestTypeLabel.Margin = new System.Windows.Forms.Padding(0, 10, 10, 10);
        _requestTypeLabel.Name = "_requestTypeLabel";
        _requestTypeLabel.Text = "要求";
        _requestType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        _requestType.DesignerPreviewItems = "ClearModelSet（全消去）\r\nAddModelSet（追加）\r\nRemoveModelSet（部分削除）\r\nModifyModelSet（部分変更）\r\nReadModelSet（ID指定読み出し）";
        _requestType.Margin = new System.Windows.Forms.Padding(0, 4, 0, 4);
        _requestType.Name = "_requestType";
        _requestType.Size = new System.Drawing.Size(260, 23);
        //
        // _sendButtonsPanel
        //
        _sendButtonsPanel.AutoSize = true;
        _sendButtonsPanel.Controls.Add(_sendButton);
        _sendButtonsPanel.Controls.Add(_sendRequestButton);
        _sendButtonsPanel.Dock = System.Windows.Forms.DockStyle.Fill;
        _sendButtonsPanel.Margin = new System.Windows.Forms.Padding(0, 6, 0, 0);
        _sendButtonsPanel.Name = "_sendButtonsPanel";
        _sendButtonsPanel.WrapContents = false;
        _sendButton.AutoSize = true;
        _sendButton.Margin = new System.Windows.Forms.Padding(3, 3, 8, 3);
        _sendButton.Name = "_sendButton";
        _sendButton.Variant = CoaXis.Ui.CoaXisButtonVariant.Primary;
        _sendButton.Text = "ReplaceModelSetを送信";
        _sendRequestButton.AutoSize = true;
        _sendRequestButton.Margin = new System.Windows.Forms.Padding(3);
        _sendRequestButton.Name = "_sendRequestButton";
        _sendRequestButton.Variant = CoaXis.Ui.CoaXisButtonVariant.Primary;
        _sendRequestButton.Text = "選択した要求を送信";
        //
        // _exchangeCard
        //
        _exchangeCard.Controls.Add(_exchangeSplit);
        _exchangeCard.Dock = System.Windows.Forms.DockStyle.Fill;
        _exchangeCard.Margin = new System.Windows.Forms.Padding(0);
        _exchangeCard.Name = "_exchangeCard";
        _exchangeCard.Padding = new System.Windows.Forms.Padding(12);
        _exchangeCard.Size = new System.Drawing.Size(1064, 376);
        _exchangeCard.TabIndex = 2;
        //
        // _exchangeSplit
        //
        _exchangeSplit.Dock = System.Windows.Forms.DockStyle.Fill;
        _exchangeSplit.Location = new System.Drawing.Point(12, 12);
        _exchangeSplit.Name = "_exchangeSplit";
        //
        // _exchangeSplit.Panel1
        //
        _exchangeSplit.Panel1.Controls.Add(_history);
        _exchangeSplit.Panel1.Controls.Add(_historyLabel);
        //
        // _exchangeSplit.Panel2
        //
        _exchangeSplit.Panel2.Controls.Add(_exchangeLayout);
        _exchangeSplit.Size = new System.Drawing.Size(1040, 352);
        _exchangeSplit.SplitterDistance = 310;
        _exchangeSplit.TabIndex = 0;
        //
        // _history
        //
        _history.Dock = System.Windows.Forms.DockStyle.Fill;
        _history.IntegralHeight = false;
        _history.Name = "_history";
        _history.TabIndex = 0;
        //
        // _historyLabel
        //
        _historyLabel.Dock = System.Windows.Forms.DockStyle.Top;
        _historyLabel.Height = 34;
        _historyLabel.Name = "_historyLabel";
        _historyLabel.Padding = new System.Windows.Forms.Padding(4, 0, 0, 0);
        _historyLabel.Text = "通信履歴";
        _historyLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        //
        // _exchangeLayout
        //
        _exchangeLayout.ColumnCount = 1;
        _exchangeLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
        _exchangeLayout.Controls.Add(_exchangeTabBar, 0, 0);
        _exchangeLayout.Controls.Add(_jsonPanel, 0, 1);
        _exchangeLayout.Dock = System.Windows.Forms.DockStyle.Fill;
        _exchangeLayout.Location = new System.Drawing.Point(0, 0);
        _exchangeLayout.Name = "_exchangeLayout";
        _exchangeLayout.RowCount = 2;
        _exchangeLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        _exchangeLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
        _exchangeLayout.TabIndex = 0;
        //
        // _exchangeTabBar
        //
        _exchangeTabBar.AutoSize = true;
        _exchangeTabBar.Controls.Add(_requestTabButton);
        _exchangeTabBar.Controls.Add(_responseTabButton);
        _exchangeTabBar.Dock = System.Windows.Forms.DockStyle.Top;
        _exchangeTabBar.Margin = new System.Windows.Forms.Padding(0);
        _exchangeTabBar.Name = "_exchangeTabBar";
        _exchangeTabBar.WrapContents = false;
        _requestTabButton.AutoSize = true;
        _requestTabButton.Margin = new System.Windows.Forms.Padding(0, 0, 6, 6);
        _requestTabButton.Name = "_requestTabButton";
        _requestTabButton.IsSelected = true;
        _requestTabButton.Text = "送信JSON";
        _responseTabButton.AutoSize = true;
        _responseTabButton.Margin = new System.Windows.Forms.Padding(0, 0, 0, 6);
        _responseTabButton.Name = "_responseTabButton";
        _responseTabButton.Text = "応答JSON";
        //
        // _jsonPanel
        //
        _jsonPanel.Controls.Add(_responseJson);
        _jsonPanel.Controls.Add(_requestJson);
        _jsonPanel.Dock = System.Windows.Forms.DockStyle.Fill;
        _jsonPanel.Name = "_jsonPanel";
        _jsonPanel.TabIndex = 1;
        _requestJson.Dock = System.Windows.Forms.DockStyle.Fill;
        _requestJson.Multiline = true;
        _requestJson.Name = "_requestJson";
        _requestJson.ReadOnly = true;
        _requestJson.ScrollBars = System.Windows.Forms.ScrollBars.Both;
        _requestJson.WordWrap = false;
        _responseJson.Dock = System.Windows.Forms.DockStyle.Fill;
        _responseJson.Multiline = true;
        _responseJson.Name = "_responseJson";
        _responseJson.ReadOnly = true;
        _responseJson.ScrollBars = System.Windows.Forms.ScrollBars.Both;
        _responseJson.Visible = false;
        _responseJson.WordWrap = false;
        //
        // development pages
        //
        _developmentOnePage.Controls.Add(_demoLayout);
        _developmentOnePage.Controls.Add(_developmentOneLabel);
        _developmentOnePage.Dock = System.Windows.Forms.DockStyle.Fill;
        _developmentOnePage.Name = "_developmentOnePage";
        _developmentOnePage.Padding = new System.Windows.Forms.Padding(20);
        _developmentOnePage.SurfaceRole = CoaXis.Ui.CoaXisSurfaceRole.Background;
        _developmentOneLabel.AutoSize = true;
        _developmentOneLabel.Dock = System.Windows.Forms.DockStyle.Top;
        _developmentOneLabel.Name = "_developmentOneLabel";
        _developmentOneLabel.Role = CoaXis.Ui.CoaXisLabelRole.Brand;
        _developmentOneLabel.Text = "開発中1";
        //
        // _demoLayout
        //
        _demoLayout.ColumnCount = 2;
        _demoLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 60F));
        _demoLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 40F));
        _demoLayout.Controls.Add(_demoInputLabel, 0, 0);
        _demoLayout.Controls.Add(_demoComboLabel, 1, 0);
        _demoLayout.Controls.Add(_demoTextBox, 0, 1);
        _demoLayout.Controls.Add(_demoComboBox, 1, 1);
        _demoLayout.Controls.Add(_demoCheckBox, 0, 2);
        _demoLayout.Controls.Add(_demoMessageButton, 1, 2);
        _demoLayout.Controls.Add(_demoGridLabel, 0, 3);
        _demoLayout.Controls.Add(_demoTreeLabel, 1, 3);
        _demoLayout.Controls.Add(_demoGrid, 0, 4);
        _demoLayout.Controls.Add(_demoTree, 1, 4);
        _demoLayout.Dock = System.Windows.Forms.DockStyle.Fill;
        _demoLayout.Name = "_demoLayout";
        _demoLayout.RowCount = 5;
        _demoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        _demoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        _demoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        _demoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        _demoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
        _demoLayout.SurfaceRole = CoaXis.Ui.CoaXisSurfaceRole.Background;
        _demoInputLabel.AutoSize = true;
        _demoInputLabel.Margin = new System.Windows.Forms.Padding(3, 16, 3, 6);
        _demoInputLabel.Name = "_demoInputLabel";
        _demoInputLabel.Text = "テキストボックス";
        _demoTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
        _demoTextBox.Name = "_demoTextBox";
        _demoTextBox.Text = "CoaXis UI";
        _demoComboLabel.AutoSize = true;
        _demoComboLabel.Margin = new System.Windows.Forms.Padding(12, 16, 3, 6);
        _demoComboLabel.Name = "_demoComboLabel";
        _demoComboLabel.Text = "コンボボックス";
        _demoComboBox.Dock = System.Windows.Forms.DockStyle.Fill;
        _demoComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        _demoComboBox.Items.AddRange(new object[] { "選択肢 1", "選択肢 2", "選択肢 3" });
        _demoComboBox.Margin = new System.Windows.Forms.Padding(12, 3, 3, 3);
        _demoComboBox.Name = "_demoComboBox";
        _demoComboBox.SelectedIndex = 0;
        _demoCheckBox.AutoSize = true;
        _demoCheckBox.Checked = true;
        _demoCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
        _demoCheckBox.Margin = new System.Windows.Forms.Padding(3, 16, 3, 16);
        _demoCheckBox.Name = "_demoCheckBox";
        _demoCheckBox.Text = "チェックボックス";
        _demoMessageButton.AutoSize = true;
        _demoMessageButton.Margin = new System.Windows.Forms.Padding(12, 8, 3, 8);
        _demoMessageButton.Name = "_demoMessageButton";
        _demoMessageButton.Variant = CoaXis.Ui.CoaXisButtonVariant.Primary;
        _demoMessageButton.Text = "メッセージを表示";
        _demoGridLabel.AutoSize = true;
        _demoGridLabel.Margin = new System.Windows.Forms.Padding(3, 8, 3, 6);
        _demoGridLabel.Name = "_demoGridLabel";
        _demoGridLabel.Text = "テーブルビュー";
        _demoTreeLabel.AutoSize = true;
        _demoTreeLabel.Margin = new System.Windows.Forms.Padding(12, 8, 3, 6);
        _demoTreeLabel.Name = "_demoTreeLabel";
        _demoTreeLabel.Text = "ツリービュー";
        _demoGrid.AllowUserToAddRows = false;
        _demoGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
        _demoGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        _demoGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { _demoNameColumn, _demoValueColumn });
        _demoGrid.DesignerPreviewRows = "名前\tCoaXis\r\n状態\t準備完了";
        _demoGrid.Dock = System.Windows.Forms.DockStyle.Fill;
        _demoGrid.Name = "_demoGrid";
        _demoGrid.RowHeadersVisible = false;
        _demoNameColumn.HeaderText = "項目";
        _demoNameColumn.Name = "_demoNameColumn";
        _demoValueColumn.HeaderText = "値";
        _demoValueColumn.Name = "_demoValueColumn";
        _demoTree.Dock = System.Windows.Forms.DockStyle.Fill;
        _demoTree.Margin = new System.Windows.Forms.Padding(12, 3, 3, 3);
        _demoTree.Name = "_demoTree";
        _demoTree.Nodes.AddRange(new System.Windows.Forms.TreeNode[] {
            new System.Windows.Forms.TreeNode("モデル", new System.Windows.Forms.TreeNode[] {
                new System.Windows.Forms.TreeNode("実体"),
                new System.Windows.Forms.TreeNode("属性") }),
            new System.Windows.Forms.TreeNode("設定") });
        _demoTree.DesignerExpandAll = true;
        _developmentTwoPage.Controls.Add(_developmentTwoLabel);
        _developmentTwoPage.Dock = System.Windows.Forms.DockStyle.Fill;
        _developmentTwoPage.Name = "_developmentTwoPage";
        _developmentTwoPage.Padding = new System.Windows.Forms.Padding(20);
        _developmentTwoPage.SurfaceRole = CoaXis.Ui.CoaXisSurfaceRole.Background;
        _developmentTwoLabel.AutoSize = true;
        _developmentTwoLabel.Dock = System.Windows.Forms.DockStyle.Top;
        _developmentTwoLabel.Name = "_developmentTwoLabel";
        _developmentTwoLabel.Role = CoaXis.Ui.CoaXisLabelRole.Brand;
        _developmentTwoLabel.Text = "開発中2";
        //
        // _statusStrip
        //
        _statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { _status });
        _statusStrip.Dock = System.Windows.Forms.DockStyle.Bottom;
        _statusStrip.Location = new System.Drawing.Point(0, 701);
        _statusStrip.Name = "_statusStrip";
        _statusStrip.Size = new System.Drawing.Size(1104, 22);
        _statusStrip.TabIndex = 2;
        _statusStrip.Text = "statusStrip1";
        _status.Text = "Viewer未接続";
        //
        // MainForm
        //
        AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        ClientSize = new System.Drawing.Size(1104, 723);
        HeaderTitle = "IPC Tester";
        Controls.Add(_designerTabs);
        Controls.Add(_navigationRail);
        Controls.Add(_statusStrip);
        MinimumSize = new System.Drawing.Size(860, 580);
        Name = "MainForm";
        StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
        Text = "CoaXis IPC Tester";
        _designerTabs.ResumeLayout(false);
        _ipcDesignerTab.ResumeLayout(false);
        _developmentOneDesignerTab.ResumeLayout(false);
        _developmentTwoDesignerTab.ResumeLayout(false);
        _ipcPage.ResumeLayout(false);
        _ipcLayout.ResumeLayout(false);
        _ipcLayout.PerformLayout();
        _connectionPanel.ResumeLayout(false);
        _connectionPanel.PerformLayout();
        _filePanel.ResumeLayout(false);
        _filePanel.PerformLayout();
        _sendButtonsPanel.ResumeLayout(false);
        _exchangeCard.ResumeLayout(false);
        _exchangeSplit.Panel1.ResumeLayout(false);
        _exchangeSplit.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)_exchangeSplit).EndInit();
        _exchangeSplit.ResumeLayout(false);
        _exchangeLayout.ResumeLayout(false);
        _exchangeLayout.PerformLayout();
        _exchangeTabBar.ResumeLayout(false);
        _exchangeTabBar.PerformLayout();
        _jsonPanel.ResumeLayout(false);
        _jsonPanel.PerformLayout();
        _developmentOnePage.ResumeLayout(false);
        _developmentOnePage.PerformLayout();
        _demoLayout.ResumeLayout(false);
        _demoLayout.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)_demoGrid).EndInit();
        _developmentTwoPage.ResumeLayout(false);
        _developmentTwoPage.PerformLayout();
        _statusStrip.ResumeLayout(false);
        _statusStrip.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }
}
