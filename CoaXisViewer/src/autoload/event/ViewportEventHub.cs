using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// ビューポート関連のイベント集約ハブです。AutoLoadノードとしてシーンツリーに配置し、ビューポートの状態変更や操作のリクエストを通知するためのシグナルを提供します。これにより、ビューポート操作のロジックを分散させずに一元管理できます。
/// Autoloadに登録してシングルトン参照することを前提としています。
/// </summary>
public partial class ViewportEventHub : Node
{
	/// <summary>
	/// シングルトン参照です。
	/// </summary>
	public static ViewportEventHub I { get; private set; }

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
	/// ビューポート関連の状態の通知をリクエストします。
	/// </summary>
	public static void RequestNotifyState()
	{
		I.EmitSignal(SignalName.NotifyStateRequested);
	}

	[Signal] public delegate void MovePositionToRequestedEventHandler(Vector3 position, bool useTween);
	/// <summary>
	/// 注視点の位置の設定をリクエストします。
	/// </summary>
	/// <param name="position">設定する注視点の位置です。</param>
	/// <param name="useTween">移動にトゥイーンを使用するかどうかのフラグです。デフォルトは false です。</param>
	/// <remarks>SetPositionという名前にしたいところですが、Node3Dの標準メソッドと区別するためにRequestMovePositionToという名前にしています。</remarks>
	public static void RequestMovePositionTo(Vector3 position, bool useTween = false)
	{
		I.EmitSignal(SignalName.MovePositionToRequested, position, useTween);
	}

	[Signal] public delegate void MoveRotationToRequestedEventHandler(Quaternion rotation, bool useTween);
	/// <summary>
	/// 注視点の回転の設定をリクエストします。
	/// </summary>
	/// <param name="rotation">設定する注視点の回転です。</param>
	/// <param name="useTween">移動にトゥイーンを使用するかどうかのフラグです。デフォルトは false です。</param>
	/// <remarks>SetRotationという名前にしたいところですが、Node3Dの標準メソッドと区別するためにRequestMoveRotationToという名前にしています。</remarks>
	public static void RequestMoveRotationTo(Quaternion rotation, bool useTween = false)
	{
		I.EmitSignal(SignalName.MoveRotationToRequested, rotation, useTween);
	}

	[Signal] public delegate void SetDistanceRequestedEventHandler(float distance, bool useTween);
	/// <summary>
	/// カメラの距離の設定をリクエストします。
	/// </summary> <param name="distance">設定するカメラの距離です。透視投影の場合のみ有効です。</param>
	/// <param name="useTween">移動にトゥイーンを使用するかどうかのフラグです。デフォルトは false です。</param>
	public static void RequestSetDistance(float distance, bool useTween = false)
	{
		I.EmitSignal(SignalName.SetDistanceRequested, distance, useTween);
	}

	[Signal] public delegate void SetSizeRequestedEventHandler(float size, bool useTween);
	/// <summary>
	/// カメラのサイズの設定をリクエストします。
	/// </summary>
	/// <param name="size">設定するカメラのサイズです。平行投影の場合のみ有効です。</param>
	/// <param name="useTween">サイズ変更にトゥイーンを使用するかどうかのフラグです。デフォルトは false です。</param>
	public static void RequestSetSizeTo(float size, bool useTween = false)
	{
		I.EmitSignal(SignalName.SetSizeRequested, size, useTween);
	}

	[Signal] public delegate void SetFovRequestedEventHandler(float fov, bool useTween);
	/// <summary>
	/// FOV（視野角）の設定をリクエストします。
	/// </summary>
	/// <param name="fov">設定する FOV の値です。</param>
	/// <param name="useTween">FOV変更にトゥイーンを使用するかどうかのフラグです。デフォルトは false です。</param>
	public static void RequestSetFov(float fov, bool useTween = false)
	{
		I.EmitSignal(SignalName.SetFovRequested, fov, useTween);
	}

