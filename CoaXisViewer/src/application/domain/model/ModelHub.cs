/// <summary>
/// Application 経由で Model 機能を利用するためのファサード
/// </summary>
public partial class ModelHub : BaseHub
{
    public ModelRegistry Registry { get; }
    public ModelLoadHub Load { get; }
    public ModelVisualHub Visual { get; }
    public ModelStateHub State { get; }
    public ModelTreeHub Tree { get; }

    public ModelHub()
    {
        Registry = AddModule<ModelRegistry>("Registry");
        Load = AddModule<ModelLoadHub>("Load");
        Visual = AddModule<ModelVisualHub>("Visual");
        State = AddModule<ModelStateHub>("State");
        Tree = AddModule<ModelTreeHub>("Tree");
    }
}