using Godot;

/// <summary>
/// FPS 表示用の UI、主に 開発中のパフォーマンス確認に使用する
/// </summary>
public partial class FpsUi : Label
{
    #region Lifecycle

    /// <summary>
    /// ノード初期化時のフック
    /// </summary>
    public override void _Ready()
    {
        Text = "fps: -";
    }

    /// <summary>
    /// 毎フレーム現在の FPS を表示する
    /// </summary>
    /// <param name="delta">前フレームからの経過秒</param>
    public override void _Process(double delta)
    {
        Text = $"fps: {Engine.GetFramesPerSecond()}";
    }

    #endregion
}



