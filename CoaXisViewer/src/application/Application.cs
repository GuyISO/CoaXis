using Godot;

/// <summary>
/// AutoLoad 登録ノードのエントリポイント。
/// 旧 AutoLoad シングルトンをモジュールとして子ノードに集約する。
/// </summary>
public partial class Application : Node
{
    #region Fields

    private MeasurementEvent _measurementEvent;
    private ModelEventHub _modelEventHub;
    private PickEventHub _pickEventHub;
    private ViewportEventHub _viewportEventHub;

    private MeasurementFacade _measurementFacade;

    // singleton nodes
    private MeasurementService _measurementService;
    private ModelOperationService _modelOperationService;
    private ModelVisualService _modelVisualService;
    private SelectionService _selectionService;
    private SettingService _settingService;
    private UiManager _uiManager;

    private DeviceInputHandler _deviceInputHandler;

    #endregion

    #region Properties

    public static Application Instance { get; private set; }
    
    public static LogHub Logger => Instance.LogHub;

    public static MeasurementFacade Measurement => Instance._measurementFacade;

    public static ModelEventHub Model => Instance._modelEventHub;
    public static PickEventHub Pick => Instance._pickEventHub;
    public static ViewportEventHub Viewport => Instance._viewportEventHub;

    public static SelectionService Selection => Instance._selectionService;
    public static AssetManager Asset => Instance.AssetManager;
    public static UiManager Ui => Instance._uiManager;

    // System
    internal LogHub LogHub { get; private set; }
    internal AssetManager AssetManager { get; private set; }

    // Service
    internal MeasurementEvent MeasurementEventNode => _measurementEvent;
    internal MeasurementService MeasurementServiceNode => _measurementService;
    internal ModelOperationService ModelOperationServiceNode => _modelOperationService;
    internal ModelVisualService ModelVisualServiceNode => _modelVisualService;
    internal SelectionService SelectionServiceNode => _selectionService;
    internal SettingService SettingServiceNode => _settingService;
    internal UiManager UiManagerNode => _uiManager;

    // Input
    internal DeviceInputHandler DeviceInputHandlerNode => _deviceInputHandler;

    #endregion

    #region Lifecycle

    public override void _EnterTree()
    {
        Instance = this;

        EnsureModules();

        _measurementFacade = new MeasurementFacade();
        _modelEventHub = new ModelEventHub();
        _pickEventHub = new PickEventHub();
        _viewportEventHub = new ViewportEventHub();

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
        // System
        LogHub = AddModule<LogHub>("LogHub");
        AssetManager = AddModule<AssetManager>("AssetManager");

        // Event
        _measurementEvent = AddModule<MeasurementEvent>("MeasurementEvent");
        _modelEventHub = AddModule<ModelEventHub>("ModelEventHub");
        _pickEventHub = AddModule<PickEventHub>("PickEventHub");
        _viewportEventHub = AddModule<ViewportEventHub>("ViewportEventHub");

        // Service
        _measurementService = AddModule<MeasurementService>("MeasurementService");
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