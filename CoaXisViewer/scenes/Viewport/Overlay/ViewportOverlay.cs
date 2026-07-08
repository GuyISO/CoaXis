using Godot;

/// <summary>
/// 画面中央のナビゲータ（中心軸・アークボール補助表示）を描画する
/// </summary>
/// <remarks>
/// 描画対象のオブジェクトは常に保持しているため描画更新はイベント駆動で行い、ビューポート状態の変化イベントを受け取ったときに表示状態へ反映する
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

    private bool _isInitialized = false; // ビューポートの初期状態を取得してUIに反映するためのフラグ
    private float _arcballRadius = 0.0f; // アークボールの半径
    private Quaternion _arcballHandleRotation = Quaternion.Identity;

    // 関連ノードのキャッシュ
    private Control _centerAxis;
    private Line2D _centerAxisLineXPositive;
    private Line2D _centerAxisLineXNegative;
    private Line2D _centerAxisLineYPositive;
    private Line2D _centerAxisLineYNegative;
    private Line2D _centerAxisLineZ;
    private Control _arcballCross;
    private Line2D _arcballCrossLineX;
    private Line2D _arcballCrossLineY;
    private Control _arcballOutline;
    private Control _selectionRect;
    private Line2D _selectionRectLineHorizontal1;
    private Line2D _selectionRectLineHorizontal2;
    private Line2D _selectionRectLineVertical1;
    private Line2D _selectionRectLineVertical2;

    #endregion

    #region Lifecycle

    public override void _Ready()
    {
        // 関連ノードのキャッシュ
        _centerAxis = GetNode<Control>("CenterAxis");
        _centerAxisLineXPositive = _centerAxis.GetNode<Line2D>("LineXPositive");
        _centerAxisLineXNegative = _centerAxis.GetNode<Line2D>("LineXNegative");
        _centerAxisLineYPositive = _centerAxis.GetNode<Line2D>("LineYPositive");
        _centerAxisLineYNegative = _centerAxis.GetNode<Line2D>("LineYNegative");
        _centerAxisLineZ = _centerAxis.GetNode<Line2D>("LineZ");
        _arcballCross = GetNode<Control>("ArcballCross");
        _arcballCrossLineX = _arcballCross.GetNode<Line2D>("LineX");
        _arcballCrossLineY = _arcballCross.GetNode<Line2D>("LineY");
        _arcballOutline = GetNode<Control>("ArcballOutline");
        _selectionRect = GetNode<Control>("PickRect");
        _selectionRectLineHorizontal1 = _selectionRect.GetNode<Line2D>("LineHorizontal1");
        _selectionRectLineHorizontal2 = _selectionRect.GetNode<Line2D>("LineHorizontal2");
        _selectionRectLineVertical1 = _selectionRect.GetNode<Line2D>("LineVertical1");
        _selectionRectLineVertical2 = _selectionRect.GetNode<Line2D>("LineVertical2");

        // イベントの購読登録
        Application.Events.Viewport.Hub.RotateRequested += OnRotateRequested;
        Application.Events.Viewport.Hub.RotationNotified += OnRotationNotified;
        Application.Events.Viewport.Hub.InteractionModeNotified += OnInteractionModeNotified;
        Application.Events.Viewport.Hub.ArcballRadiusNotified += OnArcballRadiusNotified;
        Application.Events.Viewport.Hub.ArcballHandleNotified += OnArcballHandleNotified;
        Application.Events.Viewport.Hub.PickRectNotified += OnPickRectNotified;
    }

    public override void _ExitTree()
    {
        // イベントの購読解除
        Application.Events.Viewport.Hub.RotateRequested -= OnRotateRequested;
        Application.Events.Viewport.Hub.RotationNotified -= OnRotationNotified;
        Application.Events.Viewport.Hub.InteractionModeNotified -= OnInteractionModeNotified;
        Application.Events.Viewport.Hub.ArcballRadiusNotified -= OnArcballRadiusNotified;
        Application.Events.Viewport.Hub.ArcballHandleNotified -= OnArcballHandleNotified;
        Application.Events.Viewport.Hub.PickRectNotified -= OnPickRectNotified;
    }

    public override void _Process(double delta)
    {
        if (!_isInitialized)
        {
            // カメラの初期状態を取得してUIに反映する
            Application.Events.Viewport.RequestNotifyState();
        }
    }

    #endregion

    #region Events

    /// <summary>
    /// カメラの回転設定がリクエストされたときに呼び出されるイベントハンドラ、アークボールの接線ベクトルを回転に合わせて更新する
    /// </summary>
    /// <param name="rotation">リクエストされた回転</param>
    /// <param name="spaceMode">回転の基準となる座標系</param>
    /// <param name="useTween">回転に補間を使用するかどうかを示すフラグ</param>
    /// <remarks>
    /// Arcball操作は回転結果を拾って処理するのが難しいため、リクエスト量を拾って処理する
    /// </remarks>
    private void OnRotateRequested(Quaternion rotation, SpaceMode spaceMode, bool useTween)
    {
        if (spaceMode == SpaceMode.FocalPoint)
        {
            RotateArcball(rotation);
        }
    }

    /// <summary>
    /// カメラの回転が通知されたときに呼び出されるイベントハンドラ、中心軸の表示を更新する
    /// </summary>
    /// <param name="rotation">通知されたカメラの回転</param>
    private void OnRotationNotified(Quaternion rotation)
    {
        _isInitialized = true;
        DrawCenterAxis(rotation);
    }

    /// <summary>
    /// カメラの入力モードが通知されたときに呼び出されるイベントハンドラ、中心軸とアークボール補助表示の表示を切り替える
    /// </summary>
    /// <param name="mode">通知されたカメラの入力モード</param>
    private void OnInteractionModeNotified(ViewportInteractionMode mode)
    {
        _arcballOutline.Visible = IsArcballMode(mode);
        _centerAxis.Visible = IsCenterAxisMode(mode);
        _arcballCross.Visible = IsArcballMode(mode);
        _selectionRect.Visible = IsPickRectMode(mode);
    }

    /// <summary>
    /// カメラのアークボール操作の半径が通知されたときに呼び出されるイベントハンドラ、アークボールの補助表示を更新する
    /// </summary>
    /// <param name="radius">通知されたアークボールの半径</param>
    private void OnArcballRadiusNotified(float radius)
    {
        _arcballRadius = radius;
        DrawArcballOutline();
    }

    /// <summary>
    /// カメラのアークボール操作のハンドル位置が通知されたときに呼び出されるイベントハンドラ、アークボールの操作点を更新する
    /// </summary>
    /// <param name="position">通知されたアークボールのハンドル位置</param>
    private void OnArcballHandleNotified(Vector3 position)
    {
        ComputeArcballHandleRotation(position);
        DrawArcballCross();
    }

    /// <summary>
    /// 矩形選択の範囲が通知されたときに呼び出されるイベントハンドラで、矩形選択の表示を更新する
    /// </summary>
    /// <param name="startPosition">通知された矩形選択の開始位置</param>
    /// <param name="endPosition">通知された矩形選択の終了位置</param>
    private void OnPickRectNotified(Vector2 startPosition, Vector2 endPosition)
    {
        DrawPickRect(startPosition, endPosition);
    }

    #endregion

    #region Internal Helpers

    /// <summary>
    /// アークボールの回転を更新する回転は現在の回転に乗算されて累積される
    /// </summary>
    /// <param name="rotation">適用する回転</param>
    private void RotateArcball(Quaternion rotation)
    {
        // Overlay はカメラ操作に対して見た目上逆向きに追従させる
        _arcballHandleRotation = rotation.Inverse() * _arcballHandleRotation;
        DrawArcballCross();
    }

    /// <summary>
    /// アークボールの補助表示の円を描画する、円は指定した半径に基づいて構成する
    /// </summary>
    /// <param name="radius">描画する円の半径</param>
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
    /// アークボールのハンドル位置に基づいて、ハンドルの回転を計算して更新する
    /// </summary>
    /// <param name="handlePosition">通知されたアークボールのハンドル位置</param>
    private void ComputeArcballHandleRotation(Vector3 handlePosition)
    {
        if (handlePosition.LengthSquared() <= Mathf.Epsilon * Mathf.Epsilon)
        {
            _arcballHandleRotation = Quaternion.Identity;
            return;
        }

        Vector3 anchor = handlePosition.Normalized();

        // 画面投影で中心方向（-x, -y）を向く接線を作る
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

        // 直交化して安定した姿勢を作る
        Vector3 xAxis = yAxis.Cross(zAxis).Normalized();
        Basis basis = new Basis(xAxis, yAxis, zAxis).Orthonormalized();
        _arcballHandleRotation = basis.GetRotationQuaternion();
    }

    /// <summary>
    /// 中心軸の表示を更新する
    /// </summary>
    /// <param name="rotation">カメラの回転</param>
    private void DrawCenterAxis(Quaternion rotation)
    {
        Basis cameraInverseBasis = new Basis(rotation).Inverse();

        // CATIA風の見た目に合わせて、表示軸と Godot 軸の対応を入れ替える
        // LineX = Godot Z(back), LineY = Godot X(right), LineZ = Godot Y(up)
        Vector2 axisX = ProjectWorldAxisToScreen(cameraInverseBasis * Vector3.Back);
        Vector2 axisY = ProjectWorldAxisToScreen(cameraInverseBasis * Vector3.Right);
        Vector2 axisZ = ProjectWorldAxisToScreen(cameraInverseBasis * Vector3.Up);

        SetSplitAxisLines(_centerAxisLineXPositive, _centerAxisLineXNegative, axisX);
        // CATIA風の見た目に寄せるため、Y の正方向だけ中心ギャップを少し詰める
        SetSplitAxisLines(_centerAxisLineYPositive, _centerAxisLineYNegative, axisY, 0.5f);
        // CATIA風の見た目に寄せるため、Z は負方向だけ短くして前後の奥行き感を強める
        SetLinePoints(_centerAxisLineZ, -axisZ * 0.5f, axisZ);

    }

    /// <summary>
    /// カメラ空間の軸ベクトルを画面空間に投影する、投影後のベクトル長は軸の向きに応じて CenterAxisLength を基準にスケーリングされる
    /// </summary>
    /// <param name="axisInCameraSpace">カメラ空間の軸ベクトル</param>
    /// <returns>画面空間に投影された軸ベクトル</returns>
    private Vector2 ProjectWorldAxisToScreen(Vector3 axisInCameraSpace)
    {
        Vector2 projected = new Vector2(axisInCameraSpace.X, -axisInCameraSpace.Y);

        // 傾きに応じた投影長をそのまま画面上の軸長へ反映する
        return projected * CenterAxisLength;
    }

    /// <summary>
    /// Line2D オブジェクトのポイントを指定した開始点と終了点で設定する、ラインは開始点から終了点へ描画される
    /// </summary>
    /// <param name="line">ポイントを設定する Line2D オブジェクト</param>
    /// <param name="from">ラインの開始点</param>
    /// <param name="to">ラインの終了点</param>
    private static void SetLinePoints(Line2D line, Vector2 from, Vector2 to)
    {
        line.ClearPoints();
        line.AddPoint(from);
        line.AddPoint(to);
    }

    /// <summary>
    /// 中心軸の正負両方のラインを、指定された軸方向に基づいて設定する
    /// </summary>
    /// <param name="positiveLine">正方向のライン</param>
    /// <param name="negativeLine">負方向のライン</param>
    /// <param name="axisDirection">軸の方向を表すベクトル、長さは軸の傾きに応じてスケーリングされる</param>
    /// <param name="positiveGapScale">正方向の中心ギャップをスケーリングするためのパラメータでデフォルトは 1.0f、値を小さくすると正方向の中心ギャップが縮小される</param>
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
    /// アークボールのハンドル位置に基づいて補助表示の十字線を描画する、十字線はハンドル位置を中心にカメラ回転へ追従して回転する
    /// </summary>
    private void DrawArcballCross()
    {
        if (_arcballRadius <= Mathf.Epsilon)
        {
            SetLinePoints(_arcballCrossLineX, Vector2.Zero, Vector2.Zero);
            SetLinePoints(_arcballCrossLineY, Vector2.Zero, Vector2.Zero);
            return;
        }

        // ハンドルの回転を正規化して安定させる、未正規化の回転は回転軸計算を不安定にする可能性がある
        Quaternion normalizedHandleRotation = EnsureNormalizedQuaternion(_arcballHandleRotation);
        Vector3 anchor = (normalizedHandleRotation * Vector3.Forward).Normalized();
        Vector3 tangentX = (normalizedHandleRotation * Vector3.Right).Normalized();
        Vector3 tangentY = (normalizedHandleRotation * Vector3.Up).Normalized();

        // 球面上の接線方向を回転軸へ変換し、短い円弧をサンプリングして描画する
        Vector3 axisX = anchor.Cross(tangentX).Normalized();
        Vector3 axisY = anchor.Cross(tangentY).Normalized();
        SetCurvedArcballLine(_arcballCrossLineX, anchor, axisX, ArcballCrossAngularSize);
        SetCurvedArcballLine(_arcballCrossLineY, anchor, axisY, ArcballCrossAngularSize);
    }

    /// <summary>
    /// Quaternion を単位長に正規化して返す、ゼロ長に近い値は Identity へフォールバックする
    /// </summary>
    /// <param name="rotation">正規化する Quaternion </param>
    /// <returns>正規化済みの Quaternion </returns>
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
    /// アークボール補助表示の十字線の片側ラインを指定した回転軸と角度で描画する、ラインはハンドル位置を中心に指定角度範囲で回転される
    /// </summary>
    /// <param name="line">描画するライン</param>
    /// <param name="anchor">回転の中心点であるアークボールのハンドル位置</param>
    /// <param name="rotationAxis">回転の軸、ゼロベクトルに近い場合は回転せずハンドル位置を中心とした直線になる</param>
    /// <param name="angularSize">回転角度の範囲、ラインは -angularSize から +angularSize の範囲で回転される</param>
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
    /// アークボール球面上の点を画面空間に投影する、投影後の点はアークボール半径に基づいてスケーリングされる
    /// </summary>
    /// <param name="pointOnArcball">アークボール上の点</param>
    /// <returns>画面空間に投影された点</returns>
    private Vector2 ProjectArcballPointToScreen(Vector3 pointOnArcball)
    {
        return new Vector2(pointOnArcball.X, -pointOnArcball.Y) * _arcballRadius;
    }

    /// <summary>
    /// 矩形選択の表示を更新する、矩形は指定した開始位置と終了位置を対角線の端点として描画される
    /// </summary>
    /// <param name="startPosition">矩形選択の開始位置</param>
    /// <param name="endPosition">矩形選択の終了位置</param>
    private void DrawPickRect(Vector2 startPosition, Vector2 endPosition)
    {
        SetLinePoints(_selectionRectLineHorizontal1, new Vector2(startPosition.X, startPosition.Y), new Vector2(endPosition.X, startPosition.Y));
        SetLinePoints(_selectionRectLineHorizontal2, new Vector2(startPosition.X, endPosition.Y), new Vector2(endPosition.X, endPosition.Y));
        SetLinePoints(_selectionRectLineVertical1, new Vector2(startPosition.X, startPosition.Y), new Vector2(startPosition.X, endPosition.Y));
        SetLinePoints(_selectionRectLineVertical2, new Vector2(endPosition.X, startPosition.Y), new Vector2(endPosition.X, endPosition.Y));
    }

    /// <summary>
    /// ViewportInteractionMode がアークボール操作モード（Orbit または Roll）かどうかを判定する
    /// </summary>
    /// <param name="mode">判定する ViewportInteractionMode </param>
    /// <returns>アークボール操作モードであれば true を返す</returns>
    private bool IsArcballMode(ViewportInteractionMode mode)
    {
        return mode == ViewportInteractionMode.CameraOrbit || mode == ViewportInteractionMode.CameraRoll;
    }

    /// <summary>
    /// ViewportInteractionMode が中心軸表示モード（Orbit/Pan/Zoom/Roll）かどうかを判定する
    /// </summary>
    /// <param name="mode">判定する ViewportInteractionMode </param>
    /// <returns>中心軸表示モードであれば true を返す</returns>
    private bool IsCenterAxisMode(ViewportInteractionMode mode)
    {
        return mode == ViewportInteractionMode.CameraOrbit || mode == ViewportInteractionMode.CameraPan || mode == ViewportInteractionMode.CameraZoom || mode == ViewportInteractionMode.CameraRoll;
    }

    /// <summary>
    /// ViewportInteractionMode が選択矩形表示モード（Select）かどうかを判定する
    /// </summary>
    /// <param name="mode">判定する ViewportInteractionMode </param>
    /// <returns>選択矩形表示モードであれば true を返す</returns>
    private bool IsPickRectMode(ViewportInteractionMode mode)
    {
        return mode == ViewportInteractionMode.PickRect;
    }

    #endregion
}



