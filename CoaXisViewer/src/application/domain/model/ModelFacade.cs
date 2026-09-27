/// <summary>
/// Application 経由で Model 機能を利用するためのファサード
/// </summary>
public partial class ModelFacade : FacadeBase
{
    public ModelEvent Event { get; }
    public ModelRegistry Registry { get; }
    public ModelLoadFacade Load { get; }
    public ModelPresentationService Presentation { get; }
    public ModelStateService State { get; }

    public ModelFacade()
    {
        Event = AddModule<ModelEvent>("ModelEvent");
        Registry = AddModule<ModelRegistry>("ModelRegistry");
        Load = AddModule<ModelLoadFacade>("ModelLoadFacade");
        Presentation = AddModule<ModelPresentationService>("ModelPresentation");
        State = AddModule<ModelStateService>("ModelState");
    }
}