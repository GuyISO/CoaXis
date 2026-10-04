// TODO: リファクタリング確認後に削除
using Godot;

/// <summary>
/// 選択されたモデルの操作モードを表す列挙型。
/// </summary>
public enum PickHandlingMode
{
    /// <summary>選択操作モード。</summary>
    Selection,
    /// <summary>測定操作モード。</summary>
    Measurement,
    /// <summary>面に垂直操作モード。</summary>
    NormalToFace,
}

/// <summary>
/// ピック操作のモードと結果を管理するハブ。
/// </summary>
public partial class ModelEntityPickHub : BaseHub
{
    #region Fields

    #endregion

    #region Properties

    /// <summary>現在の選択操作モードを取得する。</summary>
    internal PickHandlingMode HandlingMode { get; private set; } = PickHandlingMode.Selection;

    #endregion

    #region Signals

    /// <summary>選択操作モードの変更通知。</summary>
    [Signal] public delegate void HandlingModeNotifiedEventHandler();

    /// <summary>単一ピック結果の通知。</summary>
    [Signal] public delegate void ResultNotifiedEventHandler(PickResult pickResult);
    /// <summary>
    /// ピック結果の通知を行う。
    /// </summary>
    /// <param name="pickResult">ピック結果</param>
    internal void NotifyResult(PickResult pickResult)
    {
        EmitSignal(SignalName.ResultNotified, pickResult);
    }

    /// <summary>複数ピック結果の通知。</summary>
    [Signal] public delegate void ResultsNotifiedEventHandler(PickResult[] pickResults);
    /// <summary>
    /// 複数一括ピック結果の通知を行う。
    /// </summary>
    /// <param name="pickResults">ピック結果の配列</param>
    /// <remarks>複数のピック結果を一括で通知する場合はレイキャストによる取得ではないので座標値などを持たない</remarks>
    internal void NotifyResults(PickResult[] pickResults)
    {
        EmitSignal(SignalName.ResultsNotified, pickResults);
    }

    #endregion

    #region Lifecycle

    #endregion

    #region Events

    #endregion

    #region Methods

    /// <summary>
    /// 選択操作モードを変更し、変更内容を通知する。
    /// </summary>
    /// <param name="mode">設定する選択操作モード</param>
    public void SetHandlingMode(PickHandlingMode mode)
    {
        if (HandlingMode != mode)
        {
            HandlingMode = mode;
        }

        EmitSignal(SignalName.HandlingModeNotified);
    }

    #endregion

    #region Helpers

    #endregion
}