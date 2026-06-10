using Godot;
using System;

/// <summary>
/// カメラの位置と向きを制御し、イベントハブを通じて他コンポーネントと連携します。
/// このクラスをアタッチした <see cref="Node3D"/> を注視点の基準ノードとして扱います。
/// </summary>

public partial class CameraRig : Node3D
{
	#region Fields
	
	[ExportGroup("Settings")]
	[Export] private float _zoomBase = 1.005f; // ズーム倍率変更時の底、exponent1あたりの拡大倍率
	[Export] private float _minZoomValue = 0.01f; // ズームの最小値、これ以上近づけないようにするための制限値
	[Export] private float _fitPadding = 1.1f; // Fit All In 時に、対象が画面にぴったり収まるようにするための余白倍率
	[Export] private float _tweenDuration = 0.5f; // Tween を使用する場合のアニメーション時間（秒）

	private Camera3D _camera; // 操作対象のカメラノード

	#endregion

    #region Lifecycle

	public override void _Ready()
	{
		// 関連ノードのキャッシュ
		_camera = GetNode<Camera3D>("Camera3D");

		// イベント購読の登録
		CameraEventHub.I.NotifyStateRequested += OnNotifyStateRequested;
		CameraEventHub.I.MovePositionToRequested += OnMovePositionToRequested;
		CameraEventHub.I.MoveRotationToRequested += OnMoveRotationToRequested;
		CameraEventHub.I.ZoomRequested += OnZoomRequested;
		CameraEventHub.I.RotateRequested += OnRotateRequested;
		CameraEventHub.I.SetDistanceRequested += OnSetDistanceRequested;
		CameraEventHub.I.SetSizeRequested += OnSetSizeRequested;
		CameraEventHub.I.ToggleProjectionTypeRequested += OnToggleProjectionTypeRequested;
		CameraEventHub.I.SetProjectionTypeRequested += OnSetProjectionTypeRequested;
		CameraEventHub.I.SetFovRequested += OnSetFovRequested;
		CameraEventHub.I.FitRequested += OnFitRequested;
		CameraEventHub.I.AlignNormalToRequested += OnAlignNormalToRequested;
	}

    public override void _ExitTree()
	{
		// イベント購読の解除
		CameraEventHub.I.NotifyStateRequested -= OnNotifyStateRequested;
		CameraEventHub.I.MovePositionToRequested -= OnMovePositionToRequested;
		CameraEventHub.I.MoveRotationToRequested -= OnMoveRotationToRequested;
		CameraEventHub.I.ZoomRequested -= OnZoomRequested;
		CameraEventHub.I.RotateRequested -= OnRotateRequested;
		CameraEventHub.I.SetDistanceRequested -= OnSetDistanceRequested;
		CameraEventHub.I.SetSizeRequested -= OnSetSizeRequested;
		CameraEventHub.I.ToggleProjectionTypeRequested -= OnToggleProjectionTypeRequested;
		CameraEventHub.I.SetProjectionTypeRequested -= OnSetProjectionTypeRequested;
		CameraEventHub.I.SetFovRequested -= OnSetFovRequested;
		CameraEventHub.I.FitRequested -= OnFitRequested;
		CameraEventHub.I.AlignNormalToRequested -= OnAlignNormalToRequested;
	}

	#endregion

	#region Events

	/// <summary>
	/// カメラの状態の通知がリクエストされたときに呼び出されるイベントハンドラです。現在のカメラ状態をイベントハブを通じて通知します。
	/// </summary>
	private void OnNotifyStateRequested()
	{
		CameraEventHub.I.NotifyPosition(Position);
		CameraEventHub.I.NotifyRotation(Transform.Basis.GetRotationQuaternion());
		CameraEventHub.I.NotifySize(_camera.Size);
		CameraEventHub.I.NotifyDistance(_camera.Position.Z);
		CameraEventHub.I.NotifyFov(_camera.Fov);
		CameraEventHub.I.NotifyProjectionType(_camera.Projection);
	}

	/// <summary>
	/// カメラの位置移動がリクエストされたときに呼び出されるイベントハンドラです。
	/// </summary>
	/// <param name="position">移動先の位置です。</param>
	/// <param name="useTween"><see langword="true"/> の場合は補間アニメーションを使用します。</param>
	private void OnMovePositionToRequested(Vector3 position, bool useTween)
	{
		MovePositionTo(position, useTween);
	}

