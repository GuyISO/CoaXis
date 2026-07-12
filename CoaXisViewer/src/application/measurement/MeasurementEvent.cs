using Godot;

/// <summary>
/// 測定機能のイベント集約ハブ
/// </summary>
public partial class MeasurementEvent : EventBase<MeasurementEvent>
{
    #region --------------------------------------- Action ---------------------------------------

    [Signal] public delegate void AskMeasurementResultRequestedEventHandler();
    /// <summary>
    /// 最新の測定結果通知をリクエストする
    /// </summary>
    internal void AskMeasurementResult()
    {
        Emit(SignalName.AskMeasurementResultRequested);
    }

    [Signal] public delegate void SetPickPointRequestedEventHandler(int pointIndex);
    /// <summary>
    /// 測定ポイントのピック開始をリクエストする
    /// </summary>
    /// <param name="pointIndex">1 または 2</param>
    internal void SetPickPoint(int pointIndex)
    {
        Emit(SignalName.SetPickPointRequested, pointIndex);
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
        Emit(SignalName.MeasurementResultNotified, result);
    }

    #endregion
}
