// TODO: リファクタリング確認後に削除
/// <summary>
/// モデル関連モジュールの構成を管理するハブ。
/// </summary>
public partial class ModelHub : BaseHub
{
    #region Fields

    #endregion

    #region Properties

    public ModelRegistry Registry { get; }
    public ModelLoadHub Load { get; }
    public ModelVisualHub Visual { get; }
    public ModelStateHub State { get; }
    public ModelTreeHub Tree { get; }

    #endregion

    #region Lifecycle

    /// <summary>
    /// モデル機能を担当する各モジュールを初期化する。
    /// </summary>
    public ModelHub()
    {
        Registry = AddModule<ModelRegistry>("Registry");
        Load = AddModule<ModelLoadHub>("Load");
        Visual = AddModule<ModelVisualHub>("Visual");
        State = AddModule<ModelStateHub>("State");
        Tree = AddModule<ModelTreeHub>("Tree");
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