	/// <summary>
	/// カメラの回転移動がリクエストされたときに呼び出されるイベントハンドラです。
	/// </summary>
	/// <param name="rotation">回転先の姿勢です。</param>
	/// <param name="useTween"><see langword="true"/> の場合は補間アニメーションを使用します。</param>
	private void OnMoveRotationToRequested(Quaternion rotation, bool useTween)
	{
		MoveRotationTo(rotation, useTween);
	}

	/// <summary>
	/// カメラのズームがリクエストされたときに呼び出されるイベントハンドラです。
	/// </summary>
	/// <param name="exponent">ズームの指数値です。</param>
	/// <param name="useTween"><see langword="true"/> の場合は補間アニメーションを使用します。</param>
	private void OnZoomRequested(float exponent, bool useTween)
	{
		Zoom(exponent, useTween);
	}

	/// <summary>
	/// カメラの回転がリクエストされたときに呼び出されるイベントハンドラです。
	/// </summary>
	/// <param name="rotation">回転先の姿勢です。</param>
	/// <param name="useTween"><see langword="true"/> の場合は補間アニメーションを使用します。</param>
	private void OnRotateRequested(Quaternion rotation, bool useTween)
	{
		Rotate(rotation, useTween);
	}

	/// <summary>
	/// カメラの距離の設定がリクエストされたときに呼び出されるイベントハンドラです。
	/// </summary>
	/// <param name="distance">設定する距離です。</param>
	/// <param name="useTween"><see langword="true"/> の場合は補間アニメーションを使用します。</param>
	private void OnSetDistanceRequested(float distance, bool useTween)
	{
		MoveDistanceTo(distance, useTween);
	}

	/// <summary>
	/// カメラのサイズの設定がリクエストされたときに呼び出されるイベントハンドラです。
	/// </summary>
	/// <param name="size">設定するサイズです。</param>
	/// <param name="useTween"><see langword="true"/> の場合は補間アニメーションを使用します。</param>
	private void OnSetSizeRequested(float size, bool useTween)
	{
		ChangeSizeTo(size, useTween);
	}

	/// <summary>
	/// カメラの投影タイプの切り替えがリクエストされたときに呼び出されるイベントハンドラです。
	/// </summary>
	private void OnToggleProjectionTypeRequested()
	{
		ToggleProjectionType();
	}

	/// <summary>
	/// カメラの投影タイプの設定がリクエストされたときに呼び出されるイベントハンドラです。
	/// </summary>
	/// <param name="type">設定する投影タイプです。</param>
	private void OnSetProjectionTypeRequested(Camera3D.ProjectionType type)
	{
		SetProjectionType(type);
	}

	/// <summary>
	/// カメラの視野角（FOV）の設定がリクエストされたときに呼び出されるイベントハンドラです。
	/// </summary>
	/// <param name="fov">設定する視野角です。</param>
	/// <param name="useTween"><see langword="true"/> の場合は補間アニメーションを使用します。</param>
	private void OnSetFovRequested(float fov, bool useTween)
	{
		SetFov(fov, useTween);
	}

	/// <summary>
	/// カメラのフィット操作がリクエストされたときに呼び出されるイベントハンドラです。
	/// </summary>
	/// <param name="targetNode">フィット対象のノードです。</param>
	/// <param name="useTween"><see langword="true"/> の場合は補間アニメーションを使用します。</param>
	private void OnFitRequested(Node targetNode, bool useTween)
	{
		Fit(targetNode, useTween);
	}

	/// <summary>
	/// カメラの法線方向の整列がリクエストされたときに呼び出されるイベントハンドラです。
	/// </summary>
	/// <param name="normal">整列先の法線方向を表すベクトルです。</param>
	/// <param name="useTween"><see langword="true"/> の場合は補間アニメーションを使用します。</param>
	private void OnAlignNormalToRequested(Vector3 normal, bool useTween)
	{
		AlignNormalTo(normal, useTween);
	}

	#endregion

	#region Internal Helpers

	/// <summary>
	/// 注視点の位置を更新します。
	/// </summary>
	/// <param name="posision">移動先位置。<see langword="null"/> の場合は現在位置を維持。</param>
	/// <param name="useTween"><see langword="true"/> の場合は補間アニメーションを使用。</param>
	private void MovePositionTo(Vector3 posision, bool useTween = false)
	{
		if (useTween)
		{
			TweenPosition(posision);
		}
		else
		{
			Transform = new Transform3D(Transform.Basis, posision);
			CameraEventHub.I.NotifyPosition(Position);
		}
	}

