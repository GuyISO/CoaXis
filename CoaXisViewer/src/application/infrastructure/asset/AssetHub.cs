/// <summary>
/// アセット機能を担当する各モジュールの構成を管理するハブ。
/// </summary>
public partial class AssetHub : BaseHub
{
    #region Fields

    #endregion

    #region Properties

    /// <summary>アイコンハブを取得する。</summary>
    public AssetIconHub Icon { get; }
    /// <summary>シーンハブを取得する。</summary>
    public AssetSceneHub Scene { get; }
    /// <summary>マテリアルハブを取得する。</summary>
    public AssetMaterialHub Material { get; }

    #endregion

    #region Signals

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

    #region Events

    #endregion

    #region Methods

    #endregion

    #region Helpers

    #endregion
}
