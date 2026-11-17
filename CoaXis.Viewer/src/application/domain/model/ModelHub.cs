/// <summary>
/// モデル関連モジュールの構成を管理するハブ。
/// </summary>
public partial class ModelHub : BaseHub
{
    #region Fields

    #endregion

    #region Properties

    /// <summary>ModelEntity 機能を取得する。</summary>
    public ModelEntityHub Entity { get; }
    /// <summary>ModelProperty 機能を取得する。</summary>
    public ModelPropertyHub Property { get; }
    /// <summary>共通のモデル選択機能を取得する。</summary>
    public ModelSelectionHub Selection { get; }
    /// <summary>EntityとPropertyにまたがるモデル集合操作を取得する。</summary>
    public ModelSetHub ModelSet { get; }
    /// <summary>Entityと所属Propertyの検索機能を取得する。</summary>
    public ModelSearchHub Search { get; }

    #endregion

    #region Signals

    #endregion

    #region Lifecycle

    /// <summary>
    /// モデル機能を担当する各モジュールを初期化する。
    /// </summary>
    public ModelHub()
    {
        Entity = AddModule<ModelEntityHub>("Entity");
        Property = AddModule<ModelPropertyHub>("Property");
        Selection = AddModule<ModelSelectionHub>("Selection");
        ModelSet = AddModule<ModelSetHub>("Set");
        Search = AddModule<ModelSearchHub>("Search");
    }

    #endregion

    #region Events

    #endregion

    #region Methods

    #endregion

    #region Helpers

    #endregion
}