using System.Text.Json;

namespace CoaXis.Protocol.Viewer;

/// <summary>
/// ベース DTO クラス
/// </summary>
public abstract class BaseDto
{
    public string ToJson(JsonSerializerOptions options = null)
    {
        return JsonSerializer.Serialize(this, options);
    }
}
