using System;
using System.Windows.Forms;
using CoaXis.Ui;

namespace CoaXis.IpcTester;

internal sealed class ExchangeTabs : Panel
{
    private readonly TextBox _request;
    private readonly TextBox _response;
    private readonly CoaXisButton _requestButton = new() { Text = "送信JSON", AutoSize = true, Margin = new Padding(0, 0, 6, 6) };
    private readonly CoaXisButton _responseButton = new() { Text = "応答JSON", AutoSize = true, Margin = new Padding(0, 0, 0, 6) };

    public ExchangeTabs(TextBox request, TextBox response)
    {
        _request = request;
        _response = response;
        var content = new Panel { Dock = DockStyle.Fill };
        content.Controls.Add(_request);
        content.Controls.Add(_response);

        var bar = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = false };
        bar.Controls.Add(_requestButton);
        bar.Controls.Add(_responseButton);
        Controls.Add(content);
        Controls.Add(bar);
        bar.SendToBack();

        _requestButton.Click += OnRequestClick;
        _responseButton.Click += OnResponseClick;
        _requestButton.KeyDown += OnTabKeyDown;
        _responseButton.KeyDown += OnTabKeyDown;
        SelectRequest(true);
    }

    private void OnRequestClick(object sender, EventArgs e) => SelectRequest(true);

    private void OnResponseClick(object sender, EventArgs e) => SelectRequest(false);

    private void OnTabKeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Left && e.KeyCode != Keys.Right)
        {
            return;
        }

        bool request = sender == _responseButton;
        SelectRequest(request);
        (request ? _requestButton : _responseButton).Focus();
        e.Handled = true;
        e.SuppressKeyPress = true;
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == (Keys.Control | Keys.Tab) || keyData == (Keys.Control | Keys.Shift | Keys.Tab))
        {
            bool request = _requestButton.Tag is not string tag || tag != "Primary";
            SelectRequest(request);
            (request ? _requestButton : _responseButton).Focus();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void SelectRequest(bool request)
    {
        _request.Visible = request;
        _response.Visible = !request;
        _requestButton.Tag = request ? "Primary" : null;
        _responseButton.Tag = request ? null : "Primary";
        _requestButton.AccessibleDescription = request ? "選択中" : null;
        _responseButton.AccessibleDescription = request ? null : "選択中";
        ThemeManager.Apply(this);
    }
}
