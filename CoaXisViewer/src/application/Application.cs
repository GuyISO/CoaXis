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
    private IpcFacade _ipcFacade;
    private DeviceInputHandler _deviceInputHandler;

    // domain
    private CommandFacade _commandFacade;
    private Measurement _measurement;
    private ModelFacade _modelFacade;
    private Pick _pick;
    private Selection _selection;
    private UiFacade _uiFacade;
    private ViewportFacade _viewportFacade;

    #endregion

    #region Properties

    public static Application Instance { get; private set; }

    // infrastructure
    public static LogHub Log => Instance._logHub;
    public static SettingFacade Setting => Instance._settingFacade;
    public static AssetFacade Asset => Instance._assetFacade;
    public static IpcFacade Ipc => Instance._ipcFacade;
    public static DeviceInputHandler DeviceInputHandler => Instance._deviceInputHandler;

    // domain
    public static CommandFacade Command => Instance._commandFacade;
    public static Measurement Measurement => Instance._measurement;
    public static ModelFacade Model => Instance._modelFacade;
    public static Pick Pick => Instance._pick;
    public static Selection Selection => Instance._selection;
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
        _ipcFacade = AddModule<IpcFacade>("IpcFacade");
        _deviceInputHandler = AddModule<DeviceInputHandler>("DeviceInputHandler");
    }

    /// <summary>
    /// モジュールを初期化する。
    /// </summary>
    private void EnsureDomainModules()
    {
        _commandFacade = AddModule<CommandFacade>("CommandFacade");
        _measurement = AddModule<Measurement>("MeasurementFacade");
        _modelFacade = AddModule<ModelFacade>("ModelFacade");
        _pick = AddModule<Pick>("PickFacade");
        _selection = AddModule<Selection>("Selection");
        _uiFacade = AddModule<UiFacade>("UiFacade");
        _viewportFacade = AddModule<ViewportFacade>("ViewportFacade");
    }

    #endregion
}