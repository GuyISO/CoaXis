using Godot;

/// <summary>
/// AutoLoad 登録ノードのエントリポイント。
/// 旧 AutoLoad シングルトンをモジュールとして子ノードに集約する。
/// </summary>
public partial class Application : SingletonNodeBase<Application>
{
    #region Fields

    private ModelEventHub _modelEventHub;

    #endregion

    #region Properties

    // modules
    public ModelEventHub ModelEventHub { get; private set; }
    public PickEventHub PickEventHub { get; private set; }
    public ViewportEventHub ViewportEventHub { get; private set; }
    public MeasurementEventHub MeasurementEventHub { get; private set; }
    public Selection Selection { get; private set; }
    public ModelOperationService ModelOperationService { get; private set; }
    public ModelVisualService ModelVisualService { get; private set; }
    public MeasurementService MeasurementService { get; private set; }
    public SettingsService SettingsService { get; private set; }
    public UiManager UiManager { get; private set; }
    public AssetManager AssetManager { get; private set; }
    public DeviceInputHandler DeviceInputHandler { get; private set; }
    public LogHub LogHub { get; private set; }

    // gateways
    public ApplicationEvents Events { get; private set; }
    public ApplicationServices Services { get; private set; }
    public ApplicationSystem System { get; private set; }
    public ApplicationInput Input { get; private set; }

    #endregion

    #region Lifecycle

    public override void _EnterTree()
    {
        base._EnterTree();
        EnsureModules();

        Events = new ApplicationEvents(this);
        Services = new ApplicationServices(this);
        System = new ApplicationSystem(this);
        Input = new ApplicationInput(this);
    }

    #endregion

    #region Private Methods

    private void EnsureModules()
    {
        // 依存関係を考慮して初期化順を固定する。
        LogHub = AddModule<LogHub>("LogHub");

        ModelEventHub = AddModule<ModelEventHub>("ModelEventHub");
        PickEventHub = AddModule<PickEventHub>("PickEventHub");
        ViewportEventHub = AddModule<ViewportEventHub>("ViewportEventHub");
        MeasurementEventHub = AddModule<MeasurementEventHub>("MeasurementEventHub");

        Selection = AddModule<Selection>("Selection");
        ModelOperationService = AddModule<ModelOperationService>("ModelOperationService");
        ModelVisualService = AddModule<ModelVisualService>("ModelVisualService");
        MeasurementService = AddModule<MeasurementService>("MeasurementService");
        SettingsService = AddModule<SettingsService>("SettingsService");
        UiManager = AddModule<UiManager>("UiManager");
        AssetManager = AddModule<AssetManager>("AssetManager");
        DeviceInputHandler = AddModule<DeviceInputHandler>("DeviceInputHandler");
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