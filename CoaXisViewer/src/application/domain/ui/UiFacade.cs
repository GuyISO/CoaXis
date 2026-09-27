/// <summary>
/// Application 経由で Ui 機能を利用するためのファサード
/// </summary>
public partial class UiFacade : FacadeBase
{
	public UiMenuService Menu { get; }
	public UiWindowService Window { get; }

	public UiFacade()
	{
		Menu = AddModule<UiMenuService>("UiMenuService");
		Window = AddModule<UiWindowService>("UiWindowService");
	}
}