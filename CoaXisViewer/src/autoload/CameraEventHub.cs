using Godot;

/// <summary>
/// カメラ関連のイベント集約ハブです。AutoLoadノードとしてシーンツリーに配置し、カメラの状態変更や操作のリクエストを通知するためのシグナルを提供します。これにより、カメラ操作のロジックを分散させずに一元管理できます。
/// Autoloadに登録してシングルトン参照することを前提としていますが、複数インスタンスが存在する可能性も考慮して実装されています。
/// </summary>
public partial class CameraEventHub : Node
{
	/// <summary>
	/// シングルトン参照です。
	/// </summary>
	public static CameraEventHub I { get; private set; }

	/// <summary>
	/// シーンツリー参加時にシングルトン参照を確立します。
	/// </summary>
	public override void _EnterTree()
	{
		// AutoLoad をデフォルト参照として維持するため、未設定時のみ I を確立する。
		if (I == null)
		{
			I = this;
		}
	}

	/// <summary>
	/// シーンツリー離脱時に、現在インスタンスがシングルトン参照なら解放します。
	/// </summary>
	public override void _ExitTree()
	{
		// 複数インスタンスが存在し得るため、自身が I の場合のみ解放する。
		if (ReferenceEquals(I, this))
		{
			I = null;
		}
	}

	#region --------------------------------------- Request ---------------------------------------

	[Signal] public delegate void NotifyStateRequestedEventHandler();
	/// <summary>
	/// カメラ関連の状態の通知をリクエストします。
	/// </summary>
	public void RequestNotifyState()
	{
		EmitSignal(SignalName.NotifyStateRequested);
	}

	[Signal] public delegate void MovePositionToRequestedEventHandler(Vector3 position, bool useTween);
	/// <summary>
	/// 注視点の位置の設定をリクエストします。
	/// </summary>
	/// <param name="position">設定する注視点の位置です。</param>
	/// <param name="useTween">移動にトゥイーンを使用するかどうかのフラグです。デフォルトは false です。</param>
	/// <remarks>SetPositionという名前にしたいところですが、Node3Dの標準メソッドと区別するためにRequestMovePositionToという名前にしています。</remarks>
	public void RequestMovePositionTo(Vector3 position, bool useTween = false)
	{
		EmitSignal(SignalName.MovePositionToRequested, position, useTween);
	}

	[Signal] public delegate void MoveRotationToRequestedEventHandler(Quaternion rotation, bool useTween);
	/// <summary>
	/// 注視点の回転の設定をリクエストします。
	/// </summary>
	/// <param name="rotation">設定する注視点の回転です。</param>
	/// <param name="useTween">移動にトゥイーンを使用するかどうかのフラグです。デフォルトは false です。</param>
	/// <remarks>SetRotationという名前にしたいところですが、Node3Dの標準メソッドと区別するためにRequestMoveRotationToという名前にしています。</remarks>
	public void RequestMoveRotationTo(Quaternion rotation, bool useTween = false)
	{
		EmitSignal(SignalName.MoveRotationToRequested, rotation, useTween);
	}

	[Signal] public delegate void SetDistanceRequestedEventHandler(float distance, bool useTween);
	/// <summary>
	/// カメラの距離の設定をリクエストします。
	/// </summary> <param name="distance">設定するカメラの距離です。透視投影の場合のみ有効です。</param>
	/// <param name="useTween">移動にトゥイーンを使用するかどうかのフラグです。デフォルトは false です。</param>
	public void RequestSetDistance(float distance, bool useTween = false)
	{
		EmitSignal(SignalName.SetDistanceRequested, distance, useTween);
	}

	[Signal] public delegate void SetSizeRequestedEventHandler(float size, bool useTween);
	/// <summary>
	/// カメラのサイズの設定をリクエストします。
	/// </summary>
	/// <param name="size">設定するカメラのサイズです。平行投影の場合のみ有効です。</param>
	/// <param name="useTween">サイズ変更にトゥイーンを使用するかどうかのフラグです。デフォルトは false です。</param>
	public void RequestSetSizeTo(float size, bool useTween = false)
	{
		EmitSignal(SignalName.SetSizeRequested, size, useTween);
	}

