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
    private ApplicationEvents _applicationEvents;
    private ApplicationInput _applicationInput;
    private ApplicationServices _applicationServices;
    private ApplicationSystem _applicationSystem;

    // singleton nodes
    private MeasurementEventHub _measurementEventHub;
    private ModelEventHub _modelEventHub;
    private PickEventHub _pickEventHub;
    private ViewportEventHub _viewportEventHub;
    private DeviceInputHandler _deviceInputHandler;
    private MeasurementService _measurementService;
    private ModelOperationService _modelOperationService;
    private ModelVisualService _modelVisualService;
    private Selection _selection;
    private SettingsService _settingsService;
    private UiManager _uiManager;
    private AssetManager _assetManager;
    private LogHub _logHub;

    // modules
    // Nodeインスタンス不要論がわいたらこっちにうつす

    #endregion

    #region Properties

    internal ModelEventHub ModelEventHubNode => _modelEventHub;
    internal PickEventHub PickEventHubNode => _pickEventHub;
    internal ViewportEventHub ViewportEventHubNode => _viewportEventHub;
    internal MeasurementEventHub MeasurementEventHubNode => _measurementEventHub;
    internal Selection SelectionNode => _selection;
    internal SettingsService SettingsServiceNode => _settingsService;
    internal UiManager UiManagerNode => _uiManager;
    internal AssetManager AssetManagerNode => _assetManager;
    internal DeviceInputHandler DeviceInputHandlerNode => _deviceInputHandler;
    internal LogHub LogHubNode => _logHub;

    #endregion

    #region Lifecycle

    public override void _EnterTree()
    {
        Instance = this;
        EnsureModules();

        _applicationEvents = new ApplicationEvents(this);
        _applicationServices = new ApplicationServices(this);
        _applicationSystem = new ApplicationSystem(this);
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
    public static ApplicationEvents Events => Instance._applicationEvents;
    public static ApplicationInput Input => Instance._applicationInput;
    public static ApplicationServices Services => Instance._applicationServices;
    public static ApplicationSystem System => Instance._applicationSystem;

    // modules
    public static MeasurementService MeasurementService => Instance._measurementService;
    public static ModelEventHub ModelEventHub => Instance._modelEventHub;
    public static PickEventHub PickEventHub => Instance._pickEventHub;
    public static ViewportEventHub ViewportEventHub => Instance._viewportEventHub;
    public static Selection Selection => Instance._selection;
    public static ModelOperationService ModelOperationService => Instance._modelOperationService;
    public static ModelVisualService ModelVisualService => Instance._modelVisualService;
    public static SettingsService SettingsService => Instance._settingsService;
    public static UiManager UiManager => Instance._uiManager;
    public static AssetManager AssetManager => Instance._assetManager;
    public static DeviceInputHandler DeviceInputHandler => Instance._deviceInputHandler;
    public static LogHub LogHub => Instance._logHub;

    #endregion

    #region Private Methods

    private void EnsureModules()
    {
        // 依存関係を考慮して初期化順を固定する。
        _logHub = AddModule<LogHub>("LogHub");

        _modelEventHub = AddModule<ModelEventHub>("ModelEventHub");
        _pickEventHub = AddModule<PickEventHub>("PickEventHub");
        _viewportEventHub = AddModule<ViewportEventHub>("ViewportEventHub");
        _measurementEventHub = AddModule<MeasurementEventHub>("MeasurementEventHub");

        _selection = AddModule<Selection>("Selection");
        _measurementService = AddModule<MeasurementService>("MeasurementService");
        _modelOperationService = AddModule<ModelOperationService>("ModelOperationService");
        _modelVisualService = AddModule<ModelVisualService>("ModelVisualService");
        _settingsService = AddModule<SettingsService>("SettingsService");
        _uiManager = AddModule<UiManager>("UiManager");
        _assetManager = AddModule<AssetManager>("AssetManager");
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