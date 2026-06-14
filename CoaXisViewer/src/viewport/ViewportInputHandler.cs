using Godot;
using System;

/// <summary>
/// メインの3Dビューの入力を受け取り、EventHub へ中継します。
/// </summary>
/// <remarks>
/// クラスに関する備考や注意点をここに記述します。
/// </remarks>
public partial class ViewportInputHandler : SubViewport
{
	#region Fields
	
	[ExportGroup("Settings")]
	[Export] private float _zoomFactor = 1.0f; // ズーム倍率変更時の係数
	[Export] private float _arcballRegionRatio = 0.45f; // 画面サイズに対する、Orbit/Rollの切り替え用の円領域の半径比率
	[Export] private Material _defaultMaterial; // 通常表示用のマテリアル（将来の拡張で使用予定）
	[Export] private Material _selectedMaterial; // 選択ハイライト用のマテリアル（将来の拡張で使用予定）

	private ViewportInputMode _mode = ViewportInputMode.None; // 現在の操作モード
	private Vector2 _lastPosition = Vector2.Zero; // 移動量算出のために前フレームの操作座標を保持
	private Vector2 _startPosition = Vector2.Zero; // 操作開始点の座標を保持
	private bool _hasMoved = false; // ボタンを押してから移動操作したかのフラグ。マウスのクリックと移動の区別に使用
	private Vector2 _screenCenter; // 画面中心座標のキャッシュ
	private float _arcballRadius; // アークボール半径のキャッシュ

	#endregion

	#region Lifecycle

	public override void _Ready()
	{
		// ビューポートサイズの変更を検知してキャッシュを更新するためのシグナル接続
		SizeChanged += OnSizeChanged;
		
		// イベントの購読
		ViewportEventHub.I.NotifyStateRequested += OnNotifyStateRequested;

		// ビューポートサイズに基づいて、アークボールのパラメータを初期化する。
		RefreshArcballParameters();
	}

    public override void _ExitTree()
	{
		// シグナルの切断
		SizeChanged -= OnSizeChanged;

		// イベントの購読解除
		ViewportEventHub.I.NotifyStateRequested -= OnNotifyStateRequested;
	}

	public override void _Process(double delta)
	{

		// 入力モードが None のときは、マウス移動の検知やカメラ操作の適用を行わない。
		if (_mode == ViewportInputMode.None)
		{
			return;
		}

		// マウス位置の変化を検知して、変化があれば操作を適用する。
		Vector2 currentPos = GetMousePosition();
		if (currentPos != _lastPosition)
		{
			_hasMoved = true;
			ApplyOperation(_lastPosition, currentPos);
			
			// 現在のマウス位置を保存して次フレームに備える。
			_lastPosition = currentPos;
		}

	}

	public override void _UnhandledInput(InputEvent @event)
	{
		// マウスボタンイベント以外は無視する。
		if (@event is not InputEventMouseButton button)
		{
			return;
		}

		OnMouseButtonClicked(button);
	}

	#endregion

	#region Events

	/// <summary>
	/// ビューポートサイズ変更時に呼び出されるイベントハンドラです。
	/// </summary>
	private void OnSizeChanged()
	{
		RefreshArcballParameters();
	}

	/// <summary>
	/// カメラ関連の状態の通知がリクエストされたときに呼び出されるイベントハンドラです。
	/// </summary>
	private void OnNotifyStateRequested()
	{
		ViewportEventHub.I.NotifyInputMode(_mode);
		ViewportEventHub.I.NotifyArcballRadius(_arcballRadius);
		ViewportEventHub.I.NotifyArcballHandle(new Vector3(0, 0, 1)); // アークボールハンドルは初期状態では画面正面方向にしておく
	}

