/// <summary>
/// Application 経由で Model 機能を利用するためのファサード
/// </summary>
public partial class ModelFacade : FacadeBase
{
    public ModelEvent Event { get; }
    public ModelOperationService Operation { get; }
    public ModelVisualService Visual { get; }

    public ModelFacade()
    {
        Event = AddModule<ModelEvent>("ModelEvent");
        Operation = AddModule<ModelOperationService>("ModelOperationService");
        Visual = AddModule<ModelVisualService>("ModelVisualService");
    }
}