	/// <summary>
	/// 注視点の回転を更新します。
	/// </summary>
	/// <param name="rotation">回転先姿勢。</param>
	/// <param name="useTween"><see langword="true"/> の場合は補間アニメーションを使用。</param>
	private void MoveRotationTo(Quaternion rotation, bool useTween = false)
	{
		if (useTween)
		{
			TweenRotation(rotation);
		}
		else
		{
			Transform = new Transform3D(new Basis(rotation), Transform.Origin);
			CameraEventHub.I.NotifyRotation(Transform.Basis.GetRotationQuaternion());
		}
	}

	/// <summary>
	/// 現在の回転に対して、さらに回転を加算します。
	/// </summary>
	/// <param name="rotation">加算する回転。</param>
	/// <param name="useTween"><see langword="true"/> の場合は補間アニメーションを使用。</param>
	private void Rotate(Quaternion rotation, bool useTween = false)
	{
		Quaternion nowRotation = Transform.Basis.GetRotationQuaternion();
		Quaternion newRotation = nowRotation * rotation;
		MoveRotationTo(newRotation, useTween);
	}

	/// <summary>
	/// ズーム操作を実行します。等角投影の場合はサイズを、透視投影の場合はカメラのZ距離を変更してズームを表現します。
	/// </summary>
	/// <param name="exponent">ズームの指数値です。</param>
	/// <param name="useTween"><see langword="true"/> の場合は補間アニメーションを使用します。</param>
	private void Zoom(float exponent, bool useTween = false)
	{
		float scale = Mathf.Pow(_zoomBase, exponent);
		// 投影方式ごとにズーム表現が異なるため、変更先を分ける。
		if (_camera.Projection == Camera3D.ProjectionType.Orthogonal)
		{
			// 等角投影の場合はサイズを変更してズームを表現する
			float newSize = _camera.Size * scale;
			// サイズが小さくなりすぎて見えなくなるのを防止するため、最小値を設定する
			float fixedSize = Mathf.Max(newSize, _minZoomValue);
			ChangeSizeTo(fixedSize, useTween);
		}
		else
		{
			// 透視投影の場合はカメラのZ距離を変更してズームを表現する
			float distance = _camera.Position.Z;
			float newDistance = Mathf.Max(distance * scale, _minZoomValue);
			// 距離が近すぎて見えなくなるのを防止するため、最小値を設定する
			float fixedDistance = Mathf.Max(newDistance, _minZoomValue);
			MoveDistanceTo(fixedDistance, useTween);
		}
	}

	/// <summary>
	/// カメラの距離を設定します。
	/// </summary>
	/// <param name="distance">設定する距離です。</param>
	/// <param name="useTween"><see langword="true"/> の場合は補間アニメーションを使用します。</param>
	private void MoveDistanceTo(float distance, bool useTween = false)
	{
		if (useTween)
		{
			TweenDistance(distance);
		}
		else
		{
			_camera.Position = new Vector3(0, 0, distance);
			CameraEventHub.I.NotifyDistance(_camera.Position.Z);
		}
	}

	/// <summary>
	/// カメラのサイズを設定します。
	/// </summary>
	/// <param name="size">設定するサイズです。</param>
	/// <param name="useTween"><see langword="true"/> の場合は補間アニメーションを使用します。</param>
	private void ChangeSizeTo(float size, bool useTween = false)
	{
		if (useTween)
		{
			TweenSize(size);
		}
		else
		{
			_camera.Size = size;
			CameraEventHub.I.NotifySize(_camera.Size);
		}
	}

	/// <summary>
	/// カメラの投影方式を設定します。
	/// </summary>
	/// <param name="projectionType">切り替え先の投影方式です。</param>
	private void SetProjectionType(Camera3D.ProjectionType projectionType)
	{
		if (_camera.Projection == projectionType)
		{
			return;
		}

		if (projectionType == Camera3D.ProjectionType.Perspective)
		{
			float distance = GetPerspectiveDistanceFromOrthographicSize();
			MoveDistanceTo(distance, false);
		}
		else
		{
			float size = GetOrthographicSizeFromPerspectiveDistance();
			ChangeSizeTo(size, false);

			// 投影物がカメラの視界から出ないようにNearとFarの中間あたりに注視点を置く
			float farZ = (_camera.Near + _camera.Far) / 2.0f;
			MoveDistanceTo(farZ, false);
		}

		_camera.Projection = projectionType;
		CameraEventHub.I.NotifyProjectionType(projectionType);
	}