	/// <summary>
	/// マウスボタン入力に応じた処理を行います。
	/// </summary>
	/// <param name="button">マウスボタン入力イベントです。</param>
	private void OnMouseButtonClicked(InputEventMouseButton button)
	{
		// 入力モードに応じて、マウス入力の処理を分岐する。
		if (_mode == ViewportInputMode.None)
		{
			// None モードのときは、カメラ操作開始のトリガーを検知するための処理を行う。
			HandleIdleModeInput(button);
		}
		else if (_mode == ViewportInputMode.SelectRect)
		{
			// Selectモードのときは、矩形選択操作の開始・終了を検知するための処理を行う。
			HandleSelectModeInput(button);
		}
		else
		{
			// CameraControlモードのときは、カメラ操作の開始・終了を検知するための処理を行う。
			HandleCameraControlModeInput(button);
		}
	}

	#endregion

	#region Internal Helpers
	
	/// <summary>
	/// 操作モードを切り替え、EventHub を通じて変更を通知します。
	/// </summary>
	/// <param name="mode">新しい操作モードです。</param>
	private void SetMode(ViewportInputMode mode)
	{
		if (_mode == mode)
		{
			return;
		}

		_mode = mode;
		ViewportEventHub.I.NotifyInputMode(mode);
	}

	/// <summary>
	/// 入力モードが None のときのマウス入力を処理します。
	/// </summary>
	/// <param name="button">マウスボタン入力イベントです。</param>
	private void HandleIdleModeInput(InputEventMouseButton button)
	{
		// 中ボタンのクリック開始を検知したら、移動フラグをリセットしてカメラコントロール開始
		if (button.Pressed && button.ButtonIndex == MouseButton.Middle)
		{
			_hasMoved = false;
			_lastPosition = button.Position;
			SetMode(ViewportInputMode.CameraPan);
		}
		// 左ボタンのクリック開始を検知したら、矩形選択操作を行う
		else if (button.Pressed && button.ButtonIndex == MouseButton.Left)
		{
			_hasMoved = false;
			_startPosition = button.Position;
			SetMode(ViewportInputMode.SelectRect);
		}
		else if (button.Pressed && button.ButtonIndex == MouseButton.Right)
		{
			// 右クリックで注視点にフォーカス後、法線方向にカメラを整列させる
			TryFocusAt(button.Position, true);
			TryAlignNormalTo(button.Position, true);
		}
	}

	private void HandleSelectModeInput(InputEventMouseButton button)
	{
		// ウィンドウフォーカス喪失などのキャンセル時は即時終了。
		if (button.Canceled)
		{
			// 何らかの理由で操作がキャンセルされた場合は、確実にコントロールを終了する。
			SetMode(ViewportInputMode.None);
			return;
		}

		// 左ボタンのクリック終了を検知したら、矩形選択操作を終了
		if (!button.Pressed && button.ButtonIndex == MouseButton.Left)
		{
			if (_hasMoved)
			{
				/////////////////////////////////////////////矩形選択を実装予定////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
			}
			else
			{
				// ドラッグしていない場合は、クリックとみなして単一選択を行う。
				TrySelect(button.Position);
			}

			SetMode(ViewportInputMode.None);
		}
	}

