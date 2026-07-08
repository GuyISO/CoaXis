using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// アタッチされている3Dビューの入力を受け取り、ViewportEventHub へ中継する
/// </summary>
public partial class ViewportInteractionHandler : SubViewport
{
    #region Fields

    [ExportGroup("Settings")]
    [Export] private float _zoomFactor = 1.0f; // ズーム倍率変更時の係数
    [Export] private float _arcballRegionRatio = 0.45f; // 画面サイズに対する、Orbit/Rollの切り替え用の円領域の半径比率
    [Export] private float _moveThreshold = 1.0f; // マウス移動の閾値（この値未満の移動は移動なしとみなす）
    [ExportGroup("Materials")]
    [Export] private Material _defaultMaterial; // 通常表示用のマテリアル（将来の拡張で使用予定）
    [Export] private Material _selectedMaterial; // 選択ハイライト用のマテリアル（将来の拡張で使用予定）

    private ViewportInteractionMode _mode = ViewportInteractionMode.None; // 現在の操作モード
    private Vector2 _lastPosition = Vector2.Zero; // 移動量算出のために前フレームの操作座標を保持
    private Vector2 _startPosition = Vector2.Zero; // 操作開始点の座標を保持
    private bool _hasMoved = false; // ボタンを押してから移動操作したかのフラグ、マウスのクリックと移動の区別に使用
    private Vector2 _screenCenter; // 画面中心座標のキャッシュ
    private float _arcballRadius; // アークボール半径のキャッシュ

    #endregion

    #region Lifecycle

    public override void _Ready()
    {
        // ビューポートサイズの変更を検知してキャッシュを更新するためのシグナル接続
        SizeChanged += OnSizeChanged;

        // イベントの購読
        Application.Instance.Events.Viewport.Hub.NotifyStateRequested += OnNotifyStateRequested;
        Application.Instance.Events.Viewport.Hub.InteractionModeNotified += OnInteractionModeNotified;

        // ビューポートサイズに基づいて、アークボールのパラメータを初期化する
        RefreshArcballParameters();
        Application.Instance.System.Log.Info("ViewportInteractionHandler initialized.");
    }

    public override void _ExitTree()
    {
        // シグナルの切断
        SizeChanged -= OnSizeChanged;

        // イベントの購読解除
        Application.Instance.Events.Viewport.Hub.NotifyStateRequested -= OnNotifyStateRequested;
        Application.Instance.Events.Viewport.Hub.InteractionModeNotified -= OnInteractionModeNotified;

        Application.Instance.System.Log.Info("ViewportInteractionHandler released.");
    }

    public override void _Process(double delta)
    {
        // 入力モードが None のときはマウス移動の検知やカメラ操作の適用を行わず、リソース節約のためここで早期リターンする
        if (_mode == ViewportInteractionMode.None)
        {
            return;
        }

        // マウス位置の変化を検知して、変化があれば操作を適用する
        Vector2 currentPos = GetMousePosition();
        float deltaDistance = currentPos.DistanceTo(_lastPosition);

        // 小数点以下の微小な移動を無視するため、1pixel未満の移動は移動なしとみなす
        if (deltaDistance < _moveThreshold)
        {
            return;
        }

        _hasMoved = true;
        ApplyOperation(_lastPosition, currentPos);

        // 現在のマウス位置を保存して次フレームに備える
        _lastPosition = currentPos;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        // マウスボタンイベント以外は無視する
        if (@event is not InputEventMouseButton button)
        {
            return;
        }

        OnMouseButtonClicked(button);
    }

    #endregion

    #region Events

    /// <summary>
    /// ビューポートサイズ変更時に呼び出されるイベントハンドラ
    /// </summary>
    private void OnSizeChanged()
    {
        RefreshArcballParameters();
    }

    /// <summary>
    /// カメラ関連の状態の通知がリクエストされたときに呼び出されるイベントハンドラ
    /// </summary>
    private void OnNotifyStateRequested()
    {
        Application.Instance.Events.Viewport.NotifyInteractionMode(_mode);
        Application.Instance.Events.Viewport.NotifyArcballRadius(_arcballRadius);
        Application.Instance.Events.Viewport.NotifyArcballHandle(new Vector3(0, 0, 1)); // アークボールハンドルは初期状態では画面正面方向にしておく
    }

    /// <summary>
    /// ViewportEventHub からの InteractionModeNotified シグナルを受け取るイベントハンドラ、操作モードを切り替える
    /// </summary>
    private void OnInteractionModeNotified(ViewportInteractionMode mode)
    {
        SetMode(mode);
    }

