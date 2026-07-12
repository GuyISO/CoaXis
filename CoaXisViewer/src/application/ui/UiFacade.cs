/// <summary>
/// Application 経由で Ui 機能を利用するためのファサード
/// </summary>
public partial class UiFacade : FacadeBase
{
	public UiManager Service { get; }

	public UiFacade()
	{
		Service = AddModule<UiManager>("UiManager");
	}
}