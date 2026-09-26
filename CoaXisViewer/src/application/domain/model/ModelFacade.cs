/// <summary>
/// Application 経由で Model 機能を利用するためのファサード
/// </summary>
public partial class ModelFacade : FacadeBase
{
    public ModelEvent Event { get; }
    public ModelPresentationService Presentation { get; }
    public ModelStateService State { get; }
    public ModelRegistry Registry { get; }
    public ModelLoadService Load { get; }
    public ModelSceneService Scene { get; }
    public ModelFacade()
    {
        Event = AddModule<ModelEvent>("ModelEvent");
        Presentation = AddModule<ModelPresentationService>("ModelPresentationService");
        State = AddModule<ModelStateService>("ModelStateService");
        Registry = AddModule<ModelRegistry>("ModelRegistry");
        Load = AddModule<ModelLoadService>("ModelLoadService");
        Scene = AddModule<ModelSceneService>("ModelSceneService");
    }
}