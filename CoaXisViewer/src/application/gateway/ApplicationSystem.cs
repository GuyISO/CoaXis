using Godot;

/// <summary>
/// Application 経由でシステム系モジュールへアクセスするためのゲートウェイ
/// </summary>
public sealed class ApplicationSystem
{
    public ApplicationLog Log { get; }
    public ApplicationAssets Assets { get; }

    public ApplicationSystem(Application app)
    {
        Log = new ApplicationLog(app);
        Assets = new ApplicationAssets(app);
    }
}

public sealed class ApplicationLog
{
    private readonly Application _app;

    public LogHub Hub => _app.LogHub;

    public ApplicationLog(Application app)
    {
        _app = app;
    }

    public void Log(LogLevel level, string message) => LogHub.Log(level, message);
    public void Debug(string msg) => LogHub.Debug(msg);
    public void Info(string msg) => LogHub.Info(msg);
    public void Warn(string msg) => LogHub.Warn(msg);
    public void Error(string msg) => LogHub.Error(msg);
}

public sealed class ApplicationAssets
{
    private readonly Application _app;

    public AssetManager Hub => _app.AssetManager;

    public ApplicationAssets(Application app)
    {
        _app = app;
    }

    public Texture2D GetVisibilityIcon(bool isVisible, int size = 24) => AssetManager.GetVisibilityIcon(isVisible, size);
    public Texture2D GetIcon(string path, int size = 16) => AssetManager.GetIcon(path, size);
}
