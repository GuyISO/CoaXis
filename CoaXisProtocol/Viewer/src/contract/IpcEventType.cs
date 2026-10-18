namespace CoaXis.Protocol.Viewer;

/// <summary>
/// IPCで使用するイベント種別の文字列定数
/// </summary>
public static class IpcEventType
{
    /// <summary>
    /// Viewerが受信するイベント種別
    /// </summary>
    public static class ToViewer
    {
        /// <summary>モデル集合置換要求</summary>
        public const string ReplaceModelSet = "ReplaceModelSet";

        /// <summary>モデル実体の追加または置換要求</summary>
        public const string AddOrReplaceModelEntities = "AddOrReplaceModelEntities";

        /// <summary>モデル属性の追加または置換要求</summary>
        public const string AddOrReplaceModelProperties = "AddOrReplaceModelProperties";

        /// <summary>モデル実体の削除要求</summary>
        public const string DeleteModelEntities = "DeleteModelEntities";

        /// <summary>モデル属性の削除要求</summary>
        public const string DeleteModelProperties = "DeleteModelProperties";
    }

    /// <summary>
    /// Viewerが送信するイベント種別
    /// </summary>
    public static class FromViewer
    {
        /// <summary>選択通知</summary>
        public const string ModelEntitySelected = "ModelEntitySelected";
    }

    /// <summary>
    /// 双方向で使用するイベント種別
    /// </summary>
    public static class Common
    {
        /// <summary>IPC要求への処理結果応答</summary>
        public const string Result = "Result";
    }
}