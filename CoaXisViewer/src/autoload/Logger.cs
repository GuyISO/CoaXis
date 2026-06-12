using Godot;
using System;
using System.IO;

/// <summary>
/// ログ関連のイベント集約ハブです。AutoLoadノードとしてシーンツリーに配置し、ログの通知や出力を管理します。これにより、ログ操作のロジックを分散させずに一元管理できます。
/// Autoloadに登録してシングルトン参照することを前提としていますが、複数インスタンスが存在する可能性も考慮して実装されています。
/// </summary>
public partial class Logger : Node
{
    public static Logger I { get; private set; } // シングルトン参照

    private StreamWriter _fileWriter;
    private string _logFilePath = string.Empty;
    private bool _enableFileLog = false;
    private bool _enableDebugConsole = true;

    [Signal] public delegate void LoggedEventHandler(LogLevel level, string message);

    public override void _Ready()
    {
        I = this;

        _logFilePath = ProjectSettings.GlobalizePath("user://app.log");
        _fileWriter = new StreamWriter(_logFilePath, append: true);
        _enableFileLog = true;
    }

    /// <summary>
    /// ログレベルとメッセージを指定してログを出力します。
    /// </summary>
    /// <param name="level">ログレベルです。</param>
    /// <param name="message">ログメッセージです。</param> 
    public void Log(LogLevel level, string message)
    {
        string line = $"{DateTime.Now:HH:mm:ss} [{level}] {message}";

        GD.Print(line);

        I.EmitSignal(SignalName.Logged, (int)level, message);

        if (I._enableFileLog)
        {
            I._fileWriter.WriteLine(line);
            I._fileWriter.Flush();
        }
    }

    /// <summary>
    /// デバッグレベルのログを出力します。
    /// </summary>
    /// <param name="msg">ログメッセージです。</param>
    public void Debug(string msg) => Log(LogLevel.Debug, msg);

    /// <summary>
    /// 情報レベルのログを出力します。
    /// </summary>
    /// <param name="msg">ログメッセージです。</param>
    public void Info(string msg)  => Log(LogLevel.Info, msg);

    /// <summary>
    /// 警告レベルのログを出力します。
    /// </summary>
    /// <param name="msg">ログメッセージです。</param>
    public void Warn(string msg)  => Log(LogLevel.Warn, msg);

    /// <summary>
    /// エラーレベルのログを出力します。
    /// </summary>
    /// <param name="msg">ログメッセージです。</param>
    public void Error(string msg) => Log(LogLevel.Error, msg);
}
