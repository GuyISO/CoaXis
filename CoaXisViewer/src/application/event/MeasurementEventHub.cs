using Godot;

/// <summary>
/// 測定機能のイベント集約ハブ
/// </summary>
public partial class MeasurementEventHub : EventHubBase<MeasurementEventHub>
{
    #region --------------------------------------- Request ---------------------------------------

    [Signal] public delegate void NotifyMeasurementResultRequestedEventHandler();
    /// <summary>
    /// 最新の測定結果通知をリクエストする
    /// </summary>
    internal void RequestNotifyMeasurementResult()
    {
        TryEmitSignal(SignalName.NotifyMeasurementResultRequested);
    }

    [Signal] public delegate void PickPointRequestedEventHandler(int pointIndex);
    /// <summary>
    /// 測定ポイントのピック開始をリクエストする
    /// </summary>
    /// <param name="pointIndex">1 または 2</param>
    internal void RequestPickPoint(int pointIndex)
    {
        TryEmitSignal(SignalName.PickPointRequested, pointIndex);
    }

    #endregion

    #region --------------------------------------- Notification ---------------------------------------

    [Signal] public delegate void MeasurementResultNotifiedEventHandler(MeasurementResult result);
    /// <summary>
    /// 測定結果を通知する
    /// </summary>
    /// <param name="result">通知する測定結果</param>
    internal void NotifyMeasurementResult(MeasurementResult result)
    {
        TryEmitSignal(SignalName.MeasurementResultNotified, result);
    }

    #endregion
}
