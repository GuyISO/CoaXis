using Godot;

/// <summary>
/// AutoLoad 登録ノードのエントリポイント。
/// </summary>
public partial class Application : Node
{
    #region Fields

    // domain facades
    private LogFacade _logFacade;
    private SettingFacade _settingFacade;
    private ViewportFacade _viewportFacade;
    private ModelFacade _modelFacade;
    private PickFacade _pickFacade;
    private SelectionFacade _selectionFacade;
    private MeasurementFacade _measurementFacade;
    private UiFacade _uiFacade;

    private AssetFacade _assetFacade;

    // singleton nodes
    private ModelOperationService _modelOperationService;
    private ModelVisualService _modelVisualService;

    private DeviceInputHandler _deviceInputHandler;

    #endregion

    #region Properties

    public static Application Instance { get; private set; }

    // domain facades
    public static LogFacade Log => Instance._logFacade;
    public static SettingFacade Setting => Instance._settingFacade;
    public static ViewportFacade Viewport => Instance._viewportFacade;
    public static ModelFacade Model => Instance._modelFacade;
    public static PickFacade Pick => Instance._pickFacade;
    public static SelectionFacade Selection => Instance._selectionFacade;
    public static MeasurementFacade Measurement => Instance._measurementFacade;
    public static UiFacade Ui => Instance._uiFacade;
    public static AssetFacade Asset => Instance._assetFacade;

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
        _settingFacade = AddModule<SettingFacade>("SettingFacade");
        _viewportFacade = AddModule<ViewportFacade>("ViewportFacade");
        _modelFacade = AddModule<ModelFacade>("ModelFacade");
        _pickFacade = AddModule<PickFacade>("PickFacade");
        _selectionFacade = AddModule<SelectionFacade>("SelectionFacade");
        _measurementFacade = AddModule<MeasurementFacade>("MeasurementFacade");
        _uiFacade = AddModule<UiFacade>("UiFacade");
        _assetFacade = AddModule<AssetFacade>("AssetFacade");

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