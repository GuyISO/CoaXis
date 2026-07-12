/// <summary>
/// Application 経由で Log 機能を利用するためのファサード
/// </summary>
public partial class LogFacade : FacadeBase
{
    public LogEvent Event { get; }
    public LogService Service { get; }

    public LogFacade()
    {
        Event = AddModule<LogEvent>("LogEvent");
        Service = AddModule<LogService>("LogService");
    }
}