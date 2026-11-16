using System.Text.Json;

namespace CoaXis.Protocol.Viewer;

/// <summary>
/// IPCで送受信するJSONの共通シリアライズ設定
/// </summary>
public static class IpcJsonOptions
{
    /// <summary>
    /// camelCase命名かつ大文字小文字を無視するIPC共通設定。個別のJsonPropertyName指定を不要にする
    /// </summary>
    public static readonly JsonSerializerOptions Default = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };
}
