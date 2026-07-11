using Godot;

/// <summary>
/// EventHub 系 Autoload の共通基底クラス
/// </summary>
public abstract partial class EventHubBase<THub> : Node where THub : EventHubBase<THub>
{
    #region Helper Methods

    /// <summary>
    /// シグナルを発行するためのヘルパーメソッド
    /// </summary>
    /// <param name="signalName">発行するシグナルの名前</param>
    /// <param name="args">シグナルに渡す引数</param>
	protected void Emit(StringName signalName, params Variant[] args)
    {
        EmitSignal(signalName, args);
        Application.Logger.Debug($"{typeof(THub).Name} emitted signal: {signalName}.");
    }

    #endregion
}