/// <summary>
/// Application 経由で Asset 機能を利用するためのファサード
/// </summary>
public partial class AssetFacade : FacadeBase
{
	public AssetManager Service { get; }

	public AssetFacade()
	{
		Service = AddModule<AssetManager>("AssetManager");
	}
}