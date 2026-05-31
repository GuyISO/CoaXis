using Godot;
using System;
using System.Globalization;

/// <summary>
/// Godot 上で CATIA V5 風のカメラ操作を提供するコントローラです。
/// このスクリプトをアタッチした <see cref="Node3D"/> を注視点の基準ノードとして扱います。
/// </summary>
/// <remarks>
/// 操作モードと遷移:
/// None → Pan → Orbit/Roll → Zoom → None
///
/// 操作方法:
/// 中ボタン押下で Pan 開始。
/// Pan 中に左または右ボタン押下で、画面中央寄りなら Orbit、外周寄りなら Roll に切り替え。
/// 左または右ボタンを離すと Zoom に切り替え。
/// 中ボタンを離すと操作終了し、移動がなければクリック位置へ Focus します。
///
/// 座標系の定義:
/// 注視点の Position と Orientation は、このスクリプトがアタッチされた Node3D の Transform
/// (Position/Rotation) を意味します。
///
/// 拡大率の定義:
/// ズーム量は、この Node3D の子ノードである Camera3D のローカル Z 方向距離
/// (Node3D と Camera3D の Z 方向オフセット) として表現されます。
/// </remarks>
/// 



public partial class CameraController : Node
{
	/// <summary>
	/// カメラの入力制御モードです。
	/// </summary>
	public enum Mode
	{
		/// <summary>操作待機状態。</summary>
		None,
		/// <summary>注視点の平行移動操作。</summary>
		Pan,
		/// <summary>注視点中心のオービット回転操作。</summary>
		Orbit,
		/// <summary>視線方向を軸としたロール回転操作。</summary>
		Roll,
		/// <summary>ズーム操作。</summary>
		Zoom
	}

	#region Fields

	[ExportGroup("Settings")]
	[Export] private float _zoomFactor = 1.005f; // ズームの加速度を調整する係数、マウス1pixelあたりの拡大倍率
	[Export] private float _minZoomValue = 0.01f; // ズームの最小値、これ以上近づけないようにするための制限値
	[Export] private float _rollRegionRadiusRatio = 0.45f; // 画面サイズに対する、Orbit/Rollの切り替え用の円領域の半径比率
	[Export] private float _fitPadding = 1.1f; // Fit All In 時に、対象が画面にぴったり収まるようにするための余白倍率
	[Export] private float _tweenDuration = 0.5f;  // Tween を使用する場合のアニメーション時間（秒）

	private Mode _currentMode = Mode.None; // 現在の操作モード
	private Vector2 _lastMousePos = Vector2.Zero; // マウス移動量算出のために前フレームの座標を保持
	private bool _hasMoved = false; // 中ボタンを押してからマウスを移動操作したかのフラグ。クリックと移動の区別に使用
	private Tween _tween; // FocalPointの移動と回転にアニメーションを使用する場合のTweenインスタンス

	#endregion

	#region Signals
	[Signal] public delegate void ControlModeChangedEventHandler(Mode mode);
	[Signal] public delegate void FocalPointMovedEventHandler(Vector3 newPosition);
	[Signal] public delegate void FocalPointRotatedEventHandler(Quaternion newRotation);
	[Signal] public delegate void ViewPortSizeChangedEventHandler();
	#endregion

	#region Properties
	
	/// <summary>
	/// 注視点となるノードです。
	/// </summary>
	public Node3D FocalPoint { get; private set; }

	/// <summary>
	/// 操作対象のカメラノードです。
	/// </summary>
	public Camera3D Camera { get; private set; }

	/// <summary>
	/// Orbit/Roll 判定に使うトラックボール半径です。
	/// </summary>
	public float TrackballRadius { get; private set; }

	/// <summary>
	/// 画面中心座標です。
	/// </summary>
	public Vector2 ScreenCenter { get; private set; }

	#endregion

	#region Lifecycle

	/// <summary>
	/// ノード初期化時に依存ノード解決と初期パラメータ計算を行います。
	/// </summary>
	public override void _Ready()
	{
		// 初期化時に関連Nodeをキャッシュ
		FocalPoint = GetNode<Node3D>("FocalPoint");
		Camera = FocalPoint.GetNode<Camera3D>("Camera3D");

		// ビューポートサイズの変更を検知してキャッシュを更新するためのシグナル接続
		RefreshTrackballParameters();
		GetViewport().SizeChanged += OnViewportSizeChanged;
	}

