// TODO: リファクタリング確認後に削除
/// <summary>
/// ModelEntity 関連モジュールの構成を管理するハブ。
/// </summary>
public partial class ModelEntityHub : BaseHub
{
    #region Fields

    #endregion

    #region Properties

    /// <summary>
    /// ModelEntity の識別子と階層を管理するレジストリを取得する。
    /// </summary>
    public ModelEntityRegistryHub Registry { get; }
    public ModelEntitySelectionHub Selection { get; }
    public ModelEntityTreeHub Tree { get; }

    #endregion

    #region Lifecycle

    /// <summary>
    /// ModelEntity 機能を担当する各モジュールを初期化する。
    /// </summary>
    public ModelEntityHub()
    {
        Registry = AddModule<ModelEntityRegistryHub>("Registry");
        Selection = AddModule<ModelEntitySelectionHub>("Selection");
        Tree = AddModule<ModelEntityTreeHub>("Tree");
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