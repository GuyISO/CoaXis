using Godot;

/// <summary>
/// 画面中央のナビゲータ（中心軸・アークボール補助表示）を描画します。
/// </summary>
/// <remarks>
/// 描画対象のオブジェクトは常に保持しているため、描画の更新はイベント駆動で行います。カメラの状態が変化したときにイベントを発行してもらい、そのイベントを受け取ったときに表示状態に反映します。
/// </remarks>
public partial class ViewportOverlay : Control
{
	#region Fields

	private const float ArcballOutlineDashLength = 16.0f;
	private const float ArcballOutlineGapLength = 8.0f;
	private const int MinArcballOutlineDashCount = 8;
	private const float CenterAxisLength = 32.0f;
	private const float CenterAxisGap = 16.0f;
	private const float ArcballCrossAngularSize = 0.24f;
	private const int ArcballCrossCurveSegments = 12;

	[Export] private Color _lineColor = new Color(231f / 255f, 177f / 255f, 246f / 255f);

	private bool _isInitialized = false; // カメラの初期状態を取得してUIに反映するためのフラグ
	private float _arcballRadius = 0.0f; // アークボールの半径
	private Quaternion _arcballHandleRotation = Quaternion.Identity;

	// 関連ノードのキャッシュ
	private Control _centerAxis;
	private Line2D _axisXPositive;
	private Line2D _axisXNegative;
	private Line2D _axisYPositive;
	private Line2D _axisYNegative;
	private Line2D _axisZ;
	private Control _arcballCross;
	private Line2D _ballX;
	private Line2D _ballY;
	private Control _arcballOutline;

	#endregion

	#region Lifecycle

	public override void _Ready()
	{
		// 関連ノードのキャッシュ
		_centerAxis = GetNode<Control>("CenterAxis");
		_axisXPositive = _centerAxis.GetNode<Line2D>("LineXPositive");
		_axisXNegative = _centerAxis.GetNode<Line2D>("LineXNegative");
		_axisYPositive = _centerAxis.GetNode<Line2D>("LineYPositive");
		_axisYNegative = _centerAxis.GetNode<Line2D>("LineYNegative");
		_axisZ = _centerAxis.GetNode<Line2D>("LineZ");
		_arcballCross = GetNode<Control>("ArcballCross");
		_ballX = _arcballCross.GetNode<Line2D>("LineX");
		_ballY = _arcballCross.GetNode<Line2D>("LineY");
		_arcballOutline = GetNode<Control>("ArcballOutline");

		// イベントの購読登録
		CameraEventHub.I.RotateRequested += OnRotateRequested;
		CameraEventHub.I.RotationNotified += OnRotationNotified;
		CameraEventHub.I.ControlModeNotified += OnControlModeNotified;
		CameraEventHub.I.ArcballRadiusNotified += OnArcballRadiusNotified;
		CameraEventHub.I.ArcballHandleNotified += OnArcballHandleNotified;
	}

	public override void _ExitTree()
	{
		// イベントの購読解除
		CameraEventHub.I.RotateRequested -= OnRotateRequested;
		CameraEventHub.I.RotationNotified -= OnRotationNotified;
		CameraEventHub.I.ControlModeNotified -= OnControlModeNotified;
		CameraEventHub.I.ArcballRadiusNotified -= OnArcballRadiusNotified;
		CameraEventHub.I.ArcballHandleNotified -= OnArcballHandleNotified;
	}
	
	public override void _Process(double delta)
	{
		if (!_isInitialized)
		{
			// カメラの初期状態を取得してUIに反映する。
			CameraEventHub.I.RequestNotifyState();
			_isInitialized = true;
		}
	}

	#endregion

	#region Events

