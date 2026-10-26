namespace CoaXis.Protocol.Viewer;

/// <summary>
/// Result エンベロープのpayloadに格納する処理結果
/// </summary>
/// <remarks>
/// プロパティ名は<see cref="IpcJsonOptions.Default"/>のcamelCase命名ポリシーに委ねる
/// </remarks>
public class IpcResultPayload
{
    /// <summary>要求の処理が成功したかどうか。</summary>
    public bool Ok { get; set; }

    /// <summary>処理した要求のイベント種別。</summary>
    public string Request { get; set; } = string.Empty;

    /// <summary>失敗時の標準化エラーコード。</summary>
    public string ErrorCode { get; set; } = IpcErrorCode.None;

    /// <summary>処理結果の説明。</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 成功結果を生成する
    /// </summary>
    /// <param name="request">処理した要求のイベント種別</param>
    /// <param name="message">結果メッセージ</param>
    /// <returns>成功状態の結果payload</returns>
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
    /// <param name="request">処理した要求のイベント種別</param>
    /// <param name="errorCode">標準化エラーコード</param>
    /// <param name="message">失敗理由</param>
    /// <returns>失敗状態の結果payload</returns>
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
