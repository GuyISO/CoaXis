using Godot;

/// <summary>
/// Event 系 Autoload の共通基底クラス
/// </summary>
public abstract partial class EventBase<THub> : Node where THub : EventBase<THub>
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
        Application.Log.Service.Debug($"{typeof(THub).Name} emitted signal: {signalName}.");
    }

    #endregion
}