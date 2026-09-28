/// <summary>
/// Application 経由で Ui 機能を利用するためのファサード
/// </summary>
public partial class UiHub : BaseHub
{
	public UiMenuHub Menu { get; }
	public UiWindowHub Window { get; }

	public UiHub()
	{
		Menu = AddModule<UiMenuHub>("Menu");
		Window = AddModule<UiWindowHub>("Window");
	}
}