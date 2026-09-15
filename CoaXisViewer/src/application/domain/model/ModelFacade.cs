/// <summary>
/// Application 経由で Model 機能を利用するためのファサード
/// </summary>
public partial class ModelFacade : FacadeBase
{
    public ModelEvent Event { get; }
    public ModelService Service { get; }
    public ModelRegistry Registry { get; }
    public ModelEntityFactory EntityFactory { get; }
    public ModelPropertyFactory PropertyFactory { get; }

    public ModelFacade()
    {
        Event = AddModule<ModelEvent>("ModelEvent");
        Service = AddModule<ModelService>("ModelService");
        Registry = AddModule<ModelRegistry>("ModelRegistry");
        EntityFactory = AddModule<ModelEntityFactory>("ModelEntityFactory");
        PropertyFactory = AddModule<ModelPropertyFactory>("ModelPropertyFactory");
    }
}