	/// <summary>
	/// 現在の投影方式を Perspective/Orthogonal でトグルします。
	/// </summary>
	private void ToggleProjectionType()
	{
		// 現在の投影方式をトグルして、内部で必要な補正を行う。
		Camera3D.ProjectionType nextProjection = _camera.Projection == Camera3D.ProjectionType.Perspective
			? Camera3D.ProjectionType.Orthogonal
			: Camera3D.ProjectionType.Perspective;
		SetProjectionType(nextProjection);
	}

	/// <summary>
	/// カメラの視野角（FOV）を設定します。
	/// </summary>
	/// <param name="fov">設定する視野角（度）。</param>
	/// <param name="useTween"><see langword="true"/> の場合は補間アニメーションを使用。</param>
	private void SetFov(float fov, bool useTween = false)
	{
		if (useTween)
		{
			TweenFov(fov);
		}
		else
		{
			_camera.Fov = fov;
			CameraEventHub.I.NotifyFov(fov);
		}
	}

	/// <summary>
	/// 指定ノード配下を画角内に収めるようカメラを調整します。
	/// </summary>
	/// <param name="targetRoot">フィット対象のルートノード。</param>
	/// <param name="useTween"><see langword="true"/> の場合は補間アニメーションを使用。</param>
	/// <returns>フィット対象の AABB を取得できた場合は <see langword="true"/>。</returns>
	private bool Fit(Node targetRoot, bool useTween = false)
	{
		if (!TryGetAabb(targetRoot, out Aabb worldAabb))
		{
			return false;
		}

		Vector3 center = worldAabb.Position + worldAabb.Size * 0.5f;
		MovePositionTo(center, useTween);

		Basis inverseBasis = Transform.Basis.Inverse();
		Rect2 viewportRect = GetViewport().GetVisibleRect();
		float aspect = Mathf.Max(viewportRect.Size.X / Mathf.Max(viewportRect.Size.Y, 1.0f), 0.01f);

		float maxAbsX = 0.0f;
		float maxAbsY = 0.0f;
		float maxZ = float.NegativeInfinity;
		float requiredDistance = 0.0f;

		if (_camera.Projection == Camera3D.ProjectionType.Perspective)
		{
			float halfVerticalFov = Mathf.DegToRad(_camera.Fov) * 0.5f;
			float tanHalfY = Mathf.Max(Mathf.Tan(halfVerticalFov), 1e-5f);
			float tanHalfX = Mathf.Max(tanHalfY * aspect, 1e-5f);

			foreach (Vector3 corner in GetAabbCorners(worldAabb))
			{
				Vector3 local = inverseBasis * (corner - center);
				requiredDistance = Mathf.Max(requiredDistance, local.Z + Mathf.Abs(local.X) / tanHalfX);
				requiredDistance = Mathf.Max(requiredDistance, local.Z + Mathf.Abs(local.Y) / tanHalfY);
				maxZ = Mathf.Max(maxZ, local.Z);
			}

			requiredDistance = Mathf.Max(requiredDistance, maxZ + _camera.Near * 1.5f);
			requiredDistance = Mathf.Max(requiredDistance * _fitPadding, _minZoomValue);

			MoveDistanceTo(requiredDistance, useTween);
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

			ChangeSizeTo(targetSize, useTween);
		}

		return true;
	}

	/// <summary>
	/// カメラの法線方向を指定したベクトルに整列させるよう回転を調整します。
	/// </summary>
	/// <param name="normal">整列先の法線方向を表すベクトルです。</param>
	/// <param name="useTween"><see langword="true"/> の場合は補間アニメーションを使用。</param>
	private void AlignNormalTo(Vector3 normal, bool useTween = false)
	{
		Quaternion rotation = new Quaternion();
		// ☆将来的に実装予定☆
		MoveRotationTo(rotation, useTween);
	}

	/// <summary>
	/// Tween を構築するための共通処理です。Tween の設定はこれで作成すると統一されます。
	/// </summary>
	/// <returns>構築された Tween オブジェクト。</returns>
	private Tween BuildTween()
	{
		return CreateTween()
			.SetTrans(Tween.TransitionType.Cubic)
			.SetEase(Tween.EaseType.Out);
	}

