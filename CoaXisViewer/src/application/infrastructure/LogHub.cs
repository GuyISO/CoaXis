using Godot;
using System;
using System.IO;

/// <summary>
/// ログレベルを表す列挙型
/// </summary>
public enum LogLevel
{
    /// <summary>開発中のデバッグ用</summary>
    Debug,
    /// <summary>ユーザーに通知する必要がある情報</summary>
    Info,
    /// <summary>警告を表す</summary>
    Warn,
    /// <summary>エラーを表す</summary>
    Error,
}

/// <summary>
/// アプリケーション全体のログ出力を管理するハブ。
/// </summary>
public partial class LogHub : BaseHub
{
    #region Fields

    private StreamWriter _fileWriter;
    private string _logFilePath = string.Empty;
    private bool _enableFileLog = false;

    #endregion

    #region Properties

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

    #region Events

    [Signal] public delegate void LoggedEventHandler(int level, string message);
    /// <summary>
    /// ログメッセージが通知されたときに発行されるシグナル。
    /// </summary>
    /// <param name="level">ログレベル</param>
    /// <param name="message">ログメッセージ</param>
    private void NotifyLogged(int level, string message)
    {
        EmitSignal(SignalName.Logged, level, message);
    }

    #endregion

    #region Methods

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

        // コンソール表示
        GD.Print(line);
        
        // ファイルへの出力
        if (_enableFileLog)
        {
            _fileWriter.WriteLine(line);
            _fileWriter.Flush();
        }

        // Signalの発行
        NotifyLogged((int)level, message);
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

    #region Helpers

    #endregion
}
