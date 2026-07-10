using Godot;

/// <summary>
/// Application 経由で LogHub モジュールへアクセスするためのゲートウェイ
/// </summary>
public sealed class ApplicationLogHub
{
    public LogHub Hub => Application.Instance.LogHub;

    public void Log(LogLevel level, string message) => Hub.Log(level, message);
    public void Debug(string msg) => Hub.Debug(msg);
    public void Info(string msg) => Hub.Info(msg);
    public void Warn(string msg) => Hub.Warn(msg);
    public void Error(string msg) => Hub.Error(msg);
}

/// <summary>
/// Application 経由で AssetManager モジュールへアクセスするためのゲートウェイ
/// </summary>
public sealed class ApplicationAssetManager
{
    public AssetManager Hub => Application.Instance.AssetManager;

    public Texture2D GetVisibilityIcon(bool isVisible, int size = 24) => Hub.GetVisibilityIcon(isVisible, size);
    public Texture2D GetIcon(string path, int size = 16) => Hub.GetIcon(path, size);
}