	/// <summary>
	/// ノード破棄時に購読中シグナルを解除します。
	/// </summary>
	public override void _ExitTree()
	{
		GetViewport().SizeChanged -= OnViewportSizeChanged;
	}

	/// <summary>
	/// モード中のマウス移動を反映し、カメラ操作を継続します。
	/// </summary>
	/// <param name="delta">前フレームからの経過秒。</param>
	public override void _Process(double delta)
	{

		// モード中のみマウス移動に応じたカメラ操作を行う。
		if (_currentMode == Mode.None)
		{
			return;
		}

		Vector2 currentMousePos = GetViewport().GetMousePosition();
		if (currentMousePos != _lastMousePos)
		{
			ApplyCameraOperation(currentMousePos, _lastMousePos);
			_lastMousePos = currentMousePos;
		}

	}

	/// <summary>
	/// 未処理入力を監視し、カメラ操作モード遷移を行います。
	/// </summary>
	/// <param name="@event">未処理入力イベント。</param>
	public override void _UnhandledInput(InputEvent @event)
	{
		// ボタン入力だけを状態遷移に使用する。
		if (@event is InputEventMouseButton button)
		{
			HandleMouseButton(button);
		}
	}

	#endregion

	#region Internal Helpers

	private void SetCameraControlMode(Mode mode)
	{
		if (_currentMode == mode)
		{
			return;
		}

		_currentMode = mode;
		EmitSignal(SignalName.ControlModeChanged, (int)mode);
	}

	private void HandleMouseButton(InputEventMouseButton button)
	{
		// 入力状態遷移:
		// None --(中押下)--> Pan --(左右押下)--> Orbit/Roll --(左右離し)--> Zoom --(中離し)--> None
		// 現在モードに応じて遷移ルールを切り替える。
		if (_currentMode == Mode.None)
		{
			HandleIdleModeInput(button);
		}
		else
		{
			HandleActiveModeInput(button);
		}
	}

	private void HandleIdleModeInput(InputEventMouseButton button)
	{
		// 中ボタンのクリック開始を検知したら、移動フラグをリセットしてカメラコントロール開始
		if (!button.Pressed || button.ButtonIndex != MouseButton.Middle)
		{
			return;
		}

		_hasMoved = false;
		_lastMousePos = button.Position;
		SetCameraControlMode(Mode.Pan);
	}

	private void HandleActiveModeInput(InputEventMouseButton button)
	{
		// ウィンドウフォーカス喪失などのキャンセル時は即時終了。
		if (button.Canceled)
		{
			// 何らかの理由で操作がキャンセルされた場合は、確実にコントロールを終了する。
			SetCameraControlMode(Mode.None);
			return;
		}

		// 中ボタンのクリック終了を検知したら、移動していなければクリックとみなし Focus を行い、カメラコントロール終了
		if (!button.Pressed && button.ButtonIndex == MouseButton.Middle)
		{
			if (!_hasMoved)
			{
				FocusAtMouse(button.Position);
			}
			SetCameraControlMode(Mode.None);
			return;
		}

		// 左右ボタンの入力を検知したら、Pan → Orbit/Roll または Orbit/Roll → Zoom へ遷移
		if (button.ButtonIndex != MouseButton.Left && button.ButtonIndex != MouseButton.Right)
		{
			return;
		}

		// 中ボタンを押したまま右or左クリック開始を検知したら、位置によってOrbit/Rollモードに切り替え
		if (button.Pressed)
		{
			_hasMoved = true; // クリック操作したら注視点移動しないようににするため、移動フラグを立てる
			SetCameraControlMode(IsMouseOnCenterArea(button.Position) ? Mode.Orbit : Mode.Roll);
			return;
		}
		
		// 中ボタンを押したまま右or左クリック終了を検知したら、Zoomモードに切り替え
		SetCameraControlMode(Mode.Zoom);
	}