	/// <summary>
	/// カメラの回転の設定がリクエストされたときに呼び出されるイベントハンドラです。アークボールの接線ベクトルを回転に合わせて回転させます。
	/// </summary>
	/// <param name="rotation">リクエストされた回転です。</param>
	/// <param name="spaceMode">回転の基準となる座標系です。</param>
	/// <param name="useTween">回転に補間を使用するかどうかを示すフラグです。</param>
	/// <remarks>
	/// Arcball操作は回転結果を拾って処理するのが難しいため、リクエスト量を拾って処理します。
	/// </remarks>
	private void OnRotateRequested(Quaternion rotation, SpaceMode spaceMode, bool useTween)
	{
		if (spaceMode == SpaceMode.FocalPoint)
		{
			RotateArcball(rotation);
		}
	}

	/// <summary>
	/// カメラの回転が通知されたときに呼び出されるイベントハンドラです。中心軸の表示を更新します。
	/// </summary>
	/// <param name="rotation"></param>
	private void OnRotationNotified(Quaternion rotation)
	{
		DrawCenterAxis(rotation);
	}

	/// <summary>
	/// カメラの操作モードが通知されたときに呼び出されるイベントハンドラです。中心軸とアークボール補助表示の表示を切り替えます。
	/// </summary>
	/// <param name="mode">通知されたカメラの操作モードです。</param>
	private void OnControlModeNotified(CameraControlMode mode)
	{
		_arcballOutline.Visible = IsArcballMode(mode);
		_centerAxis.Visible = IsCenterAxisMode(mode);
		_arcballCross.Visible = IsArcballMode(mode);
	}

	/// <summary>
	/// カメラのアークボール操作の半径が通知されたときに呼び出されるイベントハンドラです。アークボールの補助表示を更新します。
	/// </summary>
	/// <param name="radius">通知されたアークボールの半径です。</param>
	private void OnArcballRadiusNotified(float radius)
	{
		_arcballRadius = radius;
		DrawArcballOutline();
	}

	/// <summary>
	/// カメラのアークボール操作のハンドル位置が通知されたときに呼び出されるイベントハンドラです。アークボールの操作点を更新します。
	/// </summary>
	/// <param name="position">通知されたアークボールのハンドル位置です。</param>
	private void OnArcballHandleNotified(Vector3 position)
	{
		ComputeArcballHandleRotation(position);
		DrawArcballCross();
	}

	#endregion

	#region Internal Helpers

	/// <summary>
	/// アークボールの回転を更新します。回転は現在の回転に乗算されて累積されます。
	/// </summary>
	/// <param name="rotation">適用する回転です。</param>
	private void RotateArcball(Quaternion rotation)
	{
		// Overlay はカメラ操作に対して見た目上逆向きに追従させる。
		_arcballHandleRotation = rotation.Inverse() * _arcballHandleRotation;
		DrawArcballCross();
	}

	/// <summary>
	/// アークボールの補助表示の円を描画します。円は指定された半径に基づいて構成されます。
	/// </summary>
	/// <param name="radius">描画する円の半径です。</param>
	private void DrawArcballOutline()
	{
		float circumference = Mathf.Tau * _arcballRadius;
		float cycleLength = Mathf.Max(ArcballOutlineDashLength + ArcballOutlineGapLength, 1.0f);
		int segmentCount = Mathf.Max(MinArcballOutlineDashCount, Mathf.RoundToInt(circumference / cycleLength));

		foreach (Node child in _arcballOutline.GetChildren())
		{
			child.QueueFree();
		}

		float step = Mathf.Tau / segmentCount;
		float dashAngle = Mathf.Min(step * 0.95f, Mathf.Tau * (ArcballOutlineDashLength / Mathf.Max(circumference, 1.0f)));

		for (int index = 0; index < segmentCount; index++)
		{
			float startAngle = index * step;
			float endAngle = startAngle + dashAngle;

			Vector2 start = new Vector2(Mathf.Cos(startAngle), Mathf.Sin(startAngle)) * _arcballRadius;
			Vector2 end = new Vector2(Mathf.Cos(endAngle), Mathf.Sin(endAngle)) * _arcballRadius;

			Line2D segment = new Line2D
			{
				Name = $"ArcballOutlineDash{index}",
				Width = 1.0f,
				DefaultColor = _lineColor,
				Antialiased = true
			};

			segment.AddPoint(start);
			segment.AddPoint(end);
			_arcballOutline.AddChild(segment);
		}
	}

