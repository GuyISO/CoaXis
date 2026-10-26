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

        /// <summary>モデル集合の全消去要求</summary>
        public const string ClearModelSet = "ClearModelSet";

        /// <summary>モデル実体と属性の同時追加要求。既存IDとの衝突は拒否する</summary>
        public const string AddModelSet = "AddModelSet";

        /// <summary>モデル実体と属性の同時削除要求</summary>
        public const string RemoveModelSet = "RemoveModelSet";

        /// <summary>モデル実体と属性の値の同時変更要求</summary>
        public const string ModifyModelSet = "ModifyModelSet";

        /// <summary>モデル実体と属性のID指定読み出し要求。応答のResultにDTOを含める</summary>
        public const string ReadModelSet = "ReadModelSet";

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