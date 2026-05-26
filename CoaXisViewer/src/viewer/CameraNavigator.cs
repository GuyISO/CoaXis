using Godot;

public partial class CameraNavigator : Control
{
	private const float CircleDashLength = 16.0f;
	private const float CircleGapLength = 8.0f;
	private const int MinCircleDashCount = 8;
	private const float CenterAxisLength = 32.0f;
	private const float CenterAxisGap = 16.0f;
	private const float TrackballCrossAngularSize = 0.20f;
	private const int TrackballCrossCurveSegments = 12;

	[Export] private CameraController _cameraController;
	[Export] private Color _lineColor = new Color(231f / 255f, 177f / 255f, 246f / 255f);

	private Control _centerAxis;
	private Line2D _axisXPositive;
	private Line2D _axisXNegative;
	private Line2D _axisYPositive;
	private Line2D _axisYNegative;
	private Line2D _axisZ;

	private Control _trackball;
	private Line2D _ballX;
	private Line2D _ballY;
	private bool _hasTrackballAnchor;
	private Vector3 _trackballAnchorWorld = Vector3.Forward;
	private Vector3 _trackballTangentXWorld = Vector3.Right;
	private Vector3 _trackballTangentYWorld = Vector3.Up;
	private CameraController.Mode _currentMode = CameraController.Mode.None;

	private Control _circle;

	public override void _Ready()
	{
		// 外部参照ノードがエディターからセットされていない場合はシーンツリーから取得を試みる
		_cameraController ??= (CameraController)GetNode("/root/Main/CameraController");

		// シーン内の子ノードを取得
		_centerAxis = GetNode<Control>("CenterAxis");
		_axisXPositive = _centerAxis.GetNode<Line2D>("LineXPositive");
		_axisXNegative = _centerAxis.GetNode<Line2D>("LineXNegative");
		_axisYPositive = _centerAxis.GetNode<Line2D>("LineYPositive");
		_axisYNegative = _centerAxis.GetNode<Line2D>("LineYNegative");
		_axisZ = _centerAxis.GetNode<Line2D>("LineZ");
		_trackball = GetNode<Control>("Trackball");
		_ballX = _trackball.GetNode<Line2D>("LineX");
		_ballY = _trackball.GetNode<Line2D>("LineY");
		_circle = GetNode<Control>("Circle");

		_cameraController.ControlModeChanged += OnCameraControlModeChanged;
		_cameraController.ViewPortSizeChanged += OnViewPortSizeChanged;

		DrawCircle();

	}

	public override void _ExitTree()
	{
		_cameraController.ControlModeChanged -= OnCameraControlModeChanged;
		_cameraController.ViewPortSizeChanged -= OnViewPortSizeChanged;
	}
	
	public override void _Process(double delta)
	{

		DrawCenterAxis();
		DrawTrackballCross();
		
	}

	private void OnCameraControlModeChanged(CameraController.Mode mode)
	{
		bool wasTrackballMode = IsTrackballMode(_currentMode);
		bool isTrackballMode = IsTrackballMode(mode);

		if (!wasTrackballMode && isTrackballMode)
		{
			CaptureTrackballAnchor(GetViewport().GetMousePosition());
		}
		else if (!isTrackballMode)
		{
			ClearTrackballCross();
		}

		if (mode == CameraController.Mode.Orbit || mode == CameraController.Mode.Roll)
		{
			_trackball.Visible = true;
			_centerAxis.Visible = true;
			_circle.Visible = true;
		}
		else if (mode == CameraController.Mode.Pan || mode == CameraController.Mode.Zoom)
		{
			_trackball.Visible = false;
			_centerAxis.Visible = true;
			_circle.Visible = false;
		}
		else
		{
			_trackball.Visible = false;
			_centerAxis.Visible = false;
			_circle.Visible = false;
		}

		_currentMode = mode;
	}

	private void OnViewPortSizeChanged()
	{
		DrawCircle();
	}

	private void DrawCircle()
	{
		float radius = _cameraController.TrackballRadius;
		
		float circumference = Mathf.Tau * radius;
		float cycleLength = Mathf.Max(CircleDashLength + CircleGapLength, 1.0f);
		int segmentCount = Mathf.Max(MinCircleDashCount, Mathf.RoundToInt(circumference / cycleLength));

		foreach (Node child in _circle.GetChildren())
		{
			child.QueueFree();
		}

		float step = Mathf.Tau / segmentCount;
		float dashAngle = Mathf.Min(step * 0.95f, Mathf.Tau * (CircleDashLength / Mathf.Max(circumference, 1.0f)));

		for (int index = 0; index < segmentCount; index++)
		{
			float startAngle = index * step;
			float endAngle = startAngle + dashAngle;

			Vector2 start = new Vector2(Mathf.Cos(startAngle), Mathf.Sin(startAngle)) * radius;
			Vector2 end = new Vector2(Mathf.Cos(endAngle), Mathf.Sin(endAngle)) * radius;

			Line2D segment = new Line2D
			{
				Name = $"CircleDash{index}",
				Width = 1.0f,
				DefaultColor = _lineColor,
				Antialiased = true
			};

			segment.AddPoint(start);
			segment.AddPoint(end);
			_circle.AddChild(segment);
		}
	}

