/// <summary>
/// Application 経由で Measurement 機能を利用するためのファサード
/// </summary>
public partial class MeasurementFacade : FacadeBase
{
    public MeasurementEvent Event { get; }
    public MeasurementService Service { get; }

    public MeasurementFacade()
    {
        Event = AddModule<MeasurementEvent>("MeasurementEvent");
        Service = AddModule<MeasurementService>("MeasurementService");
    }
}
