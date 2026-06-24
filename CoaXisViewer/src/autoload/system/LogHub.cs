using Godot;
using System;
using System.IO;

/// <summary>
/// ログ関連のイベント集約ハブです。AutoLoadノードとしてシーンツリーに配置し、ログの通知や出力を管理します。これにより、ログ操作のロジックを分散させずに一元管理できます。
/// Autoloadに登録してシングルトン参照することを前提としています。
/// </summary>
public partial class LogHub : Node
{
    public static LogHub I { get; private set; } // シングルトン参照

    private StreamWriter _fileWriter;
    private string _logFilePath = string.Empty;
    private bool _enableFileLog = false;

    [Signal] public delegate void LoggedEventHandler(string line);

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
    public static void Log(LogLevel level, string message)
    {
        string line = $"{DateTime.Now:yyyy/MM/dd HH:mm:ss} [{level}] {message}";

        // デバッグコンソールへの出力
        GD.Print(line);

        // ファイルへの出力
        if (I._enableFileLog)
        {
            I._fileWriter.WriteLine(line);
            I._fileWriter.Flush();
        }
        
        // イベントの発行
        I.EmitSignal(SignalName.Logged, line);
    }

    /// <summary>
    /// デバッグレベルのログを出力します。
    /// </summary>
    /// <param name="msg">ログメッセージです。</param>
    public static void Debug(string msg) => Log(LogLevel.Debug, msg);

    /// <summary>
    /// 情報レベルのログを出力します。
    /// </summary>
    /// <param name="msg">ログメッセージです。</param>
    public static void Info(string msg)  => Log(LogLevel.Info, msg);

    /// <summary>
    /// 警告レベルのログを出力します。
    /// </summary>
    /// <param name="msg">ログメッセージです。</param>
    public static void Warn(string msg)  => Log(LogLevel.Warn, msg);

    /// <summary>
    /// エラーレベルのログを出力します。
    /// </summary>
    /// <param name="msg">ログメッセージです。</param>
    public static void Error(string msg) => Log(LogLevel.Error, msg);
}
