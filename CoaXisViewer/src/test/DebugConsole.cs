using Godot;
using System.Text;

public partial class DebugConsole : Control
{
    public static DebugConsole Instance { get; private set; }

    [Export] private RichTextLabel _label;

    private StringBuilder _buffer = new StringBuilder();
    private const int MaxLines = 200;

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _ExitTree()
    {
        if (Instance == this)
            Instance = null;
    }

    public override void _Ready()
    {
        if (_label == null)
        {
            _label = GetNodeOrNull<RichTextLabel>("RichTextLabel");
            _label ??= FindChild("RichTextLabel", true, false) as RichTextLabel;
        }

    }

    public void AddLine(string text)
    {
        if (_label == null)
            return;

        _buffer.AppendLine(text);

        // 行数制限
        var lines = _buffer.ToString().Split('\n');
        if (lines.Length > MaxLines)
        {
            _buffer.Clear();
            for (int i = lines.Length - MaxLines; i < lines.Length; i++)
                _buffer.AppendLine(lines[i]);
        }

        _label.Text = _buffer.ToString();
        _label.ScrollToLine(Mathf.Max(_label.GetLineCount() - 1, 0));
    }

    public static void Log(string msg)
    {
        GD.Print(msg);
        DebugConsole.Instance?.AddLine(msg);
    }
}