	/// <summary>
	/// アークボールのハンドル位置に基づいて、ハンドルの回転を計算して更新します。
	/// </summary>
	/// <param name="handlePosition">通知されたアークボールのハンドル位置です。</param>
	private void ComputeArcballHandleRotation(Vector3 handlePosition)
	{
		if (handlePosition.LengthSquared() <= Mathf.Epsilon * Mathf.Epsilon)
		{
			_arcballHandleRotation = Quaternion.Identity;
			return;
		}

		Vector3 anchor = handlePosition.Normalized();

		// 画面投影で中心方向（-x, -y）を向く接線を作る。
		Vector3 desiredTowardCenter = new Vector3(-anchor.X, -anchor.Y, 0.0f);
		Vector3 tangentX = desiredTowardCenter - anchor * desiredTowardCenter.Dot(anchor);
		if (tangentX.LengthSquared() <= Mathf.Epsilon * Mathf.Epsilon)
		{
			Vector3 fallback = Vector3.Right - anchor * Vector3.Right.Dot(anchor);
			if (fallback.LengthSquared() <= Mathf.Epsilon * Mathf.Epsilon)
			{
				fallback = Vector3.Up - anchor * Vector3.Up.Dot(anchor);
			}

			tangentX = fallback;
		}

		tangentX = tangentX.Normalized();
		Vector3 zAxis = -anchor;
		Vector3 yAxis = zAxis.Cross(tangentX).Normalized();
		if (yAxis.LengthSquared() <= Mathf.Epsilon * Mathf.Epsilon)
		{
			yAxis = Vector3.Up;
		}

		// 直交化して安定した姿勢を作る。
		Vector3 xAxis = yAxis.Cross(zAxis).Normalized();
		Basis basis = new Basis(xAxis, yAxis, zAxis).Orthonormalized();
		_arcballHandleRotation = basis.GetRotationQuaternion();
	}

	/// <summary>
	/// 中心軸の表示を更新します。
	/// </summary>
	/// <param name="rotation">カメラの回転です。</param>
	private void DrawCenterAxis(Quaternion rotation)
	{
		Basis cameraInverseBasis = new Basis(rotation).Inverse();

		// CATIA風の見た目に合わせて、表示軸と Godot 軸の対応を入れ替える。
		// LineX = Godot Z(back), LineY = Godot X(right), LineZ = Godot Y(up)
		Vector2 axisX = ProjectWorldAxisToScreen(cameraInverseBasis * Vector3.Back);
		Vector2 axisY = ProjectWorldAxisToScreen(cameraInverseBasis * Vector3.Right);
		Vector2 axisZ = ProjectWorldAxisToScreen(cameraInverseBasis * Vector3.Up);

		SetSplitAxisLines(_axisXPositive, _axisXNegative, axisX);
		// CATIA風の見た目に寄せるため、Y の正方向だけ中心ギャップを少し詰める。
		SetSplitAxisLines(_axisYPositive, _axisYNegative, axisY, 0.5f);
		// CATIA風の見た目に寄せるため、Z は負方向だけ短くして前後の奥行き感を強める。
		SetLinePoints(_axisZ, -axisZ * 0.5f, axisZ);

	}

