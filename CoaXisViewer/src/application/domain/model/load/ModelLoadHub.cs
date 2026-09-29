// TODO: リファクタリング確認後に削除
/// <summary>
/// モデルロード関連モジュールの構成を管理するハブ。
/// </summary>
public partial class ModelLoadHub : BaseHub
{
	#region Fields

	#endregion

	#region Properties

	public ModelLoadEntityHub Entity { get; }
	public ModelLoadPropertyHub Property { get; }
	public ModelLoadSceneHub Scene { get; }

	#endregion

	#region Lifecycle

	/// <summary>
	/// モデルロードを担当する各モジュールを初期化する。
	/// </summary>
	public ModelLoadHub()
	{
		Entity = AddModule<ModelLoadEntityHub>("Entity");
		Property = AddModule<ModelLoadPropertyHub>("Property");
		Scene = AddModule<ModelLoadSceneHub>("Scene");
	}

	#endregion

	#region Actions

	#endregion

	#region Notifications

	#endregion

	#region Events

	#endregion

	#region Methods

	#endregion

	#region Helpers

	#endregion
}