	private void ApplyCameraOperation(Vector2 currentMousePos, Vector2 previousMousePos)
	{

		// currentMousePos !!= previousMousePos は呼び出し元でチェック済みなので、ここではマウスが移動した前提で処理を行う。
		_hasMoved = true;

		switch (_currentMode)
		{
			case Mode.Pan:
				PanCamera(previousMousePos, currentMousePos);
				break;
			case Mode.Orbit:
				OrbitCamera(previousMousePos, currentMousePos);
				break;
			case Mode.Roll:
				if (IsMouseOnCenterArea(currentMousePos))
				{
					// 画面中央寄りに入ったらOrbitに変更、外周寄りはRollのままにする
					SetCameraControlMode(Mode.Orbit);
					OrbitCamera(previousMousePos, currentMousePos);
				}
				else
				{
					RollCamera(previousMousePos, currentMousePos);
				}
				break;
			case Mode.Zoom:
				ZoomCamera(previousMousePos, currentMousePos);
				break;
		}
	}

	private void OnViewportSizeChanged()
	{
		RefreshTrackballParameters();
	}

	private void RefreshTrackballParameters()
	{
		Rect2 rect = GetViewport().GetVisibleRect();
		ScreenCenter = rect.Position + rect.Size * 0.5f;
		TrackballRadius = rect.Size.Y * _rollRegionRadiusRatio;
		EmitSignal(SignalName.ViewPortSizeChanged);
	}

	#endregion

	#region Public API

	/// <summary>
	/// 投影方式を切り替え、見かけサイズが急変しないよう補正します。
	/// </summary>
	/// <param name="projectionType">切り替え先の投影方式。</param>
	public void SetCameraProjectionType(Camera3D.ProjectionType projectionType)
	{
		// 投影方式が変わるとズーム表現が変わるため、見かけの大きさをできるだけ維持して変換する。
		if (Camera.Projection == projectionType)
		{
			return;
		}

		Camera.Projection = projectionType;

		if (projectionType == Camera3D.ProjectionType.Perspective)
		{
			float distance = GetPerspectiveDistanceFromOrthographicSize();
			Camera.Position = new Vector3(0, 0, distance);
		}
		else
		{
			float size = GetOrthographicSizeFromPerspectiveDistance();
			Camera.Size = size;
			// 投影物がカメラの視界から出ないようにNearとFarの中間あたりに注視点を置く
			float farZ = (Camera.Near + Camera.Far) / 2.0f;
			Camera.Position = new Vector3(0, 0, farZ);
		}
	}

	/// <summary>
	/// 現在の投影方式を Perspective/Orthogonal でトグルします。
	/// </summary>
	public void ToggleProjectionType()
	{
		// 現在の投影方式をトグルして、内部で必要な補正を行う。
		Camera3D.ProjectionType nextProjection = Camera.Projection == Camera3D.ProjectionType.Perspective
			? Camera3D.ProjectionType.Orthogonal
			: Camera3D.ProjectionType.Perspective;

		SetCameraProjectionType(nextProjection);
	}

