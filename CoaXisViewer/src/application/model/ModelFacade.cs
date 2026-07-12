using Godot;

/// <summary>
/// Application 経由で Model 機能を利用するためのファサード
/// </summary>
public partial class ModelFacade : Node
{
    public ModelEvent Event { get; }
    public ModelOperationService Operation { get; }
    public ModelVisualService Visual { get; }

    public ModelFacade()
    {
        Event = new ModelEvent() { Name = "ModelEvent" };
        Operation = new ModelOperationService() { Name = "ModelOperationService" };
        Visual = new ModelVisualService() { Name = "ModelVisualService" };

        AddChild(Event);
        AddChild(Operation);
        AddChild(Visual);
    }
}