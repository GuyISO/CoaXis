/// <summary>
/// Application 経由で Command 機能を利用するためのファサード
/// </summary>
public partial class CommandFacade : FacadeBase
{
	public CommandEvent Event { get; }
	public CommandService Service { get; }

	public CommandFacade()
	{
		Event = AddModule<CommandEvent>("CommandEvent");
		Service = AddModule<CommandService>("CommandService");
	}
}