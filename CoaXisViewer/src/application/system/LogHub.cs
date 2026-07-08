using Godot;
using System;
using System.IO;

/// <summary>
/// ログ関連のイベント集約ハブで、ログ通知と出力を一元管理する Autoload ノード
/// </summary>
public partial class LogHub : Node
{
    #region Fields

    private StreamWriter _fileWriter;
    private string _logFilePath = string.Empty;
    private bool _enableFileLog = false;

    [Signal] public delegate void LoggedEventHandler(string line);

    #endregion

    #region Lifecycle

    public override void _Ready()
    {
        _logFilePath = ProjectSettings.GlobalizePath("user://app.log");
        _fileWriter = new StreamWriter(_logFilePath, append: true);
        _enableFileLog = true;
    }

    public override void _ExitTree()
    {
        _enableFileLog = false;
        _fileWriter?.Dispose();

        base._ExitTree();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// ログレベルとメッセージを指定してログを出力する
    /// </summary>
    /// <param name="level">ログレベル</param>
    /// <param name="message">ログメッセージ</param> 
    internal void Log(LogLevel level, string message)
    {
        string line = $"{DateTime.Now:yyyy/MM/dd HH:mm:ss} [{level}] {message}";

        if (!IsInsideTree())
        {
            GD.PrintErr("LogHub is not initialized.");
            GD.Print(line);
            return;
        }

        // デバッグコンソールへの出力
        GD.Print(line);

        // ファイルへの出力
        if (_enableFileLog)
        {
            _fileWriter.WriteLine(line);
            _fileWriter.Flush();
        }

        // イベントの発行
        EmitSignal(SignalName.Logged, line);
    }

    /// <summary>
    /// デバッグレベルのログを出力する
    /// </summary>
    /// <param name="msg">ログメッセージ</param>
    internal void Debug(string msg) => Log(LogLevel.Debug, msg);

    /// <summary>
    /// 情報レベルのログを出力する
    /// </summary>
    /// <param name="msg">ログメッセージ</param>
    internal void Info(string msg) => Log(LogLevel.Info, msg);

    /// <summary>
    /// 警告レベルのログを出力する
    /// </summary>
    /// <param name="msg">ログメッセージ</param>
    internal void Warn(string msg) => Log(LogLevel.Warn, msg);

    /// <summary>
    /// エラーレベルのログを出力する
    /// </summary>
    /// <param name="msg">ログメッセージ</param>
    internal void Error(string msg) => Log(LogLevel.Error, msg);

    #endregion
}