	/// <summary>
	///  カメラ空間の軸ベクトルを画面空間に投影します。投影されたベクトルの長さは、軸の向きに応じて CenterAxisLength に基づいてスケーリングされます。
	/// </summary>
	/// <param name="axisInCameraSpace">カメラ空間の軸ベクトルです。</param>
	/// <returns>画面空間に投影された軸ベクトルです。</returns>
	private Vector2 ProjectWorldAxisToScreen(Vector3 axisInCameraSpace)
	{
		Vector2 projected = new Vector2(axisInCameraSpace.X, -axisInCameraSpace.Y);

		// 傾きに応じた投影長をそのまま画面上の軸長へ反映する。
		return projected * CenterAxisLength;
	}

	/// <summary>
	/// 中心軸の正負両方のラインを、指定された軸方向に基づいて設定します。軸方向の長さに応じて、ラインの長さと中心ギャップが調整されます。
	/// </summary>
	/// <param name="line"></param>
	/// <param name="from"></param>
	/// <param name="to"></param>
	private static void SetLinePoints(Line2D line, Vector2 from, Vector2 to)
	{
		line.ClearPoints();
		line.AddPoint(from);
		line.AddPoint(to);
	}

	/// <summary>
	/// 中心軸の正負両方のラインを、指定された軸方向に基づいて設定します。
	/// </summary>
	/// <param name="positiveLine">正方向のラインです。</param>
	/// <param name="negativeLine">負方向のラインです。</param>
	/// <param name="axisDirection">軸の方向を表すベクトルです。長さは軸の傾きに応じてスケーリングされます。</param>
	/// <param name="positiveGapScale">正方向の中心ギャップをスケーリングするためのオプションのパラメータです。デフォルトは 1.0f で、値を小さくすると正方向の中心ギャップが縮小されます。</param>
	private static void SetSplitAxisLines(Line2D positiveLine, Line2D negativeLine, Vector2 axisDirection, float positiveGapScale = 1.0f)
	{
		float axisLength = axisDirection.Length();
		if (axisLength < 1e-6f)
		{
			SetLinePoints(positiveLine, Vector2.Zero, Vector2.Zero);
			SetLinePoints(negativeLine, Vector2.Zero, Vector2.Zero);
			return;
		}

		Vector2 direction = axisDirection / axisLength;
		Vector2 positiveStart = direction * (CenterAxisGap * positiveGapScale);
		Vector2 positiveEnd = direction * (CenterAxisGap + axisLength);
		Vector2 negativeStart = -direction * CenterAxisGap;
		Vector2 negativeEnd = -direction * (CenterAxisGap + axisLength);

		SetLinePoints(positiveLine, positiveStart, positiveEnd);
		SetLinePoints(negativeLine, negativeStart, negativeEnd);
	}

	/// <summary>
	/// アークボールのハンドル位置に基づいて、アークボールの補助表示の十字線を描画します。十字線は、ハンドル位置を中心に、カメラの回転に合わせて回転されます。
	/// </summary>
	private void DrawArcballCross()
	{
		if (_arcballRadius <= Mathf.Epsilon)
		{
			SetLinePoints(_ballX, Vector2.Zero, Vector2.Zero);
			SetLinePoints(_ballY, Vector2.Zero, Vector2.Zero);
			return;
		}

		// ハンドルの回転を正規化して安定させる。正規化されていない回転は、回転軸の計算で不安定な結果を引き起こす可能性がある。
		Quaternion normalizedHandleRotation = EnsureNormalizedQuaternion(_arcballHandleRotation);
		Vector3 anchor = (normalizedHandleRotation * Vector3.Forward).Normalized();
		Vector3 tangentX = (normalizedHandleRotation * Vector3.Right).Normalized();
		Vector3 tangentY = (normalizedHandleRotation * Vector3.Up).Normalized();

		// 球面上の接線方向を回転軸へ変換し、短い円弧をサンプリングして描画する。
		Vector3 axisX = anchor.Cross(tangentX).Normalized();
		Vector3 axisY = anchor.Cross(tangentY).Normalized();
		SetCurvedArcballLine(_ballX, anchor, axisX, ArcballCrossAngularSize);
		SetCurvedArcballLine(_ballY, anchor, axisY, ArcballCrossAngularSize);
	}