	/// <summary>
	/// カメラ操作モード中のマウス入力を処理します。
	/// </summary>
	/// <param name="button">マウスボタン入力イベントです。</param>
	private void HandleCameraControlModeInput(InputEventMouseButton button)
	{
		// ウィンドウフォーカス喪失などのキャンセル時は即時終了。
		if (button.Canceled)
		{
			// 何らかの理由で操作がキャンセルされた場合は、確実にコントロールを終了する。
			SetMode(ViewportInputMode.None);
			return;
		}

		// 中ボタンのクリック終了を検知したら、移動していなければクリックとみなし Focus を行い、カメラコントロール終了
		if (!button.Pressed && button.ButtonIndex == MouseButton.Middle)
		{
			if (!_hasMoved)
			{
				if(!TryFocusAt(button.Position))
				{
					PanCamera(_screenCenter, button.Position); // フォーカスできなかったら、クリック位置にパン扱いとする
				}
			}
			SetMode(ViewportInputMode.None);
			return;
		}

		// 左右ボタンの入力を検知したら、Pan → Orbit/Roll または Orbit/Roll → Zoom へ遷移するのでそれ以外は無視する。
		if (button.ButtonIndex != MouseButton.Left && button.ButtonIndex != MouseButton.Right)
		{
			return;
		}

		// 中ボタンを押したまま右or左クリック開始を検知したら、位置によってOrbit/Rollモードに切り替え
		if (button.Pressed)
		{
			_hasMoved = true; // クリック操作したら注視点移動しないようににするため、移動フラグを立てる
			SetMode(IsOnArcball(button.Position) ? ViewportInputMode.CameraOrbit : ViewportInputMode.CameraRoll);
			Vector3 positionOnArcball = GetPositionOnArcballSphere(button.Position);
			ViewportEventHub.I.NotifyArcballHandle(positionOnArcball);
			return;
		}
		
		// 中ボタンを押したまま右or左クリック終了を検知したら、Zoomモードに切り替え
		SetMode(ViewportInputMode.CameraZoom);
	}

	/// <summary>
	/// 入力モード中の移動量に応じて、EventHub を通じて注視点の移動や回転をリクエストします。
	/// </summary>
	/// <param name="previousPos">前フレームの画面上位置です。</param>
	/// <param name="currentPos">現在の画面上位置です。</param>
	/// <remarks>
	/// _modeがNoneのときは呼び出されない前提です。
	/// currentPos と previousPos は、画面上の移動量を算出するために使用され、移動がない場合は呼び出されません。
	/// </remarks>
	private void ApplyOperation(Vector2 previousPos, Vector2 currentPos)
	{
		switch (_mode)
		{
			case ViewportInputMode.CameraPan:
				PanCamera(previousPos, currentPos);
				break;
			case ViewportInputMode.CameraOrbit:
				OrbitCamera(previousPos, currentPos);
				break;
			case ViewportInputMode.CameraRoll:
				if (IsOnArcball(currentPos))
				{
					// 画面中央寄りに入ったらOrbitに変更、外周寄りはRollのままにする
					SetMode(ViewportInputMode.CameraOrbit);
					OrbitCamera(previousPos, currentPos);
				}
				else
				{
					RollCamera(previousPos, currentPos);
				}
				break;
			case ViewportInputMode.CameraZoom:
				ZoomCamera(previousPos, currentPos);
				break;
		}
	}

	/// <summary>
	/// 画面上の指定された位置に注視点を移動します。
	/// </summary>
	/// <param name="screenPos">スクリーン座標</param>
	/// <param name="useTween"><see langword="true"/> の場合は補間アニメーションを使用。</param>
	/// <returns>注視点を移動できた場合は true、レイキャストがヒットしなかったなどで移動できなかった場合は false を返します。</returns>
	private bool TryFocusAt(Vector2 screenPos, bool useTween = false)
	{
		// レイキャストしてヒット情報を取得
		var hit = RaycastService.RaycastFromScreen(GetCamera3D(), screenPos);

		if (hit.HasHit)
		{
			// ヒットしたら注視点を移動
			ViewportEventHub.I.RequestMovePositionTo(hit.Position, useTween);
			return true;
		}
		else
		{
			return false;
		}
	}

	/// <summary>
	/// 画面上の指定された位置の法線にカメラの向きを合わせます。
	/// </summary>
	/// <param name="screenPos">スクリーン座標</param>
	/// <param name="useTween"><see langword="true"/> の場合は補間アニメーションを使用。</param>
	/// <returns>法線にカメラを合わせられた場合は true、レイキャストがヒットしなかった場合は false を返します。</returns>
	private bool TryAlignNormalTo(Vector2 screenPos, bool useTween = false)
	{
		// レイキャストしてヒット情報を取得
		var hit = RaycastService.RaycastFromScreen(GetCamera3D(), screenPos);

		if (hit.HasHit)
		{
			ViewportEventHub.I.RequestAlignNormalTo(hit.Normal, useTween);
			return true;
		}
		else
		{
			return false;
		}
	}