	/// <summary>
	/// 指定ノード配下を画角内に収めるようカメラを調整します。
	/// </summary>
	/// <param name="targetRoot">フィット対象のルートノード。</param>
	/// <param name="useTween"><see langword="true"/> の場合は補間アニメーションを使用。</param>
	/// <returns>フィット対象の AABB を取得できた場合は <see langword="true"/>。</returns>
	public bool Fit(Node targetRoot, bool useTween = false)
	{
		if (!TryGetAabb(targetRoot, out Aabb worldAabb))
		{
			return false;
		}

		Vector3 center = worldAabb.Position + worldAabb.Size * 0.5f;

		Basis inverseBasis = FocalPoint.Transform.Basis.Inverse();
		Rect2 viewportRect = GetViewport().GetVisibleRect();
		float aspect = Mathf.Max(viewportRect.Size.X / Mathf.Max(viewportRect.Size.Y, 1.0f), 0.01f);

		float maxAbsX = 0.0f;
		float maxAbsY = 0.0f;
		float maxZ = float.NegativeInfinity;
		float requiredDistance = 0.0f;

		if (Camera.Projection == Camera3D.ProjectionType.Perspective)
		{
			float halfVerticalFov = Mathf.DegToRad(Camera.Fov) * 0.5f;
			float tanHalfY = Mathf.Max(Mathf.Tan(halfVerticalFov), 1e-5f);
			float tanHalfX = Mathf.Max(tanHalfY * aspect, 1e-5f);

			foreach (Vector3 corner in GetAabbCorners(worldAabb))
			{
				Vector3 local = inverseBasis * (corner - center);
				requiredDistance = Mathf.Max(requiredDistance, local.Z + Mathf.Abs(local.X) / tanHalfX);
				requiredDistance = Mathf.Max(requiredDistance, local.Z + Mathf.Abs(local.Y) / tanHalfY);
				maxZ = Mathf.Max(maxZ, local.Z);
			}

			requiredDistance = Mathf.Max(requiredDistance, maxZ + Camera.Near * 1.5f);
			requiredDistance = Mathf.Max(requiredDistance * _fitPadding, _minZoomValue);

			if (!useTween)
			{
				MoveFocalPoint(center, null);
				Camera.Position = new Vector3(0, 0, requiredDistance);
			}
			else
			{
				MoveFocalPoint(center, null, true);
				_tween.Parallel().TweenProperty(Camera, "position", new Vector3(0, 0, requiredDistance), _tweenDuration);
			}
		}
		else
		{
			foreach (Vector3 corner in GetAabbCorners(worldAabb))
			{
				Vector3 local = inverseBasis * (corner - center);
				maxAbsX = Mathf.Max(maxAbsX, Mathf.Abs(local.X));
				maxAbsY = Mathf.Max(maxAbsY, Mathf.Abs(local.Y));
			}

			float requiredHeight = 2.0f * Mathf.Max(maxAbsY, maxAbsX / aspect);
			float targetSize = Mathf.Max(requiredHeight * _fitPadding, _minZoomValue);

			if (!useTween)
			{
				MoveFocalPoint(center, null);
				Camera.Size = targetSize;
			}
			else
			{
				MoveFocalPoint(center, null, true);
				_tween.Parallel().TweenProperty(Camera, "size", targetSize, _tweenDuration);
			}
		}

		return true;
	}

	/// <summary>
	/// 現在の注視点姿勢にロール回転を適用します。
	/// </summary>
	/// <param name="angleDegrees">ロール角度（度）。</param>
	/// <param name="useTween"><see langword="true"/> の場合は補間アニメーションを使用。</param>
	public void Roll(float angleDegrees, bool useTween = false)
	{
		Quaternion nowRotation = FocalPoint.Transform.Basis.GetRotationQuaternion();
		float angleRadians = Mathf.DegToRad(angleDegrees);
		Quaternion rollRotation = new Quaternion(Vector3.Forward, angleRadians);
		Quaternion newRotation = nowRotation * rollRotation;
		MoveFocalPoint(null, newRotation, useTween);
	}



	#endregion

	#region Internal Helpers

	private float GetPerspectiveDistanceFromOrthographicSize()
	{
		// Orthogonal の size を Perspective のカメラ距離へ変換する。
		float sizeAtZ1 = CalculateSizeAtZ1();
		return Camera.Size / sizeAtZ1;
	}


	private float GetOrthographicSizeFromPerspectiveDistance()
	{
		// 等角投影の場合はZ距離を固定して、サイズでズームを表現する方式にする
		float sizeAtZ1 = CalculateSizeAtZ1();
		return Mathf.Abs(Camera.Position.Z) * sizeAtZ1;
	}

	private float CalculateSizeAtZ1()
	{
		// FOV から、距離 Z=1 のときに見える縦サイズを求める。
		// FOVは度数法で与えられるため、ラジアンに変換
		float fovRadians = Mathf.DegToRad(Camera.Fov);

		// FOVの半分の角度のタンジェントを使用して計算
		return Mathf.Tan(fovRadians / 2.0f) * 2.0f; // Z=1のときのサイズを計算
	}

	#endregion

	#region Internal Helpers
	
	private bool IsMouseOnCenterArea(Vector2 mousePos)
	{
		// Orbit/Roll の分岐用に、画面中央の円領域判定を行う。
		return mousePos.DistanceTo(ScreenCenter) <= TrackballRadius; // 円形判定
	}

