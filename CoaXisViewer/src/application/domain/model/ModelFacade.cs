/// <summary>
/// Application 経由で Model 機能を利用するためのファサード
/// </summary>
public partial class ModelFacade : FacadeBase
{
    public ModelEvent Event { get; }
    public ModelPresentationService Presentation { get; }
    public ModelStateService State { get; }
    public ModelRegistry Registry { get; }
    public ModelLoadService LoadService { get; }
    public ModelSceneLoader SceneLoader { get; }
    public ModelEntityFactory EntityFactory { get; }
    public ModelPropertyFactory PropertyFactory { get; }

    public ModelFacade()
    {
        Event = AddModule<ModelEvent>("ModelEvent");
        Presentation = AddModule<ModelPresentationService>("ModelPresentationService");
        State = AddModule<ModelStateService>("ModelStateService");
        Registry = AddModule<ModelRegistry>("ModelRegistry");
        LoadService = AddModule<ModelLoadService>("ModelLoadService");
        SceneLoader = AddModule<ModelSceneLoader>("ModelSceneLoader");
        EntityFactory = AddModule<ModelEntityFactory>("ModelEntityFactory");
        PropertyFactory = AddModule<ModelPropertyFactory>("ModelPropertyFactory");
    }
}