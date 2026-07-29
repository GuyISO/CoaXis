using Godot;

/// <summary>
/// カメラやビューの状態を制御するサービス。
/// </summary>
public partial class ViewportService : Node
{
	#region Fields

	// 現在のビューポート操作モードを保持する
	private ViewportInteractionMode _interactionMode = ViewportInteractionMode.None;

	#endregion

	#region Properties

	/// <summary>
	/// 現在のビューポート操作モードを取得する
	/// </summary>
	internal ViewportInteractionMode InteractionMode => _interactionMode;

	#endregion

	#region Public API

	/// <summary>
	/// ビューポート操作モードを更新し、変更があれば通知する
	/// </summary>
	/// <param name="mode">新しい操作モード</param>
	internal void SetInteractionMode(ViewportInteractionMode mode)
	{
		if (_interactionMode == mode)
		{
			return;
		}

		_interactionMode = mode;
		Application.Viewport.Event.NotifyInteractionMode(mode);
	}

	#endregion

}