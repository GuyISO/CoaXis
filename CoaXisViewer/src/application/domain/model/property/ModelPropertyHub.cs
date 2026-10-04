/// <summary>
/// ModelProperty 関連モジュールの構成を管理するハブ。
/// </summary>
public partial class ModelPropertyHub : BaseHub
{
    #region Fields

    #endregion

    #region Properties

    /// <summary>
    /// ModelProperty の識別子と階層を管理するレジストリを取得する。
    /// </summary>
    public ModelPropertyRegistryHub Registry { get; }
    /// <summary>
    /// ModelProperty のロード機能を取得する。
    /// </summary>
    public ModelPropertyLoadHub Load { get; }

    #endregion

    #region Lifecycle

    /// <summary>
    /// ModelProperty 機能を担当するモジュールを初期化する。
    /// </summary>
    public ModelPropertyHub()
    {
        Registry = AddModule<ModelPropertyRegistryHub>("Registry");
        Load = AddModule<ModelPropertyLoadHub>("Load");
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