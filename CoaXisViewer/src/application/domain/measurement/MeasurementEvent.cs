using Godot;

/// <summary>
/// 測定機能のイベント集約ハブ
/// </summary>
public partial class MeasurementEvent : EventBase<MeasurementEvent>
{
    #region --------------------------------------- Action ---------------------------------------

    [Signal] public delegate void AskResultRequestedEventHandler();
    /// <summary>
    /// 最新の測定結果通知をリクエストする
    /// </summary>
    internal void AskResult()
    {
        Emit(SignalName.AskResultRequested);
    }

    [Signal] public delegate void SetPointRequestedEventHandler(int pointIndex);
    /// <summary>
    /// 測定ポイントのピック開始をリクエストする
    /// </summary>
    /// <param name="pointIndex">1 または 2</param>
    internal void SetPoint(int pointIndex)
    {
        Emit(SignalName.SetPointRequested, pointIndex);
    }

    #endregion

    #region --------------------------------------- Notification ---------------------------------------

    [Signal] public delegate void PointNotifiedEventHandler(int pointIndex);
    /// <summary>
    /// 測定ポイントのピック点を通知する
    /// </summary>
    /// <param name="pointIndex">ピック対象ポイントのインデックス</param>
    internal void NotifyPoint(int pointIndex)
    {
        Emit(SignalName.PointNotified, pointIndex);
    }

    [Signal] public delegate void ResultNotifiedEventHandler(MeasurementResult result);
    /// <summary>
    /// 測定結果を通知する
    /// </summary>
    /// <param name="result">通知する測定結果</param>
    internal void NotifyResult(MeasurementResult result)
    {
        Emit(SignalName.ResultNotified, result);
    }

    #endregion
}
