/// <summary>
/// Application 経由で Setting 機能を利用するためのファサード
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
	/// 外部設定ファイルを再読み込みし、購読者へ変更を通知する
	/// </summary>
	internal void Reload()
	{
		Service.Reload();
		Event.NotifySettingsNotified();
	}
}