/// <summary>
/// Application 経由で Selection 機能を利用するためのファサード
/// </summary>
public partial class SelectionFacade : FacadeBase
{
	public SelectionEvent Event { get; }
	public SelectionService Service { get; }

	public SelectionFacade()
	{
		Event = AddModule<SelectionEvent>("SelectionEvent");
		Service = AddModule<SelectionService>("SelectionService");
	}
}