    /// <summary>
    /// マウスボタン入力に応じた処理を行う
    /// </summary>
    /// <param name="button">マウスボタン入力イベント</param>
    private void OnMouseButtonClicked(InputEventMouseButton button)
    {
        // 入力モードに応じて、マウス入力の処理を分岐する
        if (_mode == ViewportInteractionMode.None)
        {
            // None モードのときは、カメラ操作開始のトリガーを検知するための処理を行う
            HandleIdleModeInput(button);
        }
        else if (_mode == ViewportInteractionMode.PickRect)
        {
            // PickRectモードのときは矩形選択操作の開始・終了を検知するための処理を行う
            HandlePickModeInput(button);
        }
        else
        {
            // CameraControlモードのときは、カメラ操作の開始・終了を検知するための処理を行う
            HandleCameraControlModeInput(button);
        }
    }

    #endregion

    #region Internal Helpers

    /// <summary>
    /// 操作モードを切り替え、EventHub を通じて変更を通知する
    /// </summary>
    /// <param name="mode">新しい操作モード</param>
    private void SetMode(ViewportInteractionMode mode)
    {
        if (_mode == mode)
        {
            return;
        }

        Application.Instance.System.Log.Debug($"ViewportInteractionHandler: mode changed {_mode} -> {mode}");
        _mode = mode;
        Application.Instance.Events.Viewport.NotifyInteractionMode(mode);
    }

    /// <summary>
    /// 入力モードが None のときのマウス入力を処理する
    /// </summary>
    /// <param name="button">マウスボタン入力イベント</param>
    private void HandleIdleModeInput(InputEventMouseButton button)
    {
        // 中ボタンのクリック開始を検知したら、移動フラグをリセットしてカメラコントロール開始
        if (button.Pressed && button.ButtonIndex == MouseButton.Middle)
        {
            // カメラ操作しているかどうかは、移動量が閾値を超えたかで判定するためここでは移動フラグをリセットして現在位置も更新しておく
            _hasMoved = false;
            _lastPosition = button.Position;
            SetMode(ViewportInteractionMode.CameraPan);
        }
        // 左ボタンのクリック開始を検知したら矩形選択操作を行う
        else if (button.Pressed && button.ButtonIndex == MouseButton.Left)
        {
            // ドラッグしているかどうかは、移動量が閾値を超えたかで判定するためここでは移動フラグをリセットして現在位置も更新しておく
            _hasMoved = false;
            _lastPosition = button.Position;
            // 矩形選択の開始点を保存
            _startPosition = button.Position;
            SetMode(ViewportInteractionMode.PickRect);
            Application.Instance.Events.Viewport.NotifyPickRect(_startPosition, _startPosition); // 選択矩形の初期位置を通知して表示する
        }
        // 右クリックはメニュー表示
        else if (button.Pressed && button.ButtonIndex == MouseButton.Right)
        {
            // TODO: 右クリックの操作は未定義、将来的にメニュー表示予定
        }
    }

    /// <summary>
    /// 入力モードがPickのときのマウス入力を処理する
    /// </summary>
    /// <param name="button">マウスボタン入力イベント</param>
    private void HandlePickModeInput(InputEventMouseButton button)
    {
        // ウィンドウフォーカス喪失などのキャンセル時は即時終了
        if (button.Canceled)
        {
            // 何らかの理由で操作がキャンセルされた場合は、確実にコントロールを終了する
            SetMode(ViewportInteractionMode.None);
            return;
        }

        // 左ボタンのクリック終了を検知したら矩形選択操作を終了
        if (!button.Pressed && button.ButtonIndex == MouseButton.Left)
        {
            if (_hasMoved)
            {
                // ドラッグしていた場合は矩形選択を行う
                PickByRect(_startPosition, button.Position);
            }
            else
            {
                // ドラッグしていない場合は、クリックとみなして単一選択を行う
                PickByPoint(button.Position);
            }

            SetMode(ViewportInteractionMode.None);
        }
    }