	/// <summary>
	/// カメラをパン移動します。
	/// </summary>
	/// <param name="fromScreenPos">スクリーン座標の開始位置</param>
	/// <param name="toScreenPos">スクリーン座標の終了位置</param>
	private void PanCamera(Vector2 fromScreenPos, Vector2 toScreenPos)
	{
		// 同一深度平面で、スクリーン座標 from -> to に対応するワールド移動量を求める。
		// これにより、画面サイズ・Orthogonal の Size・Perspective の FOV/Z 距離を自動で吸収する。
		Camera3D camera = GetCamera3D();
		float panDepth = camera.Position.Z;
		Vector3 fromWorld = camera.ProjectPosition(fromScreenPos, panDepth);
		Vector3 toWorld = camera.ProjectPosition(toScreenPos, panDepth);
		// ドラッグ方向に見た目が追従するよう、差分を逆向きで適用する。
		Vector3 move = fromWorld - toWorld;

		ViewportEventHub.I.RequestTranslate(move, SpaceMode.World);
	}

	/// <summary>
	/// カメラをオービット回転させます。
	/// </summary>
	/// <param name="previousPos">前フレームの画面上位置です。</param>
	/// <param name="currentPos">現在の画面上位置です。</param>
	private void OrbitCamera(Vector2 previousPos, Vector2 currentPos)
	{
		// 仮想アークボール（アークボール）方式でFocalPointを回転させる。
		// Orbit/Roll判定と同じ円を球面半径として使い、
		// 2点の球面座標から回転軸・角度を求めてFocalPointのローカル軸で回転する。
		// FocalPointの回転はArcballの回転と逆向きになるように計算する。
		Vector3 p0 = GetPositionOnArcballSphere(currentPos);
		Vector3 p1 = GetPositionOnArcballSphere(previousPos);
		Quaternion rotation = ComputeArcballRotation(p0, p1);

		ViewportEventHub.I.RequestRotate(rotation, SpaceMode.FocalPoint);
	}

	/// <summary>
	/// カメラをロール回転させます。
	/// </summary>
	/// <param name="previousPos">前フレームの画面上位置です。</param>
	/// <param name="currentPos">現在の画面上位置です。</param>
	private void RollCamera(Vector2 previousPos, Vector2 currentPos)
	{
		// 画面中心から見た角度差を使ってロール量を計算する。
		// 前フレームと今フレームの画面上位置ベクトル（中心基準）
		// FocalPointの回転はArcballの回転と逆向きになるように計算する。
		Vector3 p0 = GetPositionOnArcballEquator(currentPos);
		Vector3 p1 = GetPositionOnArcballEquator(previousPos);
		Quaternion rotation = ComputeArcballRotation(p0, p1);

		ViewportEventHub.I.RequestRotate(rotation, SpaceMode.FocalPoint);
	}

	/// <summary>
	/// カメラをズームさせます。
	/// </summary> <param name="previousPos">前フレームの画面上位置です。</param>
	/// <param name="currentPos">現在の画面上位置です。</param>
	private void ZoomCamera(Vector2 previousPos, Vector2 currentPos)
	{
		float deltaY = (currentPos.Y - previousPos.Y);
		float exponent = deltaY * _zoomFactor;

		ViewportEventHub.I.RequestZoom(exponent);
	}

	/// <summary>
	/// ビューポートサイズの変更に応じて、アークボールのパラメータを更新します。
	/// </summary>
	private void RefreshArcballParameters()
	{
		Rect2 rect = GetVisibleRect();
		_screenCenter = rect.Position + rect.Size * 0.5f;
		_arcballRadius = rect.Size.Y * _arcballRegionRatio;

		ViewportEventHub.I.NotifyArcballRadius(_arcballRadius);
	}

