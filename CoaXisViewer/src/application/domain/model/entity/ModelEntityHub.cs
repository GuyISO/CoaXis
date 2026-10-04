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
    /// <summary>
    /// ModelEntity の論理状態と変更要求を管理するハブを取得する。
    /// </summary>
    public ModelEntityStateHub State { get; }
    /// <summary>
    /// ModelEntity の表示状態と描画反映を管理するハブを取得する。
    /// </summary>
    public ModelEntityVisualHub Visual { get; }
    /// <summary>
    /// ModelEntity の選択機能を取得する。
    /// </summary>
    public ModelEntitySelectionHub Selection { get; }
    /// <summary>
    /// ModelEntity のツリー機能を取得する。
    /// </summary>
    public ModelEntityTreeHub Tree { get; }
    /// <summary>
    /// ModelEntity に対するピック機能を取得する。
    /// </summary>
    public ModelEntityPickHub Pick { get; }
    /// <summary>
    /// ModelEntity に対する測定機能を取得する。
    /// </summary>
    public ModelEntityMeasurementHub Measurement { get; }
    /// <summary>
    /// ModelEntity の置換ロード機能を取得する。
    /// </summary>
    public ModelEntityLoadHub Load { get; }
    /// <summary>
    /// ModelEntity のシーンロード機能を取得する。
    /// </summary>
    public ModelEntitySceneHub Scene { get; }

    #endregion

    #region Lifecycle

    /// <summary>
    /// ModelEntity 機能を担当する各モジュールを初期化する。
    /// </summary>
    public ModelEntityHub()
    {
        Registry = AddModule<ModelEntityRegistryHub>("Registry");
        State = AddModule<ModelEntityStateHub>("State");
        Visual = AddModule<ModelEntityVisualHub>("Visual");
        Selection = AddModule<ModelEntitySelectionHub>("Selection");
        Tree = AddModule<ModelEntityTreeHub>("Tree");
        Pick = AddModule<ModelEntityPickHub>("Pick");
        Measurement = AddModule<ModelEntityMeasurementHub>("Measurement");
        Load = AddModule<ModelEntityLoadHub>("Load");
        Scene = AddModule<ModelEntitySceneHub>("Scene");
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