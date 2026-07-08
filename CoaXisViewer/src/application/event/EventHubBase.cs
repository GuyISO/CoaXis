using Godot;

/// <summary>
/// EventHub 系 Autoload の共通基底クラス
/// </summary>
public abstract partial class EventHubBase<THub> : SingletonNodeBase<THub> where THub : EventHubBase<THub>
{
    #region Helper Methods

    /// <summary>
    /// シグナルを安全に発行するためのヘルパーメソッド
    /// </summary>
    /// <param name="signalName">発行するシグナルの名前</param>
    /// <param name="args">シグナルに渡す引数</param>
    /// <returns>シグナルの発行に成功した場合はtrue、それ以外の場合はfalseを返す</returns>
	protected static bool TryEmitSignal(StringName signalName, params Variant[] args)
    {
        if (Instance == null)
        {
            Warn($"{typeof(THub).Name} is not initialized. Skipped signal: {signalName}.");
            return false;
        }

        Instance.EmitSignal(signalName, args);
        Debug($"{typeof(THub).Name} emitted signal: {signalName}.");
        return true;
    }

    private static void Warn(string message)
    {
        if (Application.Instance?.System?.Log != null)
        {
            Application.Instance.System.Log.Warn(message);
            return;
        }

        GD.PrintErr(message);
    }

    private static void Debug(string message)
    {
        if (Application.Instance?.System?.Log != null)
        {
            Application.Instance.System.Log.Debug(message);
            return;
        }

        GD.Print(message);
    }

    #endregion
}