	private Vector3 ProjectOntoTrackball(Vector2 screenPos)
	{
		// スクリーン座標をトラックボール球面上の3D座標へ変換する。
		// Y軸はスクリーン下向きを反転して3D上向きに合わせる。
		float x = (screenPos.X - ScreenCenter.X) / TrackballRadius;
		float y = -(screenPos.Y - ScreenCenter.Y) / TrackballRadius;

		float lenSq = x * x + y * y;
		float z;
		if (lenSq <= 1.0f)
		{
			// 単位球の半球面上に投影
			z = Mathf.Sqrt(1.0f - lenSq);
		}
		else
		{
			// 球の外側は円周で止めず、極角を進めて球の裏側へ回り込ませる。
			float len = Mathf.Sqrt(lenSq);
			Vector2 dir = new Vector2(x, y) / len;

			// r=1 で θ=pi/2（赤道）とし、rが増えるほど極角を増やし続ける。
			// これにより裏側の極（θ=pi）を超えても球面上を連続的に移動できる。
			float theta = Mathf.Pi * 0.5f * len;
			float sinTheta = Mathf.Sin(theta);
			x = dir.X * sinTheta;
			y = dir.Y * sinTheta;
			z = Mathf.Cos(theta);
		}

		return new Vector3(x, y, z);
	}

	private Aabb GetAabb(Node node)
	{
		return TryGetAabb(node, out Aabb aabb) ? aabb : new Aabb();
	}

	private bool TryGetAabb(Node node, out Aabb aabb)
	{
		// ノード階層配下の Mesh のみを対象に AABB を合算する。
		aabb = default;
		bool hasMeshAabb = false;
		GetAabbRecursive(node, ref aabb, ref hasMeshAabb);
		return hasMeshAabb;
	}

	private void GetAabbRecursive(Node node, ref Aabb aabb, ref bool hasMeshAabb)
	{
		if (node is MeshInstance3D meshInstance && meshInstance.Mesh != null)
		{
			Aabb localAabb = meshInstance.Mesh.GetAabb();
			Aabb meshAabb = BuildWorldAabb(meshInstance, localAabb);

			if (!hasMeshAabb)
			{
				aabb = meshAabb;
				hasMeshAabb = true;
			}
			else
			{
				aabb = MergeAabb(aabb, meshAabb);
			}
		}

		foreach (var child in node.GetChildren())
		{
			if (child is Node childNode)
			{
				GetAabbRecursive(childNode, ref aabb, ref hasMeshAabb);
			}
		}
	}

	private static Aabb BuildWorldAabb(MeshInstance3D meshInstance, Aabb localAabb)
	{
		Vector3 min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
		Vector3 max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

		foreach (Vector3 corner in GetAabbCorners(localAabb))
		{
			Vector3 world = meshInstance.ToGlobal(corner);
			min = new Vector3(Mathf.Min(min.X, world.X), Mathf.Min(min.Y, world.Y), Mathf.Min(min.Z, world.Z));
			max = new Vector3(Mathf.Max(max.X, world.X), Mathf.Max(max.Y, world.Y), Mathf.Max(max.Z, world.Z));
		}

		return new Aabb(min, max - min);
	}

	private static Aabb MergeAabb(Aabb a, Aabb b)
	{
		Vector3 aMax = a.Position + a.Size;
		Vector3 bMax = b.Position + b.Size;
		Vector3 min = new Vector3(
			Mathf.Min(a.Position.X, b.Position.X),
			Mathf.Min(a.Position.Y, b.Position.Y),
			Mathf.Min(a.Position.Z, b.Position.Z));
		Vector3 max = new Vector3(
			Mathf.Max(aMax.X, bMax.X),
			Mathf.Max(aMax.Y, bMax.Y),
			Mathf.Max(aMax.Z, bMax.Z));

		return new Aabb(min, max - min);
	}

	private static Vector3[] GetAabbCorners(Aabb aabb)
	{
		Vector3 p = aabb.Position;
		Vector3 s = aabb.Size;
		return new[]
		{
			new Vector3(p.X, p.Y, p.Z),
			new Vector3(p.X + s.X, p.Y, p.Z),
			new Vector3(p.X, p.Y + s.Y, p.Z),
			new Vector3(p.X, p.Y, p.Z + s.Z),
			new Vector3(p.X + s.X, p.Y + s.Y, p.Z),
			new Vector3(p.X + s.X, p.Y, p.Z + s.Z),
			new Vector3(p.X, p.Y + s.Y, p.Z + s.Z),
			new Vector3(p.X + s.X, p.Y + s.Y, p.Z + s.Z)
		};
	}