	/// <summary>
	/// 位置の補間アニメーションを実行します。
	/// </summary>
	/// <param name="position">補間先の位置です。</param>
	private void TweenPosition(Vector3 position)
	{
		Tween tween = BuildTween();
		Vector3 startPos = Position;
		tween.TweenMethod(Callable.From<float>(t =>
		{
			Position = startPos.Lerp(position, t);
			CameraEventHub.I.NotifyPosition(Position);
		}), 0f, 1f, _tweenDuration);
	}

	/// <summary>
	/// 回転の補間アニメーションを実行します。
	/// </summary>
	/// <param name="rotation">補間先の回転です。</param>
	private void TweenRotation(Quaternion rotation)
	{
		Tween tween = BuildTween();
		Quaternion startRot = Transform.Basis.GetRotationQuaternion();
		tween.TweenMethod(Callable.From<float>(t =>
		{
			Transform = new Transform3D(
				new Basis(startRot.Slerp(rotation, t)),
				Transform.Origin
			);
			CameraEventHub.I.NotifyRotation(Transform.Basis.GetRotationQuaternion());
		}), 0f, 1f, _tweenDuration);
	}

	/// <summary>
	/// 距離の補間アニメーションを実行します。
	/// </summary>
	/// <param name="distance">補間先の距離です。</param>
	private void TweenDistance(float distance)
	{
		Tween tween = BuildTween();
		float startDistance = _camera.Position.Z;
		tween.TweenMethod(Callable.From<float>(distance =>
		{
			_camera.Position = new Vector3(0, 0, distance);
			CameraEventHub.I.NotifyDistance(distance);
		}), startDistance, distance, _tweenDuration);
	}

	/// <summary>
	/// サイズの補間アニメーションを実行します。
	/// </summary>
	/// <param name="size">補間先のサイズです。</param>
	private void TweenSize(float size)
	{
		Tween tween = BuildTween();
		float startSize = _camera.Size;
		tween.TweenMethod(Callable.From<float>(size =>
		{
			_camera.Size = size;
			CameraEventHub.I.NotifySize(size);
		}), startSize, size, _tweenDuration);
	}

	/// <summary>
	/// 視野角（FOV）の補間アニメーションを実行します。
	/// </summary>
	/// <param name="fov">補間先の視野角です。</param>
	private void TweenFov(float fov)
	{
		Tween tween = BuildTween();
		float startFov = _camera.Fov;
		tween.TweenMethod(Callable.From<float>(fov =>
		{
			_camera.Fov = fov;
			CameraEventHub.I.NotifyFov(fov);
		}), startFov, fov, _tweenDuration);
	}

	/// <summary>
	/// Orthogonal のサイズを Perspective のカメラ距離へ変換します。
	/// </summary>
	/// <returns>Perspective のカメラ距離。</returns>
	private float GetPerspectiveDistanceFromOrthographicSize()
	{
		float sizeAtZ1 = CalculateSizeAtZ1();
		return _camera.Size / sizeAtZ1;
	}

	/// <summary>
	/// Perspective のカメラ距離を Orthogonal のサイズへ変換します。
	/// </summary>
	/// <returns>Orthogonal のサイズ。</returns>
	private float GetOrthographicSizeFromPerspectiveDistance()
	{
		// 等角投影の場合はZ距離を固定して、サイズでズームを表現する方式にする
		float sizeAtZ1 = CalculateSizeAtZ1();
		return Mathf.Abs(_camera.Position.Z) * sizeAtZ1;
	}

	/// <summary>
	/// カメラのFOVから、距離Z=1のときに見える縦サイズを計算します。
	/// </summary>
	/// <returns>距離Z=1のときに見える縦サイズ。</returns>
	private float CalculateSizeAtZ1()
	{
		// FOVは度数法で与えられるため、ラジアンに変換
		float fovRadians = Mathf.DegToRad(_camera.Fov);

		// FOVの半分の角度のタンジェントを使用して計算
		return Mathf.Tan(fovRadians / 2.0f) * 2.0f; // Z=1のときのサイズを計算
	}