	/// <summary>
	/// Quaternion を単位長に正規化して返します。ゼロ長に近い値は Identity にフォールバックします。
	/// </summary>
	/// <param name="rotation">正規化する Quaternion です。</param>
	/// <returns>正規化済みの Quaternion です。</returns>
	private static Quaternion EnsureNormalizedQuaternion(Quaternion rotation)
	{
		float lengthSquared = rotation.X * rotation.X + rotation.Y * rotation.Y + rotation.Z * rotation.Z + rotation.W * rotation.W;
		if (lengthSquared <= Mathf.Epsilon * Mathf.Epsilon)
		{
			return Quaternion.Identity;
		}

		if (Mathf.Abs(lengthSquared - 1.0f) <= Mathf.Epsilon)
		{
			return rotation;
		}

		float invLength = 1.0f / Mathf.Sqrt(lengthSquared);
		return new Quaternion(rotation.X * invLength, rotation.Y * invLength, rotation.Z * invLength, rotation.W * invLength);
	}

	/// <summary>
	/// アークボールの補助表示の十字線の片方のラインを、指定された回転軸と角度に基づいて描画します。ラインは、アークボールのハンドル位置を中心に、回転軸を軸として、指定された角度の範囲で回転されます。
	/// </summary>
	/// <param name="line">描画するラインです。</param>
	/// <param name="anchor">回転の中心点であるアークボールのハンドル位置です。</param>
	/// <param name="rotationAxis">回転の軸です。回転軸がゼロベクトルに近い場合、回転は行われず、ラインはハンドル位置を中心とした直線になります。</param>
	/// <param name="angularSize">回転の角度の範囲です。ラインは、ハンドル位置を中心に、回転軸を軸として、-angularSize から +angularSize の範囲で回転されます。</param>
	private void SetCurvedArcballLine(Line2D line, Vector3 anchor, Vector3 rotationAxis, float angularSize)
	{
		line.ClearPoints();

		if (rotationAxis.LengthSquared() < 1e-8f)
		{
			line.AddPoint(ProjectArcballPointToScreen(anchor));
			return;
		}

		for (int i = 0; i <= ArcballCrossCurveSegments; i++)
		{
			float t = (float)i / ArcballCrossCurveSegments;
			float angle = Mathf.Lerp(-angularSize, angularSize, t);
			Vector3 point = anchor.Rotated(rotationAxis, angle).Normalized();
			line.AddPoint(ProjectArcballPointToScreen(point));
		}
	}

	/// <summary>
	/// アークボールの球面上の点を画面空間に投影します。投影された点は、アークボールの半径に基づいてスケーリングされます。
	/// </summary>
	/// <param name="pointOnArcball">アークボール上の点です。</param>
	/// <returns>画面空間に投影された点です。</returns>
	private Vector2 ProjectArcballPointToScreen(Vector3 pointOnArcball)
	{
		return new Vector2(pointOnArcball.X, -pointOnArcball.Y) * _arcballRadius;
	}

	/// <summary>
	/// CameraControlMode がアークボール操作モード（Orbit または Roll）かどうかを判定します。
	/// </summary>
	/// <param name="mode">判定する CameraControlMode です。</param>
	/// <returns>アークボール操作モードであれば true を返します。</returns>
	private bool IsArcballMode(CameraControlMode mode)
	{
		return mode == CameraControlMode.Orbit || mode == CameraControlMode.Roll;
	}

	/// <summary>
	/// CameraControlMode が中心軸表示モード（Orbit、Pan、Zoom、Roll）かどうかを判定します。
	/// </summary>
	/// <param name="mode">判定する CameraControlMode です。</param>
	/// <returns>中心軸表示モードであれば true を返します。</returns>
	private bool IsCenterAxisMode(CameraControlMode mode)
	{
		return mode != CameraControlMode.None;
	}

	#endregion
}



