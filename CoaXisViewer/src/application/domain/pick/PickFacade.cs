/// <summary>
/// Application 経由で Pick 機能を利用するためのファサード
/// </summary>
public partial class PickFacade : FacadeBase
{
	public PickEvent Event { get; }

	public PickFacade()
	{
		Event = AddModule<PickEvent>("PickEvent");
	}
}