	/// <summary>
	/// 指定ノード配下の MeshInstance3D から AABB を合算して取得します。MeshInstance3D が存在しない場合は <see langword="false"/> を返します。
	/// </summary>
	/// <param name="node">AABB を取得する対象のノードです。</param>
	/// <returns>合算された AABB。MeshInstance3D が存在しない場合はデフォルトの AABB を返します。</returns>
	private Aabb GetAabb(Node node)
	{
		return TryGetAabb(node, out Aabb aabb) ? aabb : new Aabb();
	}

	/// <summary>
	/// 指定ノード配下の MeshInstance3D から AABB を合算して取得します。MeshInstance3D が存在しない場合は <see langword="false"/> を返します。
	/// </summary>
	/// <param name="node">AABB を取得する対象のノードです。</param>
	/// <param name="aabb">合算された AABB を格納する変数です。</param>
	/// <returns>MeshInstance3D が存在する場合は <see langword="true"/>、存在しない場合は <see langword="false"/> を返します。</returns>
	private bool TryGetAabb(Node node, out Aabb aabb)
	{
		// ノード階層配下の Mesh のみを対象に AABB を合算する。
		aabb = default;
		bool hasMeshAabb = false;
		GetAabbRecursive(node, ref aabb, ref hasMeshAabb);
		return hasMeshAabb;
	}

	/// <summary>
	/// 指定ノード配下の MeshInstance3D から AABB を再帰的に取得します。
	/// </summary>
	/// <param name="node">AABB を取得する対象のノードです。</param>
	/// <param name="aabb">合算された AABB を格納する変数です。</param>
	/// <param name="hasMeshAabb">MeshInstance3D が存在するかどうかを示すフラグです。</param>
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

	/// <summary>
	/// <see cref="MeshInstance3D"/> とそのローカルAABBからワールドAABBを構築します。
	/// </summary>
	/// <param name="meshInstance">AABBをワールド空間に変換する対象の<see cref="MeshInstance3D"/>です。</param>
	/// <param name="localAabb">ローカル空間のAABBです。</param>
	/// <returns>ワールド空間のAABB。</returns>
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

	/// <summary>
	/// 2つのAABBを合算して、それらを完全に包含する最小のAABBを返します。
	/// </summary> <param name="a">AABB A。</param>
	/// <param name="b">AABB B。</param>
	/// <returns>AABB A と B を完全に包含する最小のAABB。</returns>
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

	/// <summary>
	/// AABBの8つのコーナーポイントを返します。
	/// </summary> <param name="aabb">コーナーポイントを取得する対象のAABB。</param>
	/// <returns>AABBの8つのコーナーポイント。</returns>
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

	/// <summary>
	/// カメラ状態を JSON 文字列にシリアライズします。
	/// </summary>
	/// <returns>カメラ状態を表す JSON 文字列。</returns>
	private string ToJson()
	{
		var state = new Godot.Collections.Dictionary();
		state["position"] = new Godot.Collections.Array { Position.X, Position.Y, Position.Z };
		state["rotation"] = new Godot.Collections.Array { Rotation.X, Rotation.Y, Rotation.Z };
		state["projection"] = (int)_camera.Projection;
		state["fov"] = _camera.Fov;
		state["distance"] = _camera.Position.Z;
		state["size"] = _camera.Size;
		return Json.Stringify(state);
	}

	/// <summary>
	/// JSON 文字列からカメラ状態を復元します。
	/// </summary>
	/// <param name="json">復元対象の JSON 文字列。</param>
	private void LoadJson(string json)
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
		if (JsonUtility.TryGetVector3(state, "position", out Vector3 position))
		{
			SetPosition(position);
		}

		if (JsonUtility.TryGetVector3(state, "rotation", out Vector3 rotation))
		{
			SetRotation(rotation);
		}

		// FOV
		if (JsonUtility.TryGetFloat(state, "fov", out float fov))
		{
			_camera.Fov = fov;
		}

		// カメラZ位置
		if (JsonUtility.TryGetFloat(state, "distance", out float distance))
		{
			_camera.Position = new Vector3(0, 0, distance);
		}

		// 等角投影サイズ
		if (JsonUtility.TryGetFloat(state, "size", out float size))
		{
			_camera.Size = size;
		}

		// カメラ投影
		if (JsonUtility.TryGetInt(state, "projection", out int projection)
			&& Enum.IsDefined(typeof(Camera3D.ProjectionType), projection))
		{
			_camera.Projection = (Camera3D.ProjectionType)projection;
		}
	}

	#endregion
}
