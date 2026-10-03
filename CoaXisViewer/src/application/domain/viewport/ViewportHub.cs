using Godot;

/// <summary>
/// ビューポートの状態Hubを構成するルートハブ。
/// </summary>
public partial class ViewportHub : BaseHub
{
	#region Fields

	#endregion

	#region Properties

	/// <summary>カメラ状態と操作を管理するHubを取得する。</summary>
	internal ViewportCameraHub Camera { get; private set; } = null!;

	/// <summary>レイヤー表示状態を管理するHubを取得する。</summary>
	internal ViewportDisplayHub Display { get; private set; } = null!;

	/// <summary>操作モードと操作補助表示を管理するHubを取得する。</summary>
	internal ViewportInteractionHub Interaction { get; private set; } = null!;

    #endregion

	#region Lifecycle

    /// <summary>
    /// ビューポートの状態別Hubを生成する。
    /// </summary>
	public override void _Ready()
	{
        Camera = AddModule<ViewportCameraHub>("Camera");
        Display = AddModule<ViewportDisplayHub>("Display");
        Interaction = AddModule<ViewportInteractionHub>("Interaction");
	}

	#endregion

    #region Actions

    #endregion

    #region Notifications

    #endregion

    #region Events

    #endregion

    #region Methods

    #endregion

    #region Helpers

    #endregion
}