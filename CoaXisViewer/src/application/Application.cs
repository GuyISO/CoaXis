using Godot;

/// <summary>
/// AutoLoad 登録ノードのエントリポイント。
/// </summary>
public partial class Application : FacadeBase
{
    #region Fields

    // infrastructure
    private LogHub _logHub;
    private SettingFacade _settingFacade;
    private AssetFacade _assetFacade;
    private DeviceInputHandler _deviceInputHandler;

    // domain
    private MeasurementFacade _measurementFacade;
    private ModelFacade _modelFacade;
    private PickFacade _pickFacade;
    private SelectionFacade _selectionFacade;
    private UiFacade _uiFacade;
    private ViewportFacade _viewportFacade;

    #endregion

    #region Properties

    public static Application Instance { get; private set; }

    // infrastructure
    public static LogHub Log => Instance._logHub;
    public static SettingFacade Setting => Instance._settingFacade;
    public static AssetFacade Asset => Instance._assetFacade;
    public static DeviceInputHandler DeviceInputHandlerNode => Instance._deviceInputHandler;

    // domain
    public static MeasurementFacade Measurement => Instance._measurementFacade;
    public static ModelFacade Model => Instance._modelFacade;
    public static PickFacade Pick => Instance._pickFacade;
    public static SelectionFacade Selection => Instance._selectionFacade;
    public static UiFacade Ui => Instance._uiFacade;
    public static ViewportFacade Viewport => Instance._viewportFacade;

    #endregion

    #region Lifecycle

    public override void _EnterTree()
    {
        Instance = this;

        EnsureInfrastructureModules();
        EnsureDomainModules();
    }

    public override void _ExitTree()
    {
        Instance = null;

        base._ExitTree();
    }

    #endregion

    #region Internal Helpers

    /// <summary>
    /// 依存関係を考慮してモジュールを初期化する。
    /// </summary>
    private void EnsureInfrastructureModules()
    {
        _logHub = AddModule<LogHub>("LogHub");
        _settingFacade = AddModule<SettingFacade>("SettingFacade");
        _assetFacade = AddModule<AssetFacade>("AssetFacade");
        _deviceInputHandler = AddModule<DeviceInputHandler>("DeviceInputHandler");
    }

    /// <summary>
    /// モジュールを初期化する。
    /// </summary>
    private void EnsureDomainModules()
    {
        _measurementFacade = AddModule<MeasurementFacade>("MeasurementFacade");
        _modelFacade = AddModule<ModelFacade>("ModelFacade");
        _pickFacade = AddModule<PickFacade>("PickFacade");
        _selectionFacade = AddModule<SelectionFacade>("SelectionFacade");
        _uiFacade = AddModule<UiFacade>("UiFacade");
        _viewportFacade = AddModule<ViewportFacade>("ViewportFacade");
    }

    #endregion
}