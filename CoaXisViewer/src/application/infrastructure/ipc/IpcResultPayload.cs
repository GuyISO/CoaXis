using System.Text.Json.Serialization;

/// <summary>
/// Result エンベロープの payload に格納する処理結果
/// </summary>
public sealed class IpcResultPayload
{
    [JsonPropertyName("ok")]
    public bool Ok { get; set; }

    [JsonPropertyName("request")]
    public string Request { get; set; } = string.Empty;

    [JsonPropertyName("errorCode")]
    public string ErrorCode { get; set; } = IpcErrorCode.None;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 成功結果を生成する
    /// </summary>
    public static IpcResultPayload Success(string request, string message = "")
    {
        return new IpcResultPayload
        {
            Ok = true,
            Request = request ?? string.Empty,
            ErrorCode = IpcErrorCode.None,
            Message = message ?? string.Empty
        };
    }

    /// <summary>
    /// 失敗結果を生成する
    /// </summary>
    public static IpcResultPayload Failure(string request, string errorCode, string message)
    {
        return new IpcResultPayload
        {
            Ok = false,
            Request = request ?? string.Empty,
            ErrorCode = errorCode,
            Message = message ?? string.Empty
        };
    }
}
