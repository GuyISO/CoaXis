using Godot;

/// <summary>
/// AutoLoad 登録ノードのエントリポイント。
/// 旧 AutoLoad シングルトンをモジュールとして子ノードに集約する。
/// </summary>
public partial class Application : Node
{
    #region Fields

    // gateways
    private ApplicationLogHub _applicationLogHub;
    private ApplicationAssetManager _applicationAssetManager;
    
    private ApplicationMeasurementEventHub _applicationMeasurementEventHub;
    private ApplicationModelEventHub _applicationModelEventHub;
    private ApplicationPickEventHub _applicationPickEventHub;
    private ApplicationViewportEventHub _applicationViewportEventHub;

    private ApplicationServices _applicationServices;
    private ApplicationInput _applicationInput;

    // singleton nodes
    private MeasurementService _measurementService;
    private ModelOperationService _modelOperationService;
    private ModelVisualService _modelVisualService;
    private Selection _selection;
    private SettingsService _settingsService;
    private UiManager _uiManager;

    private DeviceInputHandler _deviceInputHandler;

    #endregion

    #region Properties

    public static Application Instance { get; private set; }

    // static facade gateways
    public static ApplicationLogHub Logger => Instance._applicationLogHub;
    public static ApplicationAssetManager Asset => Instance._applicationAssetManager;
    
    public static ApplicationMeasurementEventHub Measurement => Instance._applicationMeasurementEventHub;
    public static ApplicationModelEventHub Model => Instance._applicationModelEventHub;
    public static ApplicationPickEventHub Pick => Instance._applicationPickEventHub;
    public static ApplicationViewportEventHub Viewport => Instance._applicationViewportEventHub;

    public static ApplicationServices Service => Instance._applicationServices;
    public static ApplicationInput Input => Instance._applicationInput;

    // System
    internal LogHub LogHub { get; private set; }
    internal AssetManager AssetManager { get; private set; }

    // Event
    internal MeasurementEventHub MeasurementEventHub { get; private set; }
    internal ModelEventHub ModelEventHub { get; private set; }
    internal PickEventHub PickEventHub { get; private set; }
    internal ViewportEventHub ViewportEventHub { get; private set; }

    // Service
    internal MeasurementService MeasurementServiceNode => _measurementService;
    internal ModelOperationService ModelOperationServiceNode => _modelOperationService;
    internal ModelVisualService ModelVisualServiceNode => _modelVisualService;
    internal Selection SelectionNode => _selection;
    internal SettingsService SettingsServiceNode => _settingsService;
    internal UiManager UiManagerNode => _uiManager;

    // Input
    internal DeviceInputHandler DeviceInputHandlerNode => _deviceInputHandler;

    #endregion

    #region Lifecycle

    public override void _EnterTree()
    {
        Instance = this;
        EnsureModules();

        _applicationLogHub = new ApplicationLogHub();
        _applicationAssetManager = new ApplicationAssetManager();
        
        _applicationMeasurementEventHub = new ApplicationMeasurementEventHub();
        _applicationModelEventHub = new ApplicationModelEventHub();
        _applicationPickEventHub = new ApplicationPickEventHub();
        _applicationViewportEventHub = new ApplicationViewportEventHub();

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

    #region Private Methods

    /// <summary>
    /// 依存関係を考慮してモジュールを初期化する。
    /// </summary>
    private void EnsureModules()
    {
        // System
        LogHub = AddModule<LogHub>("LogHub");
        AssetManager = AddModule<AssetManager>("AssetManager");

        // Event
        MeasurementEventHub = AddModule<MeasurementEventHub>("MeasurementEventHub");
        ModelEventHub = AddModule<ModelEventHub>("ModelEventHub");
        PickEventHub = AddModule<PickEventHub>("PickEventHub");
        ViewportEventHub = AddModule<ViewportEventHub>("ViewportEventHub");

        // Service
        _measurementService = AddModule<MeasurementService>("MeasurementService");
        _modelOperationService = AddModule<ModelOperationService>("ModelOperationService");
        _modelVisualService = AddModule<ModelVisualService>("ModelVisualService");
        _selection = AddModule<Selection>("Selection");
        _settingsService = AddModule<SettingsService>("SettingsService");
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