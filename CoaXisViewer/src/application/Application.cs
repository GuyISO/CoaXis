using Godot;

/// <summary>
/// AutoLoad 登録ノードのエントリポイント。
/// 旧 AutoLoad シングルトンをモジュールとして子ノードに集約する。
/// </summary>
public partial class Application : Node
{
    #region Fields

    public static Application Instance { get; private set; }

    // gateways
    private ApplicationSystem _applicationSystem;
    private ApplicationEvents _applicationEvents;
    private ApplicationServices _applicationServices;
    private ApplicationInput _applicationInput;

    // singleton nodes
    private LogHub _logHub;

    private MeasurementEventHub _measurementEventHub;
    private ModelEventHub _modelEventHub;
    private PickEventHub _pickEventHub;
    private ViewportEventHub _viewportEventHub;

    private MeasurementService _measurementService;
    private ModelOperationService _modelOperationService;
    private ModelVisualService _modelVisualService;
    private Selection _selection;
    private SettingsService _settingsService;
    private UiManager _uiManager;
    private AssetManager _assetManager;

    private DeviceInputHandler _deviceInputHandler;

    #endregion

    #region Properties

    internal LogHub LogHubNode => _logHub;

    internal MeasurementEventHub MeasurementEventHubNode => _measurementEventHub;
    internal ModelEventHub ModelEventHubNode => _modelEventHub;
    internal PickEventHub PickEventHubNode => _pickEventHub;
    internal ViewportEventHub ViewportEventHubNode => _viewportEventHub;

    internal MeasurementService MeasurementServiceNode => _measurementService;
    internal ModelOperationService ModelOperationServiceNode => _modelOperationService;
    internal ModelVisualService ModelVisualServiceNode => _modelVisualService;
    internal AssetManager AssetManagerNode => _assetManager;
    internal Selection SelectionNode => _selection;
    internal SettingsService SettingsServiceNode => _settingsService;
    internal UiManager UiManagerNode => _uiManager;

    internal DeviceInputHandler DeviceInputHandlerNode => _deviceInputHandler;

    #endregion

    #region Lifecycle

    public override void _EnterTree()
    {
        Instance = this;
        EnsureModules();

        _applicationSystem = new ApplicationSystem(this);
        _applicationEvents = new ApplicationEvents(this);
        _applicationServices = new ApplicationServices(this);
        _applicationInput = new ApplicationInput(this);
    }

    public override void _ExitTree()
    {
        if (ReferenceEquals(Instance, this))
        {
            Instance = null;
        }
    }

    #endregion

    #region Public Methods

    // gateways (static facade)
    public static ApplicationSystem System => Instance._applicationSystem;
    public static ApplicationEvents Events => Instance._applicationEvents;
    public static ApplicationServices Services => Instance._applicationServices;
    public static ApplicationInput Input => Instance._applicationInput;

    // modules
    public static LogHub LogHub => Instance._logHub;

    public static MeasurementEventHub MeasurementEventHub => Instance._measurementEventHub;
    public static ModelEventHub ModelEventHub => Instance._modelEventHub;
    public static PickEventHub PickEventHub => Instance._pickEventHub;
    public static ViewportEventHub ViewportEventHub => Instance._viewportEventHub;

    public static AssetManager AssetManager => Instance._assetManager;
    public static MeasurementService MeasurementService => Instance._measurementService;
    public static ModelOperationService ModelOperationService => Instance._modelOperationService;
    public static ModelVisualService ModelVisualService => Instance._modelVisualService;
    public static Selection Selection => Instance._selection;
    public static SettingsService SettingsService => Instance._settingsService;
    public static UiManager UiManager => Instance._uiManager;

    public static DeviceInputHandler DeviceInputHandler => Instance._deviceInputHandler;

    #endregion

    #region Private Methods

    /// <summary>
    /// 依存関係を考慮してモジュールを初期化する。
    /// </summary>
    private void EnsureModules()
    {
        _logHub = AddModule<LogHub>("LogHub");

        _measurementEventHub = AddModule<MeasurementEventHub>("MeasurementEventHub");
        _modelEventHub = AddModule<ModelEventHub>("ModelEventHub");
        _pickEventHub = AddModule<PickEventHub>("PickEventHub");
        _viewportEventHub = AddModule<ViewportEventHub>("ViewportEventHub");

        _assetManager = AddModule<AssetManager>("AssetManager");
        _measurementService = AddModule<MeasurementService>("MeasurementService");
        _modelOperationService = AddModule<ModelOperationService>("ModelOperationService");
        _modelVisualService = AddModule<ModelVisualService>("ModelVisualService");
        _selection = AddModule<Selection>("Selection");
        _settingsService = AddModule<SettingsService>("SettingsService");
        _uiManager = AddModule<UiManager>("UiManager");

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