    /// <summary>
    /// 入力モードがCameraControlのときのマウス入力を処理する
    /// </summary>
    /// <param name="button">マウスボタン入力イベント</param>
    private void HandleCameraControlModeInput(InputEventMouseButton button)
    {
        // ウィンドウフォーカス喪失などのキャンセル時は即時終了
        if (button.Canceled)
        {
            // 何らかの理由で操作がキャンセルされた場合は、確実にコントロールを終了する
            SetMode(ViewportInteractionMode.None);
            return;
        }

        // 中ボタンのクリック終了を検知したら移動していなければクリックとみなして Focus を行い、カメラコントロールを終了する
        if (!button.Pressed && button.ButtonIndex == MouseButton.Middle)
        {
            if (!_hasMoved)
            {
                if (!TryFocusAt(button.Position))
                {
                    PanCamera(button.Position, _screenCenter); // フォーカスできなかったら、クリック位置にパン扱いとする
                }
            }
            SetMode(ViewportInteractionMode.None);
            return;
        }

        // 左右ボタンの入力を検知したら、Pan → Orbit/Roll または Orbit/Roll → Zoom へ遷移するのでそれ以外は無視する
        if (button.ButtonIndex != MouseButton.Left && button.ButtonIndex != MouseButton.Right)
        {
            return;
        }

        // 中ボタンを押したまま右or左クリック開始を検知したら、位置によってOrbit/Rollモードに切り替え
        if (button.Pressed)
        {
            _hasMoved = true; // クリック操作したら注視点移動しないようににするため、移動フラグを立てる
            SetMode(IsOnArcball(button.Position) ? ViewportInteractionMode.CameraOrbit : ViewportInteractionMode.CameraRoll);
            Vector3 positionOnArcball = GetPositionOnArcballSphere(button.Position);
            Application.Instance.Events.Viewport.NotifyArcballHandle(positionOnArcball);
            return;
        }

        // 中ボタンを押したまま右or左クリック終了を検知したら、Zoomモードに切り替え
        SetMode(ViewportInteractionMode.CameraZoom);
    }

    /// <summary>
    /// 入力モード中の移動量に応じて、EventHub を通じて注視点の移動や回転をリクエストする
    /// </summary>
    /// <param name="previousPos">前フレームの画面上位置</param>
    /// <param name="currentPos">現在の画面上位置</param>
    /// <remarks>
    /// _modeがNoneのときは呼び出されない前提
    /// currentPos と previousPos は画面上の移動量を算出するために使用し、移動がない場合は呼び出されない
    /// </remarks>
    private void ApplyOperation(Vector2 previousPos, Vector2 currentPos)
    {
        switch (_mode)
        {
            case ViewportInteractionMode.CameraPan:
                PanCamera(previousPos, currentPos);
                break;
            case ViewportInteractionMode.CameraOrbit:
                OrbitCamera(previousPos, currentPos);
                break;
            case ViewportInteractionMode.CameraRoll:
                if (IsOnArcball(currentPos))
                {
                    // 画面中央寄りに入ったらOrbitに変更、外周寄りはRollのままにする
                    SetMode(ViewportInteractionMode.CameraOrbit);
                    OrbitCamera(previousPos, currentPos);
                }
                else
                {
                    RollCamera(previousPos, currentPos);
                }
                break;
            case ViewportInteractionMode.CameraZoom:
                ZoomCamera(previousPos, currentPos);
                break;
            case ViewportInteractionMode.PickRect:
                Application.Instance.Events.Viewport.NotifyPickRect(_startPosition, currentPos);
                break;
        }
    }

    /// <summary>
    /// 画面上の指定された位置に注視点を移動する
    /// </summary>
    /// <param name="screenPos">スクリーン座標</param>
    /// <param name="useTween"><see langword="true"/> の場合は補間アニメーションを使用</param>
    /// <returns>注視点を移動できた場合は true、レイキャストがヒットしなかったなどで移動できなかった場合は false を返す</returns>
    private bool TryFocusAt(Vector2 screenPos, bool useTween = false)
    {
        // レイキャストしてヒット情報を取得
        var pickResult = PickUtility.PickByRay(GetCamera3D(), screenPos);

        if (pickResult.HasHit)
        {
            // ヒットしたら注視点を移動
            Application.Instance.System.Log.Debug($"ViewportInteractionHandler: focus target hit. model='{pickResult.Model?.Name}', useTween={useTween}");
            Application.Instance.Events.Viewport.RequestMovePositionTo(pickResult.Position, useTween);
            return true;
        }
        else
        {
            Application.Instance.System.Log.Debug("ViewportInteractionHandler: focus target not found.");
            return false;
        }
    }

