/// <summary>
/// アプリケーション全体のモジュール構成を管理するハブ。
/// </summary>
public partial class Application : BaseHub
{
    #region Fields

    // infrastructure
    /// <summary>ログハブの実体。</summary>
    private LogHub _log;
    /// <summary>設定ハブの実体。</summary>
    private SettingHub _setting;
    /// <summary>アセットハブの実体。</summary>
    private AssetHub _asset;
    /// <summary>IPCハブの実体。</summary>
    private IpcHub _ipc;
    /// <summary>入力ハブの実体。</summary>
    private InputHub _input; 

    // domain
    /// <summary>コマンドハブの実体。</summary>
    private CommandHub _command;
    /// <summary>モデルハブの実体。</summary>
    private ModelHub _model;
    /// <summary>UIハブの実体。</summary>
    private UiHub _ui;
    /// <summary>ビューポートハブの実体。</summary>
    private ViewportHub _viewport;

    #endregion

    #region Properties

    /// <summary>現在の Application インスタンスを取得する。</summary>
    public static Application Instance { get; private set; }

    // infrastructure
    /// <summary>ログ機能を取得する。</summary>
    public static LogHub Log => Instance._log;
    /// <summary>設定機能を取得する。</summary>
    public static SettingHub Setting => Instance._setting;
    /// <summary>アセット機能を取得する。</summary>
    public static AssetHub Asset => Instance._asset;
    /// <summary>IPC 機能を取得する。</summary>
    public static IpcHub Ipc => Instance._ipc;
    /// <summary>入力機能を取得する。</summary>
    public static InputHub Input => Instance._input; 

    // domain
    /// <summary>コマンド機能を取得する。</summary>
    public static CommandHub Command => Instance._command;
    /// <summary>モデル機能を取得する。</summary>
    public static ModelHub Model => Instance._model;
    /// <summary>UI 機能を取得する。</summary>
    public static UiHub Ui => Instance._ui;
    /// <summary>ビューポート機能を取得する。</summary>
    public static ViewportHub Viewport => Instance._viewport;

    #endregion

    #region Signals

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

    #region Events

    #endregion

    #region Methods

    #endregion

    #region Helpers

    /// <summary>
    /// インフラ系モジュールを初期化する。
    /// 依存関係を考慮して順序指定して初期化する。
    /// </summary>
    private void EnsureInfrastructureModules()
    {
        _log = AddModule<LogHub>("Log");
        _setting = AddModule<SettingHub>("Setting");
        _asset = AddModule<AssetHub>("Asset");
        _ipc = AddModule<IpcHub>("Ipc");
        _input = AddModule<InputHub>("Input");
    }

    /// <summary>
    /// ドメイン系モジュールを初期化する。
    /// 依存関係の考慮は不要なため順序は自由である。
    /// </summary>
    private void EnsureDomainModules()
    {
        _command = AddModule<CommandHub>("Command");
        _model = AddModule<ModelHub>("Model");
        _ui = AddModule<UiHub>("Ui");
        _viewport = AddModule<ViewportHub>("Viewport");
    }

    #endregion
}