	#endregion

	#region Internal Helpers

	private void FocusAtMouse(Vector2 screenPos)
	{
		// クリック位置へフォーカスできれば注視点を直接移動。
		if (TryGetFocusHitPosition(screenPos, out Vector3 hitPosition))
		{
			MoveFocalPoint(hitPosition, null);
			return;
		}

		// ヒットしなかった場合は、クリックした位置から画面中心への分のPan操作と同等とする
		PanCamera(screenPos, ScreenCenter);
	}

	private bool TryGetFocusHitPosition(Vector2 screenPos, out Vector3 hitPosition)
	{
		// レイキャストで最初にヒットした位置を注視候補として取得する。
		// クリック位置へレイを飛ばし、ヒット位置に注視点を移動する
		Vector3 rayOrigin = Camera.ProjectRayOrigin(screenPos);
		Vector3 rayDir = Camera.ProjectRayNormal(screenPos).Normalized();
		Vector3 rayEnd = rayOrigin + rayDir * Camera.Far;

		PhysicsDirectSpaceState3D spaceState = Camera.GetWorld3D().DirectSpaceState;
		PhysicsRayQueryParameters3D query = PhysicsRayQueryParameters3D.Create(rayOrigin, rayEnd);
		Godot.Collections.Dictionary result = spaceState.IntersectRay(query);

		if (result.Count > 0 && result.ContainsKey("position"))
		{
			hitPosition = (Vector3)result["position"];
			return true;
		}

		hitPosition = Vector3.Zero;
		return false;
	}

	private void PanCamera(Vector2 fromScreenPos, Vector2 toScreenPos)
	{
		// 同一深度平面で、スクリーン座標 from -> to に対応するワールド移動量を求める。
		// これにより、画面サイズ・Orthogonal の Size・Perspective の FOV/Z 距離を自動で吸収する。
		float panDepth = Mathf.Max(Mathf.Abs(Camera.Position.Z), _minZoomValue);
		Vector3 fromWorld = Camera.ProjectPosition(fromScreenPos, panDepth);
		Vector3 toWorld = Camera.ProjectPosition(toScreenPos, panDepth);

		// ドラッグ方向に見た目が追従するよう、差分を逆向きで適用する。
		Vector3 move = fromWorld - toWorld;
		MoveFocalPoint(FocalPoint.Position + move, null);
	}

	private void OrbitCamera(Vector2 previousMousePos, Vector2 currentMousePos)
	{
		// 仮想トラックボール（アークボール）方式でFocalPointを回転させる。
		// Orbit/Roll判定と同じ円を球面半径として使い、
		// 2点の球面座標から回転軸・角度を求めてFocalPointのローカル軸で回転する。
		Vector3 p0 = ProjectOntoTrackball(previousMousePos);
		Vector3 p1 = ProjectOntoTrackball(currentMousePos);

		Vector3 axis = p0.Cross(p1);
		float dot = Mathf.Clamp(p0.Dot(p1), -1.0f, 1.0f);
		float angle = Mathf.Acos(dot);

		Quaternion nowRotation = FocalPoint.Transform.Basis.GetRotationQuaternion();
		Quaternion orbitRotation = new Quaternion(axis.Normalized(), -angle);
		Quaternion newRotation = nowRotation * orbitRotation;
		MoveFocalPoint(null, newRotation);
	}

	private void RollCamera(Vector2 previousMousePos, Vector2 currentMousePos)
	{
		// 画面中心から見た角度差を使ってロール量を計算する。
		// 前フレームと今フレームのマウス位置ベクトル（中心基準）
		Vector2 v0 = previousMousePos - ScreenCenter;
		Vector2 v1 = currentMousePos - ScreenCenter;

		// それぞれの角度
		float angle0 = Mathf.Atan2(v0.Y, v0.X);
		float angle1 = Mathf.Atan2(v1.Y, v1.X);

		// 差分角度
		float deltaAngle = angle0 - angle1;

		// Z軸回転
		Quaternion nowRotation = FocalPoint.Transform.Basis.GetRotationQuaternion();
		Quaternion rollRotation = new Quaternion(Vector3.Forward, deltaAngle);
		Quaternion newRotation = nowRotation * rollRotation;
		MoveFocalPoint(null, newRotation);
	}

