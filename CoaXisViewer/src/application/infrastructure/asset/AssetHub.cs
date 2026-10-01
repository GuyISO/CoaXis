// TODO: リファクタリング確認後に削除

/// <summary>
/// アセット機能を担当する各モジュールの構成を管理するハブ。
/// </summary>
public partial class AssetHub : BaseHub
{
    #region Fields

    #endregion

    #region Properties

    public AssetIconHub Icon { get; }
    public AssetSceneHub Scene { get; }
    public AssetMaterialHub Material { get; }

    #endregion

    #region Lifecycle

    /// <summary>
    /// アセット機能を担当する各モジュールを初期化する。
    /// </summary>
    public AssetHub()
    {
        Icon = AddModule<AssetIconHub>("Icon");
        Scene = AddModule<AssetSceneHub>("Scene");
        Material = AddModule<AssetMaterialHub>("Material");
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
