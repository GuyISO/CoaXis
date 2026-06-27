#nullable enable

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// CoaXisViewer が扱う IPC コマンド名を定義する
/// </summary>
public static class ViewerIpcCommand
{
    public const string LoadModel = "LoadModel";
    public const string Highlight = "Highlight";
    public const string Select = "Select";
    public const string ApplyViewPreset = "ApplyViewPreset";
    public const string ApplyCameraPreset = "ApplyCameraPreset";
    public const string ClearHighlight = "ClearHighlight";
    public const string Focus = "Focus";
    public const string Hide = "Hide";
    public const string Show = "Show";
}

/// <summary>
/// CoaXisViewer から Editor へ通知する IPC イベント名を定義する
/// </summary>
public static class ViewerIpcEvent
{
    public const string OnSelect = "OnSelect";
    public const string OnHover = "OnHover";
    public const string OnLoaded = "OnLoaded";
    public const string OnError = "OnError";
    public const string Result = "Result";
}

/// <summary>
/// IPC失敗時に返す標準エラーコード
/// </summary>
public static class ViewerIpcErrorCode
{
    public const string None = "NONE";
    public const string EmptyMessage = "EMPTY_MESSAGE";
    public const string InvalidJson = "INVALID_JSON";
    public const string MissingCommand = "MISSING_COMMAND";
    public const string UnsupportedCommand = "UNSUPPORTED_COMMAND";
    public const string InvalidPayload = "INVALID_PAYLOAD";
    public const string TargetNotFound = "TARGET_NOT_FOUND";
    public const string InternalError = "INTERNAL_ERROR";
    public const string MainThreadTimeout = "MAIN_THREAD_TIMEOUT";
}

/// <summary>
/// NamedPipe で送受信する JSON メッセージの共通エンベロープ
/// </summary>
public sealed class ViewerIpcEnvelope
{
    /// <summary>
    /// 実行対象の IPC コマンドまたはイベント名
    /// </summary>
    [JsonPropertyName("command")]
    public string Command { get; init; } = string.Empty;

    /// <summary>
    /// コマンド固有の任意ペイロード
    /// </summary>
    [JsonPropertyName("payload")]
    public JsonElement? Payload { get; init; }
}



