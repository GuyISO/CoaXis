using Godot;

/// <summary>
/// Viewer シーン全体の統括ポイントです。
/// IPC・UI・モデルローダー統合時の受け口として使用します。
/// </summary>
public partial class GameManager : Node
{
	#region Lifecycle

	/// <summary>
	/// ノード初期化時のフックです。
	/// </summary>
	public override void _Ready()
	{
	}

	/// <summary>
	/// 毎フレーム呼び出される更新フックです。
	/// </summary>
	/// <param name="delta">前フレームからの経過秒。</param>
	public override void _Process(double delta)
	{
	}

	#endregion
}
