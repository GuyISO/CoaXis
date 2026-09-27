/// <summary>
/// Application 経由でモデル状態機能を利用するためのファサード
/// </summary>
public partial class ModelStateFacade : FacadeBase
{
	/// <summary>
	/// モデル状態サービスを取得する
	/// </summary>
	public ModelStateService Service { get; }

	/// <summary>
	/// モデル状態機能を初期化する
	/// </summary>
	public ModelStateFacade()
	{
		Service = AddModule<ModelStateService>("ModelStateService");
	}
}