/// <summary>
/// Application 経由でモデル表示機能を利用するためのファサード
/// </summary>
public partial class ModelPresentationFacade : FacadeBase
{
	/// <summary>
	/// モデル表示サービスを取得する
	/// </summary>
	public ModelPresentationService Service { get; }

	/// <summary>
	/// モデル表示機能を初期化する
	/// </summary>
	public ModelPresentationFacade()
	{
		Service = AddModule<ModelPresentationService>("ModelPresentationService");
	}
}