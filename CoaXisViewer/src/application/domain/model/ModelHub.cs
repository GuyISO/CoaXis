// TODO: リファクタリング確認後に削除
/// <summary>
/// モデル関連モジュールの構成を管理するハブ。
/// </summary>
public partial class ModelHub : BaseHub
{
    #region Fields

    #endregion

    #region Properties

    /// <summary>
    /// ModelEntity 機能を取得する。
    /// </summary>
    public ModelEntityHub Entity { get; }
    /// <summary>
    /// ModelProperty 機能を取得する。
    /// </summary>
    public ModelPropertyHub Property { get; }

    #endregion

    #region Lifecycle

    /// <summary>
    /// モデル機能を担当する各モジュールを初期化する。
    /// </summary>
    public ModelHub()
    {
        Entity = AddModule<ModelEntityHub>("Entity");
        Property = AddModule<ModelPropertyHub>("Property");
    }

    #endregion

    #region Signals

    #endregion

    #region Events

    #endregion

    #region Methods

    #endregion

    #region Helpers

    #endregion
}