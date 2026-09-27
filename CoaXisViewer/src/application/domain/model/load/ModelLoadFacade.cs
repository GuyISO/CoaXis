/// <summary>
/// Application 経由でモデルのロード機能を利用するためのファサード
/// </summary>
public partial class ModelLoadFacade : BaseFacade
{
	public ModelLoadEntityService Entity { get; }
	public ModelLoadPropertyService Property { get; }
	public ModelLoadSceneService Scene { get; }

	public ModelLoadFacade()
	{
		Entity = AddModule<ModelLoadEntityService>("ModelLoadEntityService");
		Property = AddModule<ModelLoadPropertyService>("ModelLoadPropertyService");
		Scene = AddModule<ModelLoadSceneService>("ModelLoadSceneService");
	}
}