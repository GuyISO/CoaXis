/// <summary>
/// Application 経由で Ui 機能を利用するためのファサード
/// </summary>
public partial class UiFacade : BaseFacade
{
	public UiMenuHub Menu { get; }
	public UiWindowHub Window { get; }

	public UiFacade()
	{
		Menu = AddModule<UiMenuHub>("Menu");
		Window = AddModule<UiWindowHub>("Window");
	}
}