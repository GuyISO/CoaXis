namespace CoaXis.Protocol.Viewer;

/// <summary>
/// IPCプロトコルのバージョン情報と互換性判定
/// </summary>
public static class IpcProtocolVersion
{
    /// <summary>
    /// 現行のIPCプロトコルバージョン
    /// </summary>
    public const string Current = "1.0.0";

    /// <summary>
    /// 指定されたバージョンが現行プロトコルと互換性のあるメジャーバージョンか判定する
    /// </summary>
    /// <param name="version">要求側が指定したプロトコルバージョン</param>
    /// <returns>メジャーバージョンが一致する場合は true</returns>
    public static bool IsCompatible(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            return false;
        }

        string currentMajor = Current.Split('.')[0];
        string requestedMajor = version.Split('.')[0];
        return string.Equals(currentMajor, requestedMajor, System.StringComparison.Ordinal);
    }
}