	/// <summary>
	/// 指定されたスクリーン座標が、アークボールの操作領域（画面中央の円領域）内にあるかどうかを判定します。
	/// </summary>
	/// <param name="screenPos">スクリーン座標</param>
	/// <returns>アークボールの操作領域内にある場合は true、それ以外の場合は false を返します。</returns>
	private bool IsOnArcball(Vector2 screenPos)
	{
		// Orbit/Roll の分岐用に、画面中央の円領域判定を行う。
		return screenPos.DistanceTo(_screenCenter) <= _arcballRadius; // 円形判定
	}

	/// <summary>
	/// スクリーン座標をアークボール球面上の座標に変換します。
	/// </summary>
	/// <param name="screenPos">スクリーン座標</param>
	/// <returns>アークボール球面上の3D座標</returns>
	private Vector3 GetPositionOnArcballSphere(Vector2 screenPos)
	{
		// スクリーン座標をアークボール球面上の3D座標へ変換する。
		// Y軸はスクリーン下向きを反転して3D上向きに合わせる。
		float x = (screenPos.X - _screenCenter.X) / _arcballRadius;
		float y = -(screenPos.Y - _screenCenter.Y) / _arcballRadius;

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

	/// <summary>
	/// スクリーン座標をアークボールの赤道（z=0平面）に投影する。
	/// </summary>
	/// <param name="screenPos">スクリーン座標</param>
	/// <returns>アークボールの赤道上の3D座標を返します。</returns>
	private Vector3 GetPositionOnArcballEquator(Vector2 screenPos)
	{
		// スクリーン座標を Arcball の正規化平面へ
		float x = (screenPos.X - _screenCenter.X) / _arcballRadius;
		float y = -(screenPos.Y - _screenCenter.Y) / _arcballRadius;

		// 原点からの距離
		float len = Mathf.Sqrt(x * x + y * y);

		if (len < 1e-6f)
		{
			// ど真ん中（中心）なら X 軸方向に置く（Roll の基準方向）
			return Vector3.Right;
		}

		// 円周上に正規化（赤道上に投影）
		float nx = x / len;
		float ny = y / len;

		// Arcball 赤道は z = 0
		return new Vector3(nx, ny, 0.0f);
	}

	/// <summary>
	/// 2点のアークボール球面上の座標から、回転軸と回転角を計算します。
	/// </summary>
	/// <param name="p0">アークボール上の最初の点です。</param>
	/// <param name="p1">アークボール上の2番目の点です。</param>
	/// <returns>アークボール上の点p0からp1への回転を表すクォータニオンを返します。</returns>
	/// <remarks>。
	/// この関数は、p0とp1が同一位置の場合や、ほぼ同一位置の場合にも安定して回転を計算できるように設計されています。
	/// </remarks>
	private static Quaternion ComputeArcballRotation(Vector3 p0, Vector3 p1)
	{
		Vector3 axis = p0.Cross(p1);
		float dot = Mathf.Clamp(p0.Dot(p1), -1.0f, 1.0f);
		float angle = Mathf.Acos(dot);

		if (axis.LengthSquared() < 1e-6f)
		{
			axis = Vector3.Up; // どこでもいいが、ゼロ除算回避
		}
		else
		{
			axis = axis.Normalized();
		}
		return new Quaternion(axis, angle);
	}

	private void TrySelect(Vector2 screenPos)
	{
		var hit = RaycastService.RaycastFromScreen(GetCamera3D(), screenPos);

		if (hit.HasHit)
		{
			Node3D selectedNode = hit.Collider.GetParentOrNull<Node3D>();
			Selection.Set(selectedNode);
		}
	}

	#endregion
}