	[Signal] public delegate void SetFovRequestedEventHandler(float fov, bool useTween);
	/// <summary>
	/// FOV（視野角）の設定をリクエストします。
	/// </summary>
	/// <param name="fov">設定する FOV の値です。</param>
	/// <param name="useTween">FOV変更にトゥイーンを使用するかどうかのフラグです。デフォルトは false です。</param>
	public void RequestSetFov(float fov, bool useTween = false)
	{
		EmitSignal(SignalName.SetFovRequested, fov, useTween);
	}

	[Signal] public delegate void TranslateRequestedEventHandler(Vector3 translation, bool useTween);
	/// <summary>
	/// カメラの平行移動をリクエストします。
	/// </summary> <param name="translation">カメラの平行移動量です。</param>
	/// <param name="useTween">移動にトゥイーンを使用するかどうかのフラグです。デフォルトは false です。</param>
	public void RequestTranslate(Vector3 translation, bool useTween = false)
	{
		EmitSignal(SignalName.TranslateRequested, translation, useTween);
	}

	[Signal] public delegate void RotateRequestedEventHandler(Quaternion rotation, bool useTween);
	/// <summary>
	/// カメラの回転をリクエストします。
	/// </summary>
	/// <param name="rotation">設定するカメラの回転です。</param>
	/// <param name="useTween">回転にトゥイーンを使用するかどうかのフラグです。デフォルトは false です。</param>
	public void RequestRotate(Quaternion rotation, bool useTween = false)
	{
		EmitSignal(SignalName.RotateRequested, rotation, useTween);
	}

	[Signal] public delegate void ZoomRequestedEventHandler(float exponent, bool useTween);
	/// <summary>
	/// カメラのズームをリクエストします。
	/// </summary>
	/// <param name="exponent">ズームの指数値です。1 より大きい値はズームイン、1 より小さい値はズームアウトを意味します。</param>
	/// <param name="useTween">ズームにトゥイーンを使用するかどうかのフラグです。デフォルトは false です。</param>
	public void RequestZoom(float exponent, bool useTween = false)
	{
		EmitSignal(SignalName.ZoomRequested, exponent, useTween);
	}

	[Signal] public delegate void SetProjectionTypeRequestedEventHandler(Camera3D.ProjectionType type);
	/// <summary>
	/// 投影タイプの設定をリクエストします。
	/// </summary>
	/// <param name="type">設定する投影タイプです。</param>
	public void RequestSetProjectionType(Camera3D.ProjectionType type)
	{
		EmitSignal(SignalName.SetProjectionTypeRequested, (int)type);
	}

	[Signal] public delegate void ToggleProjectionTypeRequestedEventHandler();
	/// <summary>
	/// 投影タイプの切り替えをリクエストします。
	/// </summary>
	public void RequestToggleProjectionType()
	{
		EmitSignal(SignalName.ToggleProjectionTypeRequested);
	}

	[Signal] public delegate void FitRequestedEventHandler(Node3D targetNode, bool useTween);
	/// <summary>
	/// カメラをシーン全体にフィットさせる操作をリクエストします。
	/// </summary>
	/// <param name="targetNode">フィットさせたいターゲットノードです。</param>
	/// <param name="useTween">フィット操作にトゥイーンを使用するかどうかのフラグです。デフォルトは false です。</param>
	public void RequestFit(Node3D targetNode, bool useTween = false)
	{
		EmitSignal(SignalName.FitRequested, targetNode, useTween);
	}

