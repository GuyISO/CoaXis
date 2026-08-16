using Godot;

/// <summary>
/// カメラやビューの状態を制御するサービス。
/// </summary>
public partial class ViewportService : Node
{
	#region Fields

	private uint _activeLayers = (uint)ViewportLayer.Default | (uint)ViewportLayer.Visible;

	#endregion

	#region Lifecycle

	public override void _Ready()
	{
		SubscribeApplicationEvents();
	}

	public override void _ExitTree()
	{
		UnsubscribeApplicationEvents();

		base._ExitTree();
	}

	#endregion

	#region Events

	/// <summary>
	/// Applicationイベントの購読を開始する
	/// </summary>
	private void SubscribeApplicationEvents()
	{
		Application.Viewport.Event.AskStateRequested += OnAskStateRequested;
	}

	/// <summary>
	/// Applicationイベントの購読を解除する
	/// </summary>
	private void UnsubscribeApplicationEvents()
	{
		Application.Viewport.Event.AskStateRequested -= OnAskStateRequested;
	}

	/// <summary>
	/// ビューポート状態の通知要求を受け取ったときに現在の操作モードを返す
	/// </summary>
	private void OnAskStateRequested()
	{
		Application.Viewport.Event.NotifyInteractionMode(_interactionMode);
		Application.Viewport.Event.NotifyLayer(_activeLayers, true);
	}

	#endregion

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

	internal void SetLayerActive(uint layer, bool isActive)
	{
		if (isActive)
		{
			_activeLayers |= layer;
		}
		else
		{
			_activeLayers &= ~layer;
		}

		Application.Viewport.Event.NotifyLayer(layer, isActive);
	}

	#endregion

}