	private void ZoomCamera(Vector2 previousMousePos, Vector2 currentMousePos)
	{
		float deltaY = currentMousePos.Y - previousMousePos.Y;
		float scale = Mathf.Pow(_zoomFactor, deltaY);
		
		// 投影方式ごとにズーム表現が異なるため、変更先を分ける。
		if (Camera.Projection == Camera3D.ProjectionType.Orthogonal)
		{
			// 等角投影の場合はサイズを変更してズームを表現する
			float newSize = Camera.Size * scale;
			// サイズが小さくなりすぎて見えなくなるのを防止するため、最小値を設定する
			Camera.Size = Mathf.Max(newSize, _minZoomValue);
		}
		else
		{
			// 透視投影の場合はカメラのZ距離を変更してズームを表現する
			float distance = Camera.Position.Z;
			float newDistance = Mathf.Max(distance * scale, _minZoomValue);
			// 距離が近すぎて見えなくなるのを防止するため、最小値を設定する
			Camera.Position = new Vector3(0, 0, newDistance);
		}
	}

	/// <summary>
	/// 注視点の位置と回転を更新します。
	/// </summary>
	/// <param name="targetPosition">移動先位置。<see langword="null"/> の場合は現在位置を維持。</param>
	/// <param name="targetRotation">回転先姿勢。<see langword="null"/> の場合は現在姿勢を維持。</param>
	/// <param name="useTween"><see langword="true"/> の場合は補間アニメーションを使用。</param>
	public void MoveFocalPoint(Vector3? targetPosition, Quaternion? targetRotation, bool useTween = false)
	{
		// 実行中の補間動作があればキャンセルする
		_tween?.Kill();

		Vector3 endPos = targetPosition ?? FocalPoint.Position;
		Quaternion endQ = targetRotation ?? FocalPoint.Transform.Basis.GetRotationQuaternion();

		// Tween を使用しない場合は即時に移動・回転を適用する。
		if (!useTween)
		{
			FocalPoint.Transform = new Transform3D(new Basis(endQ), endPos);
			return;
		}

		Vector3 startPos = FocalPoint.Position;
		Quaternion startQ = FocalPoint.Transform.Basis.GetRotationQuaternion();

		_tween = CreateTween()
			.SetTrans(Tween.TransitionType.Cubic)
			.SetEase(Tween.EaseType.Out);

		_tween.TweenMethod(Callable.From((float t) =>
		{
			FocalPoint.Transform = new Transform3D(
				new Basis(startQ.Slerp(endQ, t)),
				startPos.Lerp(endPos, t)
			);
		}), 0f, 1f, _tweenDuration);
	}

	#endregion

	#region Internal Helpers

	/// <summary>
	/// カメラ状態を JSON 文字列として保存します。
	/// </summary>
	/// <returns>カメラ状態を表す JSON 文字列。</returns>
	public string ToJson()
	{
		var state = new Godot.Collections.Dictionary();
		state["position"] = new Godot.Collections.Array { FocalPoint.Position.X, FocalPoint.Position.Y, FocalPoint.Position.Z };
		state["rotation"] = new Godot.Collections.Array { FocalPoint.Rotation.X, FocalPoint.Rotation.Y, FocalPoint.Rotation.Z };
		state["projection"] = (int)Camera.Projection;
		state["fov"] = Camera.Fov;
		state["distance"] = Camera.Position.Z;
		state["size"] = Camera.Size;
		return Json.Stringify(state);
	}