	[Signal] public delegate void AlignNormalToRequestedEventHandler(Vector3 normal, bool useTween);
	/// <summary>
	/// カメラの向きを指定した法線に合わせる操作をリクエストします。
	/// </summary>
	/// <param name="normal">カメラの向きを合わせたい法線ベクトルです。</param>
	/// <param name="useTween">回転にトゥイーンを使用するかどうかのフラグです。デフォルトは false です。</param>
	public void RequestAlignNormalTo(Vector3 normal, bool useTween = false)
	{
		EmitSignal(SignalName.AlignNormalToRequested, normal, useTween);
	}
	
	#endregion

	#region --------------------------------------- Notification ---------------------------------------

	[Signal] public delegate void ControlModeNotifiedEventHandler(CameraControlMode mode);
	/// <summary>
	/// カメラ操作モードを通知するシグナルです。
	/// </summary>
	/// <param name="mode">カメラ操作モードです。</param>
	public void NotifyControlMode(CameraControlMode mode)
	{
		EmitSignal(SignalName.ControlModeNotified, (int)mode);
	}

	[Signal] public delegate void PositionNotifiedEventHandler(Vector3 position);
	/// <summary>
	/// 注視点の位置を通知するシグナルです。
	/// </summary>
	/// <param name="position">注視点の位置です。</param>
	public void NotifyPosition(Vector3 position)
	{
		EmitSignal(SignalName.PositionNotified, position);
	}

	[Signal] public delegate void RotationNotifiedEventHandler(Quaternion rotation);
	/// <summary>
	/// 注視点の回転を通知するシグナルです。
	/// </summary>
	/// <param name="rotation">注視点の回転です。</param>
	public void NotifyRotation(Quaternion rotation)
	{
		EmitSignal(SignalName.RotationNotified, rotation);
	}

	[Signal] public delegate void DistanceNotifiedEventHandler(float distance);
	/// <summary>
	/// カメラの距離を通知するシグナルです。
	/// </summary>
	/// <param name="distance">カメラの距離です。透視投影の場合のみ有効です。</param>
	public void NotifyDistance(float distance)
	{
		EmitSignal(SignalName.DistanceNotified, distance);
	}

	[Signal] public delegate void SizeNotifiedEventHandler(float size);
	/// <summary>
	/// カメラのズームレベルを通知するシグナルです。
	/// </summary>
	/// <param name="size">カメラのサイズ（ズームレベル）です。平行投影の場合のみ有効です。</param>
	public void NotifySize(float size)
	{
		EmitSignal(SignalName.SizeNotified, size);
	}

	[Signal] public delegate void FovNotifiedEventHandler(float fov);
	/// <summary>
	/// カメラの FOV（視野角）を通知するシグナルです。
	/// </summary> <param name="fov">FOV の値です。</param>
	public void NotifyFov(float fov)
	{
		EmitSignal(SignalName.FovNotified, fov);
	}

	[Signal] public delegate void ProjectionTypeNotifiedEventHandler(Camera3D.ProjectionType type);
	/// <summary>
	/// カメラの投影タイプを通知するシグナルです。
	/// </summary> <param name="type">投影タイプです。</param>
	public void NotifyProjectionType(Camera3D.ProjectionType type)
	{
		EmitSignal(SignalName.ProjectionTypeNotified, (int)type);
	}

	[Signal] public delegate void ArcballRadiusNotifiedEventHandler(float radius);
	/// <summary>
	/// アークボールの半径を通知するシグナルです。
	/// </summary>
	/// <param name="radius">アークボールの半径です。</param>
	public void NotifyArcballRadius(float radius)
	{
		EmitSignal(SignalName.ArcballRadiusNotified, radius);
	}

	[Signal] public delegate void ArcballHandleNotifiedEventHandler(Vector3 position);
	/// <summary>
	/// アークボール操作の操作点を通知するシグナルです。
	/// </summary>
	/// <param name="position">アークボールの操作点の位置です。</param>
	public void NotifyArcballHandle(Vector3 position)
	{
		EmitSignal(SignalName.ArcballHandleNotified, position);
	}

	#endregion
}