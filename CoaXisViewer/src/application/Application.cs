using Godot;

/// <summary>
/// AutoLoad 登録ノードのエントリポイント。
/// </summary>
public partial class Application : Node
{
    #region Fields

    private LogFacade _logFacade;
    private MeasurementFacade _measurementFacade;

    private ModelEvent _modelEvent;
    private PickEvent _pickEvent;
    private ViewportEvent _viewportEvent;

    private AssetManager _assetManager;

    // singleton nodes
    private ModelOperationService _modelOperationService;
    private ModelVisualService _modelVisualService;
    private SelectionService _selectionService;
    private SettingService _settingService;
    private UiManager _uiManager;

    private DeviceInputHandler _deviceInputHandler;

    #endregion

    #region Properties

    public static Application Instance { get; private set; }
    
    public static LogFacade Log => Instance._logFacade;
    public static MeasurementFacade Measurement => Instance._measurementFacade;

    public static ModelEvent Model => Instance._modelEvent;
    public static PickEvent Pick => Instance._pickEvent;
    public static ViewportEvent Viewport => Instance._viewportEvent;

    public static SelectionService Selection => Instance._selectionService;
    public static AssetManager Asset => Instance._assetManager;
    public static UiManager Ui => Instance._uiManager;

    // Service
    public static ModelOperationService ModelOperationServiceNode => Instance._modelOperationService;
    public static ModelVisualService ModelVisualServiceNode => Instance._modelVisualService;
    public static SelectionService SelectionServiceNode => Instance._selectionService;
    public static SettingService SettingServiceNode => Instance._settingService;
    public static UiManager UiManagerNode => Instance._uiManager;

    // Input
    public static DeviceInputHandler DeviceInputHandlerNode => Instance._deviceInputHandler;

    #endregion

    #region Lifecycle

    public override void _EnterTree()
    {
        Instance = this;

        EnsureModules();
    }

    public override void _ExitTree()
    {
        Instance = null;
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 依存関係を考慮してモジュールを初期化する。
    /// </summary>
    private void EnsureModules()
    {
        _logFacade = AddModule<LogFacade>("LogFacade");

        _measurementFacade = AddModule<MeasurementFacade>("MeasurementFacade");

        // System
        _assetManager = AddModule<AssetManager>("AssetManager");

        // Event
        _modelEvent = AddModule<ModelEvent>("ModelEvent");
        _pickEvent = AddModule<PickEvent>("PickEvent");
        _viewportEvent = AddModule<ViewportEvent>("ViewportEvent");

        // Service
        _modelOperationService = AddModule<ModelOperationService>("ModelOperationService");
        _modelVisualService = AddModule<ModelVisualService>("ModelVisualService");
        _selectionService = AddModule<SelectionService>("SelectionService");
        _settingService = AddModule<SettingService>("SettingService");
        _uiManager = AddModule<UiManager>("UiManager");

        // Input
        _deviceInputHandler = AddModule<DeviceInputHandler>("DeviceInputHandler");
    }

    private TModule AddModule<TModule>(string nodeName) where TModule : Node, new()
    {
        TModule existingModule = GetNodeOrNull<TModule>(nodeName);
        if (existingModule != null)
        {
            return existingModule;
        }

        TModule module = new TModule
        {
            Name = nodeName
        };

        AddChild(module);
        return module;
    }

    #endregion
}