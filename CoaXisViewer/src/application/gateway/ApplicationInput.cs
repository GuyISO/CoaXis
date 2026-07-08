/// <summary>
/// Application 経由で入力モジュールへアクセスするためのゲートウェイ
/// </summary>
public sealed class ApplicationInput
{
    private readonly Application _app;

    public DeviceInputHandler Device => _app.DeviceInputHandler;

    public ApplicationInput(Application app)
    {
        _app = app;
    }
}