	private void DrawCenterAxis()
	{
		if (_cameraController == null || _cameraController.Camera == null)
		{
			return;
		}

		Basis cameraInverseBasis = _cameraController.Camera.GlobalTransform.Basis.Inverse();

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

	private Vector2 ProjectWorldAxisToScreen(Vector3 axisInCameraSpace)
	{
		Vector2 projected = new Vector2(axisInCameraSpace.X, -axisInCameraSpace.Y);
		if (projected.LengthSquared() < 1e-6f)
		{
			return Vector2.Zero;
		}

		// 傾きに応じた投影長をそのまま画面上の軸長へ反映する。
		return projected * CenterAxisLength;
	}

	private static void SetLinePoints(Line2D line, Vector2 from, Vector2 to)
	{
		line.ClearPoints();
		line.AddPoint(from);
		line.AddPoint(to);
	}

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

	private void DrawTrackballCross()
	{
		if (!_hasTrackballAnchor || _cameraController == null || _cameraController.Camera == null)
		{
			SetLinePoints(_ballX, Vector2.Zero, Vector2.Zero);
			SetLinePoints(_ballY, Vector2.Zero, Vector2.Zero);
			return;
		}

		Basis cameraInverseBasis = _cameraController.Camera.GlobalTransform.Basis.Inverse();
		Vector3 anchorCamera = (cameraInverseBasis * _trackballAnchorWorld).Normalized();

		Vector3 tangentXCamera = (cameraInverseBasis * _trackballTangentXWorld).Normalized();
		Vector3 tangentYCamera = (cameraInverseBasis * _trackballTangentYWorld).Normalized();

		float depthScale = Mathf.Lerp(0.5f, 1.0f, Mathf.Clamp(Mathf.Abs(anchorCamera.Z), 0.0f, 1.0f));
		float angularSize = TrackballCrossAngularSize * depthScale;

		Vector3 axisX = anchorCamera.Cross(tangentXCamera).Normalized();
		Vector3 axisY = anchorCamera.Cross(tangentYCamera).Normalized();

		SetCurvedTrackballLine(_ballX, anchorCamera, axisX, angularSize);
		SetCurvedTrackballLine(_ballY, anchorCamera, axisY, angularSize);

	}

	private bool IsTrackballMode(CameraController.Mode mode)
	{
		return mode == CameraController.Mode.Orbit || mode == CameraController.Mode.Roll;
	}

	private void CaptureTrackballAnchor(Vector2 mousePos)
	{
		if (_cameraController == null || _cameraController.Camera == null)
		{
			ClearTrackballCross();
			return;
		}

		Vector3 anchorCamera = ProjectOntoTrackball(mousePos);
		Basis cameraBasis = _cameraController.Camera.GlobalTransform.Basis;

		_trackballAnchorWorld = (cameraBasis * anchorCamera).Normalized();

		Vector2 screenPos = new Vector2(anchorCamera.X, -anchorCamera.Y);
		Vector2 toCenterScreen = -screenPos;

		Vector3 tangentXCamera;
		if (toCenterScreen.LengthSquared() < 1e-6f)
		{
			tangentXCamera = Vector3.Right - anchorCamera * anchorCamera.Dot(Vector3.Right);
		}
		else
		{
			Vector2 toCenterDir = toCenterScreen.Normalized();
			Vector3 desired = new Vector3(toCenterDir.X, -toCenterDir.Y, 0.0f);
			tangentXCamera = desired - anchorCamera * desired.Dot(anchorCamera);
		}

		if (tangentXCamera.LengthSquared() < 1e-6f)
		{
			tangentXCamera = anchorCamera.Cross(Vector3.Up);
			if (tangentXCamera.LengthSquared() < 1e-6f)
			{
				tangentXCamera = anchorCamera.Cross(Vector3.Right);
			}
		}

		Vector3 tangentYCamera = anchorCamera.Cross(tangentXCamera).Normalized();
		tangentXCamera = tangentXCamera.Normalized();

		_trackballTangentXWorld = (cameraBasis * tangentXCamera).Normalized();
		_trackballTangentYWorld = (cameraBasis * tangentYCamera).Normalized();
		_hasTrackballAnchor = true;
	}

	private void SetCurvedTrackballLine(Line2D line, Vector3 anchorCamera, Vector3 rotationAxis, float angularSize)
	{
		line.ClearPoints();

		if (rotationAxis.LengthSquared() < 1e-6f)
		{
			line.AddPoint(ProjectTrackballPointToScreen(anchorCamera));
			return;
		}

		for (int i = 0; i <= TrackballCrossCurveSegments; i++)
		{
			float t = (float)i / TrackballCrossCurveSegments;
			float angle = Mathf.Lerp(-angularSize, angularSize, t);
			Vector3 point = anchorCamera.Rotated(rotationAxis, angle).Normalized();
			line.AddPoint(ProjectTrackballPointToScreen(point));
		}
	}

	private void ClearTrackballCross()
	{
		_hasTrackballAnchor = false;
		SetLinePoints(_ballX, Vector2.Zero, Vector2.Zero);
		SetLinePoints(_ballY, Vector2.Zero, Vector2.Zero);
	}

	private Vector2 ProjectTrackballPointToScreen(Vector3 pointOnTrackball)
	{
		return new Vector2(pointOnTrackball.X, -pointOnTrackball.Y) * _cameraController.TrackballRadius;
	}

	private Vector3 ProjectOntoTrackball(Vector2 screenPos)
	{
		float x = (screenPos.X - _cameraController.ScreenCenter.X) / _cameraController.TrackballRadius;
		float y = -(screenPos.Y - _cameraController.ScreenCenter.Y) / _cameraController.TrackballRadius;

		float lenSq = x * x + y * y;
		if (lenSq <= 1.0f)
		{
			return new Vector3(x, y, Mathf.Sqrt(1.0f - lenSq));
		}

		float len = Mathf.Sqrt(lenSq);
		Vector2 dir = new Vector2(x, y) / len;
		float theta = Mathf.Pi * 0.5f * len;
		float sinTheta = Mathf.Sin(theta);

		return new Vector3(dir.X * sinTheta, dir.Y * sinTheta, Mathf.Cos(theta));
	}
}