    /// <summary>
    /// カメラをパン移動する
    /// </summary>
    /// <param name="fromScreenPos">スクリーン座標の開始位置</param>
    /// <param name="toScreenPos">スクリーン座標の終了位置</param>
    private void PanCamera(Vector2 fromScreenPos, Vector2 toScreenPos)
    {
        // 同一深度平面で、スクリーン座標 from -> to に対応するワールド移動量を求める
        // これにより、画面サイズ・Orthogonal の Size・Perspective の FOV/Z 距離を自動で吸収する
        Camera3D camera = GetCamera3D();
        float panDepth = camera.Position.Z;
        Vector3 fromWorld = camera.ProjectPosition(fromScreenPos, panDepth);
        Vector3 toWorld = camera.ProjectPosition(toScreenPos, panDepth);
        // ドラッグ方向に見た目が追従するよう、差分を逆向きで適用する
        Vector3 move = fromWorld - toWorld;

        Application.Instance.Events.Viewport.RequestTranslate(move, SpaceMode.World);
    }

    /// <summary>
    /// カメラをオービット回転させる
    /// </summary>
    /// <param name="previousPos">前フレームの画面上位置</param>
    /// <param name="currentPos">現在の画面上位置</param>
    private void OrbitCamera(Vector2 previousPos, Vector2 currentPos)
    {
        // 仮想アークボール（アークボール）方式でFocalPointを回転させる
        // Orbit/Roll判定と同じ円を球面半径として使い
        // 2点の球面座標から回転軸・角度を求めてFocalPointのローカル軸で回転する
        // FocalPointの回転はArcballの回転と逆向きになるように計算する
        Vector3 p0 = GetPositionOnArcballSphere(currentPos);
        Vector3 p1 = GetPositionOnArcballSphere(previousPos);
        Quaternion rotation = ComputeArcballRotation(p0, p1);

        Application.Instance.Events.Viewport.RequestRotate(rotation, SpaceMode.FocalPoint);
    }

    /// <summary>
    /// カメラをロール回転させる
    /// </summary>
    /// <param name="previousPos">前フレームの画面上位置</param>
    /// <param name="currentPos">現在の画面上位置</param>
    private void RollCamera(Vector2 previousPos, Vector2 currentPos)
    {
        // 画面中心から見た角度差を使ってロール量を計算する
        // 前フレームと今フレームの画面上位置ベクトル（中心基準）
        // FocalPointの回転はArcballの回転と逆向きになるように計算する
        Vector3 p0 = GetPositionOnArcballEquator(currentPos);
        Vector3 p1 = GetPositionOnArcballEquator(previousPos);
        Quaternion rotation = ComputeArcballRotation(p0, p1);

        Application.Instance.Events.Viewport.RequestRotate(rotation, SpaceMode.FocalPoint);
    }

    /// <summary>
    /// カメラをズームさせる
    /// </summary>
    /// <param name="previousPos">前フレームの画面上位置</param>
    /// <param name="currentPos">現在の画面上位置</param>
    private void ZoomCamera(Vector2 previousPos, Vector2 currentPos)
    {
        float deltaY = (currentPos.Y - previousPos.Y);
        float exponent = deltaY * _zoomFactor;

        Application.Instance.Events.Viewport.RequestZoom(exponent);
    }

    /// <summary>
    /// ビューポートサイズの変更に応じて、アークボールのパラメータを更新する
    /// </summary>
    private void RefreshArcballParameters()
    {
        Rect2 rect = GetVisibleRect();
        _screenCenter = rect.Position + rect.Size * 0.5f;
        _arcballRadius = rect.Size.Y * _arcballRegionRatio;

        Application.Instance.Events.Viewport.NotifyArcballRadius(_arcballRadius);
    }

    /// <summary>
    /// 指定されたスクリーン座標が、アークボールの操作領域（画面中央の円領域）内にあるかどうかを判定する
    /// </summary>
    /// <param name="screenPos">スクリーン座標</param>
    /// <returns>アークボールの操作領域内にある場合は true、それ以外の場合は false を返す</returns>
    private bool IsOnArcball(Vector2 screenPos)
    {
        // Orbit/Roll の分岐用に、画面中央の円領域判定を行う
        return screenPos.DistanceTo(_screenCenter) <= _arcballRadius; // 円形判定
    }

