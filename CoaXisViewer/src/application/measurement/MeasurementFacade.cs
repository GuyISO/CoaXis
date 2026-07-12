using Godot;

/// <summary>
/// Application 経由で Measurement 機能を利用するためのファサード
/// </summary>
public partial class MeasurementFacade : Node
{
    // Domain instances
	public MeasurementEvent Event { get; }
	public MeasurementService Service { get; }

    public MeasurementFacade()
    {
        Event = new MeasurementEvent();
        Event.Name = "MeasurementEvent";
        AddChild(Event);

        Service = new MeasurementService();
        Service.Name = "MeasurementService";
        AddChild(Service);
    }

    // Event gateways
    public void AskMeasurementResult() => Event.AskMeasurementResult();
    public void SetPickPoint(int pointIndex) => Event.SetPickPoint(pointIndex);
    public void NotifyMeasurementResult(MeasurementResult result) => Event.NotifyMeasurementResult(result);

    // Service gateways

}
