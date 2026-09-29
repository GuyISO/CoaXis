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
        /// <summary>モデル読み込み要求</summary>
        public const string LoadModel = "LoadModel";

        /// <summary>ハイライト要求</summary>
        public const string Highlight = "Highlight";

        /// <summary>選択要求</summary>
        public const string Select = "Select";

        /// <summary>表示プリセット適用要求</summary>
        public const string ApplyViewPreset = "ApplyViewPreset";

        /// <summary>カメラプリセット適用要求</summary>
        public const string ApplyCameraPreset = "ApplyCameraPreset";

        /// <summary>ハイライト解除要求</summary>
        public const string ClearHighlight = "ClearHighlight";

        /// <summary>対象へのフォーカス要求</summary>
        public const string Focus = "Focus";

        /// <summary>表示要求</summary>
        public const string Show = "Show";

        /// <summary>非表示要求</summary>
        public const string Hide = "Hide";
    }

    /// <summary>
    /// Viewerが送信するイベント種別
    /// </summary>
    public static class FromViewer
    {
        /// <summary>選択通知</summary>
        public const string OnSelect = "OnSelect";

        /// <summary>ホバー通知</summary>
        public const string OnHover = "OnHover";

        /// <summary>読み込み完了通知</summary>
        public const string OnLoaded = "OnLoaded";

        /// <summary>エラー通知</summary>
        public const string OnError = "OnError";
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