	[Signal] public delegate void TranslateRequestedEventHandler(Vector3 translation, SpaceMode spaceMode, bool useTween);
	/// <summary>
	/// カメラの平行移動をリクエストします。
	/// </summary> <param name="translation">カメラの平行移動量です。</param>
	/// <param name="spaceMode">平行移動の基準となる座標系です。</param>
	/// <param name="useTween">移動にトゥイーンを使用するかどうかのフラグです。デフォルトは false です。</param>
	public static void RequestTranslate(Vector3 translation, SpaceMode spaceMode = SpaceMode.World, bool useTween = false)
	{
		I.EmitSignal(SignalName.TranslateRequested, translation, (int)spaceMode, useTween);
	}

	[Signal] public delegate void RotateRequestedEventHandler(Quaternion rotation, SpaceMode spaceMode, bool useTween);
	/// <summary>
	/// カメラの回転をリクエストします。
	/// </summary>
	/// <param name="rotation">設定するカメラの回転です。</param>
	/// <param name="spaceMode">回転の基準となる座標系です。</param>
	/// <param name="useTween">回転にトゥイーンを使用するかどうかのフラグです。デフォルトは false です。</param>
	public static void RequestRotate(Quaternion rotation, SpaceMode spaceMode = SpaceMode.World, bool useTween = false)
	{
		I.EmitSignal(SignalName.RotateRequested, rotation, (int)spaceMode, useTween);
	}

	[Signal] public delegate void ZoomRequestedEventHandler(float exponent, bool useTween);
	/// <summary>
	/// カメラのズームをリクエストします。
	/// </summary>
	/// <param name="exponent">ズームの指数値です。1 より大きい値はズームイン、1 より小さい値はズームアウトを意味します。</param>
	/// <param name="useTween">ズームにトゥイーンを使用するかどうかのフラグです。デフォルトは false です。</param>
	public static void RequestZoom(float exponent, bool useTween = false)
	{
		I.EmitSignal(SignalName.ZoomRequested, exponent, useTween);
	}

	[Signal] public delegate void SetProjectionTypeRequestedEventHandler(Camera3D.ProjectionType type);
	/// <summary>
	/// 投影タイプの設定をリクエストします。
	/// </summary>
	/// <param name="type">設定する投影タイプです。</param>
	public static void RequestSetProjectionType(Camera3D.ProjectionType type)
	{
		I.EmitSignal(SignalName.SetProjectionTypeRequested, (int)type);
	}

	[Signal] public delegate void ToggleProjectionTypeRequestedEventHandler();
	/// <summary>
	/// 投影タイプの切り替えをリクエストします。
	/// </summary>
	public static void RequestToggleProjectionType()
	{
		I.EmitSignal(SignalName.ToggleProjectionTypeRequested);
	}

	[Signal] public delegate void FitRequestedEventHandler(Node3D[] targetNodes, bool useTween);
	/// <summary>
	/// カメラを指定ノード群全体にフィットさせる操作をリクエストします。
	/// </summary>
	/// <param name="targetNodes">フィットさせたいターゲットノード群です。</param>
	/// <param name="useTween">フィット操作にトゥイーンを使用するかどうかのフラグです。デフォルトは false です。</param>
	public static void RequestFit(IEnumerable<Node3D> targetNodes, bool useTween = false)
	{
		Node3D[] targets = targetNodes as Node3D[] ?? (targetNodes == null ? Array.Empty<Node3D>() : new List<Node3D>(targetNodes).ToArray());
		I.EmitSignal(SignalName.FitRequested, targets, useTween);
	}

	[Signal] public delegate void AlignNormalToRequestedEventHandler(Vector3 normal, bool useTween);
	/// <summary>
	/// カメラの向きを指定した法線に合わせる操作をリクエストします。
	/// </summary>
	/// <param name="normal">カメラの向きを合わせたい法線ベクトルです。</param>
	/// <param name="useTween">回転にトゥイーンを使用するかどうかのフラグです。デフォルトは false です。</param>
	public static void RequestAlignNormalTo(Vector3 normal, bool useTween = false)
	{
		I.EmitSignal(SignalName.AlignNormalToRequested, normal, useTween);
	}
	
	[Signal] public delegate void DecideSelectionRectRequestedEventHandler(Vector2 startPosition, Vector2 endPosition);
	/// <summary>
	/// 矩形選択の確定を通知するシグナルです。
	/// </summary> <param name="startPosition">矩形選択の開始位置です。</param>
	/// <param name="endPosition">矩形選択の終了位置です。</param>
	public static void RequestDecideSelectionRect(Vector2 startPosition, Vector2 endPosition)
	{
		I.EmitSignal(SignalName.DecideSelectionRectRequested, startPosition, endPosition);
	}

