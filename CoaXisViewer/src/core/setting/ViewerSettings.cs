/// <summary>
/// ビューア全体の設定ルート。
/// セクション単位でプロパティを増やして拡張する。
/// </summary>
public sealed class ViewerSettings
{
    /// <summary>
    /// IPC 関連の設定セクション。
    /// </summary>
    public IpcSettings Ipc { get; set; } = new IpcSettings();

    /// <summary>
    /// カメラ操作に関する設定セクション。
    /// </summary>
    public CameraSettings Camera { get; set; } = new CameraSettings();

    /// <summary>
    /// 入力操作の感度に関する設定セクション。
    /// </summary>
    public InputSettings Input { get; set; } = new InputSettings();

    /// <summary>
    /// UI・描画に関する色設定セクション。
    /// </summary>
    public ColorSettings Color { get; set; } = new ColorSettings();

    /// <summary>
    /// UI一般の挙動に関する設定セクション。
    /// </summary>
    public UiSettings Ui { get; set; } = new UiSettings();

    /// <summary>
    /// 既定設定を生成する。
    /// </summary>
    /// <returns>安全に起動可能な最小設定</returns>
    public static ViewerSettings CreateDefault()
    {
        return new ViewerSettings
        {
            Ipc = new IpcSettings
            {
                PipeName = Constant.Ipc.PipeName,
                StartPipeServerOnReady = Constant.Ipc.StartPipeServerOnReady
            },
            Camera = new CameraSettings(),
            Input = new InputSettings(),
            Color = new ColorSettings(),
            Ui = new UiSettings()
        };
    }

    /// <summary>
    /// null や不正値を補正して利用可能な状態にする。
    /// </summary>
    public void Normalize()
    {
        Ipc ??= new IpcSettings();
        Ipc.Normalize();
        Camera ??= new CameraSettings();
        Camera.Normalize();
        Input ??= new InputSettings();
        Input.Normalize();
        Color ??= new ColorSettings();
        Color.Normalize();
        Ui ??= new UiSettings();
        Ui.Normalize();
    }
}
