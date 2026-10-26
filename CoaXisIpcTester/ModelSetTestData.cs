using CoaXis.Protocol.Viewer;

namespace CoaXis.IpcTester;

/// <summary>
/// IPC Testerが各モデル集合要求へ送信するテストpayloadを保持する。
/// </summary>
public sealed class ModelSetTestData
{
    /// <summary>AddModelSet用payload。</summary>
    public ModelSetPayload Add { get; init; }

    /// <summary>RemoveModelSet用payload。</summary>
    public ModelSetIdsPayload Remove { get; init; }

    /// <summary>ModifyModelSet用payload。</summary>
    public ModelSetModificationPayload Modify { get; init; }

    /// <summary>ReadModelSet用payload。</summary>
    public ModelSetIdsPayload Read { get; init; }
}
