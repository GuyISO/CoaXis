using Godot;

/// <summary>
/// FPS 表示用の UI です。
/// </summary>
public partial class FpsUi : Label
{
	#region Lifecycle

	/// <summary>
	/// ノード初期化時のフックです。
	/// </summary>
	public override void _Ready()
	{
	}

	/// <summary>
	/// 毎フレーム現在の FPS を表示します。
	/// </summary>
	/// <param name="delta">前フレームからの経過秒。</param>
	public override void _Process(double delta)
	{
        Text = $"Frame: {Engine.GetFramesPerSecond()}";
	}

	#endregion
}



