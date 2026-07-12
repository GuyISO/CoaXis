/// <summary>
/// Application 経由で Setting 機能を利用するためのファサード
/// </summary>
public partial class SettingFacade : FacadeBase
{
	public SettingService Service { get; }

	public SettingFacade()
	{
		Service = AddModule<SettingService>("SettingService");
	}
}