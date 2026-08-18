/// <summary>
/// IPC Result 応答で使う標準化エラーコード
/// </summary>
public static class IpcErrorCode
{
    public const string None = "NONE";
    public const string EmptyMessage = "EMPTY_MESSAGE";
    public const string InvalidJson = "INVALID_JSON";
    public const string MissingEventType = "MISSING_EVENT_TYPE";
    public const string UnsupportedEventType = "UNSUPPORTED_EVENT_TYPE";
    public const string InvalidPayload = "INVALID_PAYLOAD";
    public const string TargetNotFound = "TARGET_NOT_FOUND";
    public const string VersionMismatch = "VERSION_MISMATCH";
    public const string InternalError = "INTERNAL_ERROR";
    public const string MainThreadTimeout = "MAIN_THREAD_TIMEOUT";
}
