using System;

namespace CoaXis.Protocol.Viewer;

/// <summary>
/// ReadModelSetの成功Result。共通Result項目にモデル集合を追加する。
/// </summary>
public sealed class ReadModelSetResultPayload : IpcResultPayload
{
    /// <summary>要求されたモデル実体と属性のDTO。</summary>
    public ModelSetPayload ModelSet { get; init; }

    /// <summary>
    /// モデル集合を含む成功結果を生成する。
    /// </summary>
    /// <param name="request">処理した要求のイベント種別</param>
    /// <param name="message">結果メッセージ</param>
    /// <param name="modelSet">読み出したモデル集合</param>
    /// <returns>モデル集合を含む成功結果</returns>
    /// <exception cref="ArgumentNullException">modelSetがnullの場合</exception>
    public static ReadModelSetResultPayload Success(string request, string message, ModelSetPayload modelSet)
    {
        if (modelSet == null)
        {
            throw new ArgumentNullException(nameof(modelSet));
        }

        return new ReadModelSetResultPayload
        {
            Ok = true,
            Request = request ?? string.Empty,
            ErrorCode = IpcErrorCode.None,
            Message = message ?? string.Empty,
            ModelSet = modelSet
        };
    }
}
