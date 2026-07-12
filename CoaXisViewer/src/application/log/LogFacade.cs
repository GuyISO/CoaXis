using Godot;

/// <summary>
/// Application 経由で Log 機能を利用するためのファサード
/// </summary>
public partial class LogFacade : Node
{
    // Domain instances
	public LogEvent Event { get; }
	public LogService Service { get; }

    public LogFacade()
    {
        Event = new LogEvent();
        Event.Name = "LogEvent";
        AddChild(Event);

        Service = new LogService();
        Service.Name = "LogService";
        AddChild(Service);
    }

    // Event gateways
    public void NotifyLog(string message) => Event.NotifyLog(message);

    // Service gateways
    public void Log(LogLevel level, string message) => Service.Log(level, message);
    public void Debug(string message) => Service.Debug(message);
    public void Info(string message) => Service.Info(message);
    public void Warn(string message) => Service.Warn(message);
    public void Error(string message) => Service.Error(message);
}