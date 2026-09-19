/// <summary>
/// Application 経由で Menu 機能を利用するためのファサード
/// </summary>
public partial class MenuFacade : FacadeBase
{
    public MenuService Service { get; }

    public MenuFacade()
    {
        Service = AddModule<MenuService>("MenuService");
    }
}