    /// <summary>
    /// スクリーン座標をアークボール球面上の座標に変換する
    /// </summary>
    /// <param name="screenPos">スクリーン座標</param>
    /// <returns>アークボール球面上の3D座標</returns>
    private Vector3 GetPositionOnArcballSphere(Vector2 screenPos)
    {
        // スクリーン座標をアークボール球面上の3D座標へ変換する
        // Y軸はスクリーン下向きを反転して3D上向きに合わせる
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
            // 球の外側は円周で止めず、極角を進めて球の裏側へ回り込ませる
            float len = Mathf.Sqrt(lenSq);
            Vector2 dir = new Vector2(x, y) / len;

            // r=1 で θ=pi/2（赤道）とし、rが増えるほど極角を増やし続ける
            // これにより裏側の極（θ=pi）を超えても球面上を連続的に移動できる
            float theta = Mathf.Pi * 0.5f * len;
            float sinTheta = Mathf.Sin(theta);
            x = dir.X * sinTheta;
            y = dir.Y * sinTheta;
            z = Mathf.Cos(theta);
        }

        return new Vector3(x, y, z);
    }

    /// <summary>
    /// スクリーン座標をアークボールの赤道（z=0平面）に投影する
    /// </summary>
    /// <param name="screenPos">スクリーン座標</param>
    /// <returns>アークボールの赤道上の3D座標を返す</returns>
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
    /// 2点のアークボール球面上の座標から回転軸と回転角を計算する
    /// </summary>
    /// <param name="p0">アークボール上の最初の点</param>
    /// <param name="p1">アークボール上の2番目の点</param>
    /// <returns>アークボール上の点p0からp1への回転を表すクォータニオンを返す</returns>
    /// <remarks>
    /// この関数は p0 と p1 が同一位置またはほぼ同一位置でも安定して回転を計算できるように設計している
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

    /// <summary>
    /// 画面上の指定された位置をクリックして、そこからレイキャストしてヒットしたオブジェクトを選択する
    /// </summary>
    /// <param name="screenPos">スクリーン座標</param>
    private void PickByPoint(Vector2 screenPos)
    {
        var pickResult = PickUtility.PickByRay(GetCamera3D(), screenPos);
        Application.Instance.Events.Pick.NotifyPickResult(pickResult);
    }

    /// <summary>
    /// 画面上の矩形領域をドラッグしてその領域内にあるオブジェクトを選択する
    /// </summary>
    /// <param name="topLeft">矩形の左上座標</param>
    /// <param name="bottomRight">矩形の右下座標</param>
    private void PickByRect(Vector2 topLeft, Vector2 bottomRight)
    {
        // 画面上の矩形領域をカメラの視錐台として、そこに含まれるオブジェクトを選択する
        var frustumShape = CreateFrustumShape(topLeft, bottomRight);
        var camera = GetCamera3D();
        var pickResults = PickUtility.PickByShape(camera, frustumShape, true);
        Application.Instance.Events.Pick.NotifyPickResults(pickResults);
    }

    /// <summary>
    /// カメラの視錐台を表す凸多面体形状を作成する
    /// </summary>
    /// <param name="topLeftPosition">画面上の矩形の左上座標</param>
    /// <param name="bottomRightPosition">画面上の矩形の右下座標</param>
    /// <returns>視錐台を表す凸多面体形状</returns>
    private ConvexPolygonShape3D CreateFrustumShape(Vector2 topLeftPosition, Vector2 bottomRightPosition)
    {
        Camera3D camera = GetCamera3D();

        // 画面上の矩形を正規化（左上・右下を揃える）
        var rect = new Rect2(topLeftPosition, bottomRightPosition - topLeftPosition).Abs();

        // 矩形の4隅（スクリーン座標）
        Vector2 topLeft = rect.Position;
        Vector2 topRight = rect.Position + new Vector2(rect.Size.X, 0);
        Vector2 bottomLeft = rect.Position + new Vector2(0, rect.Size.Y);
        Vector2 bottomRight = rect.Position + rect.Size;

        // near/far の8点を作る
        Vector3[] points = new Vector3[8];

        // near plane（カメラの近距離）
        points[0] = camera.ProjectRayOrigin(topLeft);
        points[1] = camera.ProjectRayOrigin(topRight);
        points[2] = camera.ProjectRayOrigin(bottomRight);
        points[3] = camera.ProjectRayOrigin(bottomLeft);

        // far plane（カメラの遠距離）
        points[4] = points[0] + camera.ProjectRayNormal(topLeft) * camera.Far;
        points[5] = points[1] + camera.ProjectRayNormal(topRight) * camera.Far;
        points[6] = points[2] + camera.ProjectRayNormal(bottomRight) * camera.Far;
        points[7] = points[3] + camera.ProjectRayNormal(bottomLeft) * camera.Far;

        // ConvexPolygonShape3D に詰める
        var shape = new ConvexPolygonShape3D();
        shape.Points = points;

        return shape;
    }

    #endregion
}
