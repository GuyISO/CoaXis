/// <summary>
/// Application 経由で Ui 機能を利用するためのファサード
/// </summary>
public partial class UiFacade : FacadeBase
{
	public UiService Service { get; }

	public UiFacade()
	{
		Service = AddModule<UiService>("UiService");
	}
}