	/// <summary>
	/// JSON 文字列からカメラ状態を復元します。
	/// </summary>
	/// <param name="json">復元対象の JSON 文字列。</param>
	public void LoadJson(string json)
	{
		if (string.IsNullOrWhiteSpace(json))
		{
			return;
		}

		Godot.Collections.Dictionary state;
		try
		{
			state = (Godot.Collections.Dictionary)Json.ParseString(json);
		}
		catch (Exception)
		{
			GD.PushWarning("CameraController.LoadJson: invalid JSON payload.");
			return;
		}

		if (state == null)
		{
			return;
		}

		// 注視点Transform
		if (TryGetVector3(state, "position", out Vector3 position))
		{
			FocalPoint.SetPosition(position);
		}

		if (TryGetVector3(state, "rotation", out Vector3 rotation))
		{
			FocalPoint.SetRotation(rotation);
		}

		// FOV
		if (TryGetFloat(state, "fov", out float fov))
		{
			Camera.Fov = fov;
		}

		// カメラZ位置
		if (TryGetFloat(state, "distance", out float distance))
		{
			Camera.Position = new Vector3(0, 0, distance);
		}

		// 等角投影サイズ
		if (TryGetFloat(state, "size", out float size))
		{
			Camera.Size = size;
		}

		// カメラ投影
		if (TryGetInt(state, "projection", out int projection)
			&& Enum.IsDefined(typeof(Camera3D.ProjectionType), projection))
		{
			Camera.Projection = (Camera3D.ProjectionType)projection;
		}

	}

	private static bool TryGetVector3(Godot.Collections.Dictionary dictionary, string key, out Vector3 value)
	{
		value = Vector3.Zero;
		if (!dictionary.ContainsKey(key))
		{
			return false;
		}

		Godot.Collections.Array array;
		try
		{
			array = (Godot.Collections.Array)dictionary[key];
		}
		catch (Exception)
		{
			return false;
		}

		if (array.Count != 3)
		{
			return false;
		}

		if (!TryConvertToFloat(array[0], out float x)
			|| !TryConvertToFloat(array[1], out float y)
			|| !TryConvertToFloat(array[2], out float z))
		{
			return false;
		}

		value = new Vector3(x, y, z);
		return true;
	}

	private static bool TryGetFloat(Godot.Collections.Dictionary dictionary, string key, out float value)
	{
		value = 0.0f;
		if (!dictionary.ContainsKey(key))
		{
			return false;
		}

		return TryConvertToFloat(dictionary[key], out value);
	}

	private static bool TryGetInt(Godot.Collections.Dictionary dictionary, string key, out int value)
	{
		value = 0;
		if (!dictionary.ContainsKey(key))
		{
			return false;
		}

		return TryConvertToInt(dictionary[key], out value);
	}

	private static bool TryConvertToFloat(object value, out float converted)
	{
		switch (value)
		{
			case Variant variantValue:
				return TryConvertVariantToFloat(variantValue, out converted);
			case float floatValue:
				converted = floatValue;
				return true;
			case double doubleValue:
				converted = (float)doubleValue;
				return true;
			case int intValue:
				converted = intValue;
				return true;
			case long longValue:
				converted = longValue;
				return true;
			case string stringValue:
				return float.TryParse(stringValue, NumberStyles.Float, CultureInfo.InvariantCulture, out converted);
		}

		try
		{
			converted = Convert.ToSingle(value, CultureInfo.InvariantCulture);
			return true;
		}
		catch (Exception)
		{
			converted = 0.0f;
			return false;
		}
	}

	private static bool TryConvertToInt(object value, out int converted)
	{
		switch (value)
		{
			case Variant variantValue:
				return TryConvertVariantToInt(variantValue, out converted);
			case int intValue:
				converted = intValue;
				return true;
			case long longValue when longValue <= int.MaxValue && longValue >= int.MinValue:
				converted = (int)longValue;
				return true;
			case float floatValue:
				converted = (int)floatValue;
				return true;
			case double doubleValue:
				converted = (int)doubleValue;
				return true;
			case string stringValue:
				return int.TryParse(stringValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out converted);
		}

		try
		{
			converted = Convert.ToInt32(value, CultureInfo.InvariantCulture);
			return true;
		}
		catch (Exception)
		{
			converted = 0;
			return false;
		}
	}

	private static bool TryConvertVariantToFloat(Variant value, out float converted)
	{
		try
		{
			converted = (float)value;
			return true;
		}
		catch (Exception)
		{
			return float.TryParse(value.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out converted);
		}
	}

	private static bool TryConvertVariantToInt(Variant value, out int converted)
	{
		try
		{
			converted = (int)value;
			return true;
		}
		catch (Exception)
		{
			return int.TryParse(value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out converted);
		}
	}

	#endregion
}