/// <summary>
/// Application 経由で Setting 機�Eを利用するためのファサーチE
/// </summary>
public partial class SettingFacade : FacadeBase
{
	public SettingEvent Event { get; }
	public SettingService Service { get; }

	public SettingFacade()
	{
		Event = AddModule<SettingEvent>("SettingEvent");
		Service = AddModule<SettingService>("SettingService");
	}

	/// <summary>
	/// 外部設定ファイルを�E読み込みし、購読老E��変更を通知する
	/// </summary>
	internal void Reload()
	{
		Service.Reload();
		Event.NotifySettingsNotified();
	}
}