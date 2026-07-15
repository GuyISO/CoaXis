/// <summary>
/// Application 経由で Selection 機能を利用するためのファサード
/// </summary>
public partial class SelectionFacade : FacadeBase
{
	public SelectionService Service { get; }

	public SelectionFacade()
	{
		Service = AddModule<SelectionService>("SelectionService");
	}
}