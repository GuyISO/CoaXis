/// <summary>
/// Application 経由で Ipc 機能を利用するためのファサード
/// </summary>
public partial class IpcFacade : FacadeBase
{
	public IpcEvent Event { get; }
	public IpcService Service { get; }

	public IpcFacade()
	{
		Event = AddModule<IpcEvent>("IpcEvent");
		Service = AddModule<IpcService>("IpcService");
	}
}
