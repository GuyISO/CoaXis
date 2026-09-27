/// <summary>
/// Application 経由で Model 機能を利用するためのファサード
/// </summary>
public partial class ModelFacade : BaseFacade
{
    public ModelRegistry Registry { get; }
    public ModelLoadFacade Load { get; }
    public ModelVisualHub Visual { get; }
    public ModelStateHub State { get; }
    public ModelTreeHub Tree { get; }

    public ModelFacade()
    {
        Registry = AddModule<ModelRegistry>("Registry");
        Load = AddModule<ModelLoadFacade>("Load");
        Visual = AddModule<ModelVisualHub>("Visual");
        State = AddModule<ModelStateHub>("State");
        Tree = AddModule<ModelTreeHub>("Tree");
    }
}