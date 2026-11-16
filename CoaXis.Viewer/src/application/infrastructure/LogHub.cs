using Godot;
using System;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// ログレベルを表す列挙型。
/// </summary>
public enum LogLevel
{
    /// <summary>開発中のデバッグ用。</summary>
    Debug,
    /// <summary>ユーザーに通知する必要がある情報。</summary>
    Info,
    /// <summary>警告を表す。</summary>
    Warn,
    /// <summary>エラーを表す。</summary>
    Error,
}

/// <summary>
/// アプリケーション全体のログ出力を管理するハブ。
/// </summary>
public partial class LogHub : BaseHub
{
    #region Fields

    /// <summary>ログファイルの書き込み先。</summary>
    private StreamWriter _fileWriter;
    /// <summary>ログファイルのパス。</summary>
    private string _logFilePath = string.Empty;
    /// <summary>ファイルログの有効フラグ。</summary>
    private bool _enableFileLog = false;
    /// <summary>メッセージパネルに表示するログ行。</summary>
    private readonly Queue<string> _history = new();

    #endregion

    #region Properties

    /// <summary>メッセージパネル用のログ行を古い順に取得する。</summary>
    public string[] History => _history.ToArray();

    #endregion

    #region Signals

    /// <summary>ログ出力の通知。</summary>
    /// <param name="level">ログレベル</param>
    /// <param name="message">ログメッセージ</param>
    [Signal] public delegate void LoggedEventHandler(int level, string message);
    
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

    #endregion

    #region Methods

    /// <summary>
    /// ログレベルとメッセージを指定してログを出力する。
    /// </summary>
    /// <param name="level">ログレベル</param>
    /// <param name="message">ログメッセージ</param> 
    public void Log(LogLevel level, string message)
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

        if (level >= LogLevel.Info)
        {
            AddToHistory(message);
        }

        // Signalの発行
        EmitSignal(SignalName.Logged, (int)level, message);
    }

    /// <summary>
    /// デバッグレベルのログを出力する。
    /// </summary>
    /// <param name="msg">ログメッセージ</param>
    public void Debug(string msg) => Log(LogLevel.Debug, msg);

    /// <summary>
    /// 情報レベルのログを出力する。
    /// </summary>
    /// <param name="msg">ログメッセージ</param>
    public void Info(string msg) => Log(LogLevel.Info, msg);

    /// <summary>
    /// 警告レベルのログを出力する。
    /// </summary>
    /// <param name="msg">ログメッセージ</param>
    public void Warn(string msg) => Log(LogLevel.Warn, msg);

    /// <summary>
    /// エラーレベルのログを出力する。
    /// </summary>
    /// <param name="msg">ログメッセージ</param>
    public void Error(string msg) => Log(LogLevel.Error, msg);

    #endregion

    #region Helpers

    /// <summary>表示対象のメッセージを行単位で保持し、設定された上限に収める。</summary>
    /// <param name="message">記録するメッセージ。</param>
    private void AddToHistory(string message)
    {
        int maxLines = Application.Instance != null && Application.Setting != null
            ? Application.Setting.Current.Ui.MessageMaxLines
            : UiSettings.DefaultMessageMaxLines;
        if (maxLines < UiSettings.MinMessageMaxLines)
        {
            maxLines = UiSettings.DefaultMessageMaxLines;
        }

        foreach (string line in (message ?? string.Empty).Split('\n'))
        {
            _history.Enqueue(line);
            while (_history.Count > maxLines)
            {
                _history.Dequeue();
            }
        }
    }

    #endregion
}
