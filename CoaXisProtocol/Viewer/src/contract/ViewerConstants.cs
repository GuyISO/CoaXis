using System;

namespace CoaXis.Protocol.Viewer;

/// <summary>
/// Viewerで使用する定数を定義するクラス。
/// </summary>
public static class ViewerConstants
{
    /// <summary>
    /// Viewerが常に保持するルートEntityの識別子。
    /// </summary>
    public static readonly Guid RootModelEntityId = new("00000000-0000-0000-0000-000000000001");
}