	#endregion

	#region --------------------------------------- Notification ---------------------------------------

	[Signal] public delegate void InputModeNotifiedEventHandler(ViewportInputMode mode);
	/// <summary>
	/// 操作モードを通知するシグナルです。
	/// </summary>
	/// <param name="mode">操作モードです。</param>
	public static void NotifyInputMode(ViewportInputMode mode)
	{
		I.EmitSignal(SignalName.InputModeNotified, (int)mode);
	}

	[Signal] public delegate void PositionNotifiedEventHandler(Vector3 position);
	/// <summary>
	/// 注視点の位置を通知するシグナルです。
	/// </summary>
	/// <param name="position">注視点の位置です。</param>
	public static void NotifyPosition(Vector3 position)
	{
		I.EmitSignal(SignalName.PositionNotified, position);
	}

	[Signal] public delegate void RotationNotifiedEventHandler(Quaternion rotation);
	/// <summary>
	/// 注視点の回転を通知するシグナルです。
	/// </summary>
	/// <param name="rotation">注視点の回転です。</param>
	public static void NotifyRotation(Quaternion rotation)
	{
		I.EmitSignal(SignalName.RotationNotified, rotation);
	}

	[Signal] public delegate void DistanceNotifiedEventHandler(float distance);
	/// <summary>
	/// カメラの距離を通知するシグナルです。
	/// </summary>
	/// <param name="distance">カメラの距離です。透視投影の場合のみ有効です。</param>
	public static void NotifyDistance(float distance)
	{
		I.EmitSignal(SignalName.DistanceNotified, distance);
	}

	[Signal] public delegate void SizeNotifiedEventHandler(float size);
	/// <summary>
	/// カメラのズームレベルを通知するシグナルです。
	/// </summary>
	/// <param name="size">カメラのサイズ（ズームレベル）です。平行投影の場合のみ有効です。</param>
	public static void NotifySize(float size)
	{
		I.EmitSignal(SignalName.SizeNotified, size);
	}

	[Signal] public delegate void FovNotifiedEventHandler(float fov);
	/// <summary>
	/// カメラの FOV（視野角）を通知するシグナルです。
	/// </summary> <param name="fov">FOV の値です。</param>
	public static void NotifyFov(float fov)
	{
		I.EmitSignal(SignalName.FovNotified, fov);
	}

	[Signal] public delegate void ProjectionTypeNotifiedEventHandler(Camera3D.ProjectionType type);
	/// <summary>
	/// カメラの投影タイプを通知するシグナルです。
	/// </summary> <param name="type">投影タイプです。</param>
	public static void NotifyProjectionType(Camera3D.ProjectionType type)
	{
		I.EmitSignal(SignalName.ProjectionTypeNotified, (int)type);
	}

	[Signal] public delegate void ArcballRadiusNotifiedEventHandler(float radius);
	/// <summary>
	/// アークボールの半径を通知するシグナルです。
	/// </summary>
	/// <param name="radius">アークボールの半径です。</param>
	public static void NotifyArcballRadius(float radius)
	{
		I.EmitSignal(SignalName.ArcballRadiusNotified, radius);
	}

	[Signal] public delegate void ArcballHandleNotifiedEventHandler(Vector3 position);
	/// <summary>
	/// アークボール操作の操作点を通知するシグナルです。
	/// </summary>
	/// <param name="position">アークボールの操作点の位置です。</param>
	public static void NotifyArcballHandle(Vector3 position)
	{
		I.EmitSignal(SignalName.ArcballHandleNotified, position);
	}

	[Signal] public delegate void SelectionRectNotifiedEventHandler(Vector2 startPosition, Vector2 endPosition);
	/// <summary>
	/// 矩形選択の範囲を通知するシグナルです。
	/// </summary> <param name="startPosition">矩形選択の開始位置です。</param>
	/// <param name="endPosition">矩形選択の終了位置です。</param>
	public static void NotifySelectionRect(Vector2 startPosition, Vector2 endPosition)
	{
		I.EmitSignal(SignalName.SelectionRectNotified, startPosition, endPosition);
	}

	#endregion
}