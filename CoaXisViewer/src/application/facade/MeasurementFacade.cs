/// <summary>
/// Application 経由で Measurement 機能を利用するためのファサード
/// </summary>
public sealed class MeasurementFacade
{
    // Domain instances
	public MeasurementEvent Event { get; }
	public MeasurementService Service { get; }

    // EventHub gateways
    public void AskMeasurementResult() => Event.AskMeasurementResult();
    public void SetPickPoint(int pointIndex) => Event.SetPickPoint(pointIndex);
    public void NotifyMeasurementResult(MeasurementResult result) => Event.NotifyMeasurementResult(result);

    // Service gateways

}
