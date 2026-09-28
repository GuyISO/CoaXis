/// <summary>
/// Application 経由でモデルのロード機能を利用するためのファサード
/// </summary>
public partial class ModelLoadHub : BaseHub
{
	public ModelLoadEntityHub Entity { get; }
	public ModelLoadPropertyHub Property { get; }
	public ModelLoadSceneHub Scene { get; }

	public ModelLoadHub()
	{
		Entity = AddModule<ModelLoadEntityHub>("Entity");
		Property = AddModule<ModelLoadPropertyHub>("Property");
		Scene = AddModule<ModelLoadSceneHub>("Scene");
	}
}