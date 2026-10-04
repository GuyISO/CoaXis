// TODO: リファクタリング確認後に削除
/// <summary>
/// モデル関連モジュールの構成を管理するハブ。
/// </summary>
public partial class ModelHub : BaseHub
{
    #region Fields

    #endregion

    #region Properties

    public ModelEntityHub Entity { get; }
    public ModelRegistry Registry { get; }
    public ModelLoadHub Load { get; }
    public ModelVisualHub Visual { get; }
    public ModelStateHub State { get; }

    #endregion

    #region Lifecycle

    /// <summary>
    /// モデル機能を担当する各モジュールを初期化する。
    /// </summary>
    public ModelHub()
    {
        Entity = AddModule<ModelEntityHub>("Entity");
        Registry = AddModule<ModelRegistry>("Registry");
        Load = AddModule<ModelLoadHub>("Load");
        Visual = AddModule<ModelVisualHub>("Visual");
        State = AddModule<ModelStateHub>("State");
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