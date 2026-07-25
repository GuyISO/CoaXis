/// <summary>
/// IPC サービスに適用する設定値。
/// </summary>
public sealed class IpcSettings
{
    /// <summary>
    /// NamedPipe 名。Editor 側と一致させる必要がある。
    /// </summary>
    public string PipeName { get; set; } = Constant.Ipc.PipeName;

    /// <summary>
    /// 起動時に NamedPipe サーバーを立ち上げるかどうか。
    /// </summary>
    public bool StartPipeServerOnReady { get; set; } = Constant.Ipc.StartPipeServerOnReady;

    /// <summary>
    /// IPC 設定の不正値を補正する。
    /// </summary>
    public void Normalize()
    {
        // PipeName/StartPipeServerOnReady は外部設定に公開しない固定値として扱う。
        PipeName = Constant.Ipc.PipeName;
        StartPipeServerOnReady = Constant.Ipc.StartPipeServerOnReady;
    }
}
