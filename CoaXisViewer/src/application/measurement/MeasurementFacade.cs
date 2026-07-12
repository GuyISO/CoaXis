using Godot;

/// <summary>
/// Application 経由で Measurement 機能を利用するためのファサード
/// </summary>
public partial class MeasurementFacade : Node
{
    public MeasurementEvent Event { get; }
	public MeasurementService Service { get; }

    public MeasurementFacade()
    {
        Event = new MeasurementEvent() { Name = "MeasurementEvent" };
        Service = new MeasurementService() { Name = "MeasurementService" };

        AddChild(Event);
        AddChild(Service);
    }
}
