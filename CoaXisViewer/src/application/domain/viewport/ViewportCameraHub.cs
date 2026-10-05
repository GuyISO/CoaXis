using Godot;

/// <summary>
/// カメラ状態を保持し、カメラ操作と状態通知を管理する。
/// </summary>
public partial class ViewportCameraHub : BaseHub
{
    #region Fields

    #endregion

    #region Properties

    /// <summary>カメラ注視点の位置を取得する。</summary>
    public Vector3 Position { get; private set; } = Vector3.Zero;

    /// <summary>カメラ注視点の回転を取得する。</summary>
    public Quaternion Rotation { get; private set; } = Quaternion.Identity;

    /// <summary>カメラ距離を取得する。</summary>
    public float Distance { get; private set; } = 5f;

    /// <summary>正投影サイズを取得する。</summary>
    public float Size { get; private set; } = 1f;

    /// <summary>カメラの視野角を取得する。</summary>
    public float Fov { get; private set; } = 35f;

    /// <summary>カメラ投影方式を取得する。</summary>
    public Camera3D.ProjectionType ProjectionType { get; private set; } = Camera3D.ProjectionType.Perspective;

    /// <summary>カメラのクリップ開始距離を取得する。</summary>
    public float Near { get; private set; } = 0.001f;

    /// <summary>カメラのクリップ終了距離を取得する。</summary>
    public float Far { get; private set; } = 4000f;

    #endregion

    #region Signals

    /// <summary>注視点位置の変更通知。</summary>
    [Signal] public delegate void PositionNotifiedEventHandler();

    /// <summary>注視点回転の変更通知。</summary>
    [Signal] public delegate void RotationNotifiedEventHandler();

    /// <summary>カメラ距離の変更通知。</summary>
    [Signal] public delegate void DistanceNotifiedEventHandler();

    /// <summary>正投影サイズの変更通知。</summary>
    [Signal] public delegate void SizeNotifiedEventHandler();

    /// <summary>視野角の変更通知。</summary>
    [Signal] public delegate void FovNotifiedEventHandler();

    /// <summary>投影方式の変更通知。</summary>
    [Signal] public delegate void ProjectionTypeNotifiedEventHandler();

    /// <summary>クリップ開始距離の通知。</summary>
    [Signal] public delegate void CameraNearNotifiedEventHandler();

    /// <summary>クリップ終了距離の通知。</summary>
    [Signal] public delegate void CameraFarNotifiedEventHandler();

    #endregion

    #region Lifecycle

    /// <summary>
    /// ピック結果通知の購読を開始する。
    /// </summary>
    public override void _Ready()
    {
        SubscribeEvents();
    }

    /// <summary>
    /// ピック結果通知の購読を解除する。
    /// </summary>
    public override void _ExitTree()
    {
        UnsubscribeEvents();

        base._ExitTree();
    }

    #endregion

    #region Events

    /// <summary>
    /// ピック結果通知の購読を開始する。
    /// </summary>
    private void SubscribeEvents()
    {
        Application.Model.Entity.Pick.ResultNotified += OnPickResultNotified;
    }

    /// <summary>
    /// ピック結果通知の購読を解除する。
    /// </summary>
    private void UnsubscribeEvents()
    {
        Application.Model.Entity.Pick.ResultNotified -= OnPickResultNotified;
    }

    /// <summary>
    /// ピック結果の通知を受けたときのイベントハンドラ。
    /// </summary>
    private void OnPickResultNotified(PickResult pickResult)
    {
        if (Application.Model.Entity.Pick.HandlingMode != PickHandlingMode.NormalToFace || !pickResult.HasHit)
        {
            return;
        }

        MovePositionTo(pickResult.Position, true);
        AlignNormalTo(pickResult.Normal, true);
    }

    #endregion

    #region Methods

    /// <summary>
    /// 注視点位置を指定位置へ移動する。
    /// </summary>
    public void MovePositionTo(Vector3 position, bool useTween = false)
    {
        if (useTween)
        {
            TweenPosition(position);
            return;
        }

        UpdatePosition(position);
    }

    /// <summary>
    /// 注視点回転を指定回転へ変更する。
    /// </summary>
    public void MoveRotationTo(Quaternion rotation, bool useTween = false)
    {
        if (useTween)
        {
            TweenRotation(rotation);
            return;
        }

        UpdateRotation(rotation);
    }

    /// <summary>
    /// カメラ距離を設定する。
    /// </summary>
    public void SetDistance(float distance, bool useTween = false)
    {
        if (useTween)
        {
            TweenDistance(distance);
            return;
        }

        UpdateDistance(distance);
    }

    /// <summary>
    /// 正投影サイズを設定する。
    /// </summary>
    public void SetSizeTo(float size, bool useTween = false)
    {
        if (useTween)
        {
            TweenSize(size);
            return;
        }

        UpdateSize(size);
    }

    /// <summary>
    /// 視野角を設定する。
    /// </summary>
    public void SetFov(float fov, bool useTween = false)
    {
        if (useTween)
        {
            TweenFov(fov);
            return;
        }

        UpdateFov(fov);
    }

    /// <summary>
    /// 投影方式を設定する。
    /// </summary>
    public void SetProjectionType(Camera3D.ProjectionType projectionType)
    {
        if (ProjectionType == projectionType)
        {
            return;
        }

        if (projectionType == Camera3D.ProjectionType.Perspective)
        {
            // 投影切替時に見かけのサイズが変わらないよう距離とサイズを相互変換する。
            SetDistance(GetPerspectiveDistanceFromOrthographicSize(), false);
        }
        else
        {
            SetSizeTo(GetOrthographicSizeFromPerspectiveDistance(), false);
            // 正投影は距離に依存しないため、クリップ範囲内にカメラを配置する。
            SetDistance((Near + Far) * 0.5f, false);
        }

        UpdateProjectionType(projectionType);
    }

    /// <summary>
    /// 注視点位置を相対移動する。
    /// </summary>
    public void Translate(Vector3 translation, SpaceMode spaceMode = SpaceMode.World, bool useTween = false)
    {
        if (!TryTranslate(Position, Rotation, translation, spaceMode, out Vector3 position))
        {
            return;
        }

        MovePositionTo(position, useTween);
    }

    /// <summary>
    /// 注視点回転を相対回転する。
    /// </summary>
    public void Rotate(Quaternion rotation, SpaceMode spaceMode = SpaceMode.World, bool useTween = false)
    {
        float cameraDistance = ProjectionType == Camera3D.ProjectionType.Perspective
            ? Distance
            : GetPerspectiveDistance(Size, Fov);
        if (!TryRotate(
            Position,
            Rotation,
            rotation,
            spaceMode,
            cameraDistance,
            out Quaternion targetRotation,
            out Vector3 targetPosition))
        {
            return;
        }

        if (spaceMode == SpaceMode.Camera)
        {
            MovePositionTo(targetPosition, useTween);
        }

        MoveRotationTo(targetRotation, useTween);
    }

    /// <summary>
    /// 指数に応じて距離または正投影サイズを拡縮する。
    /// </summary>
    public void Zoom(float exponent, bool useTween = false)
    {
        CameraSettings settings = Application.Setting.Current.Camera;
        (float distance, float size) = CalculateZoomValues(
            exponent,
            settings.ZoomBase,
            settings.MinZoomValue,
            ProjectionType,
            Distance,
            Size);
        if (ProjectionType == Camera3D.ProjectionType.Orthogonal)
        {
            SetSizeTo(size, useTween);
            return;
        }

        SetDistance(distance, useTween);
    }

    /// <summary>
    /// 透視投影と正投影を切り替える。
    /// </summary>
    public void ToggleProjectionType()
    {
        Camera3D.ProjectionType nextType = ProjectionType == Camera3D.ProjectionType.Perspective
            ? Camera3D.ProjectionType.Orthogonal
            : Camera3D.ProjectionType.Perspective;
        SetProjectionType(nextType);
    }

    /// <summary>
    /// 対象ノード群が収まるようカメラを調整する。
    /// </summary>
    public void Fit(Node3D[] targetNodes, bool useTween = false)
    {
        CameraSettings settings = Application.Setting.Current.Camera;
        if (!WorldAabbUtility.TryGetWorldAabb(targetNodes, out Aabb worldAabb))
        {
            return;
        }

        Viewport targetViewport = null;
        foreach (Node3D targetNode in targetNodes)
        {
            if (targetNode != null && GodotObject.IsInstanceValid(targetNode))
            {
                targetViewport = targetNode.GetViewport();
                break;
            }
        }

        if (targetViewport == null)
        {
            Application.Log.Warn("ViewportCameraHub: fit skipped because target viewport is unavailable.");
            return;
        }

        Vector3 center = worldAabb.Position + worldAabb.Size * 0.5f;
        MovePositionTo(center, useTween);

        // 対象AABBをカメラ座標へ投影するため、対象Nodeが属するSubViewportの比率を使う。
        Rect2 viewportRect = targetViewport.GetVisibleRect();
        float aspect = Mathf.Max(viewportRect.Size.X / Mathf.Max(viewportRect.Size.Y, 1.0f), 0.01f);
        float fitValue = CalculateFitValue(
            worldAabb,
            Rotation,
            ProjectionType,
            Fov,
            aspect,
            Near,
            settings.FitPadding,
            settings.MinZoomValue);

        if (ProjectionType == Camera3D.ProjectionType.Perspective)
        {
            SetDistance(fitValue, useTween);
        }
        else
        {
            SetSizeTo(fitValue, useTween);
        }
    }

    /// <summary>
    /// 指定法線方向からの視点へ揃える。
    /// </summary>
    public void AlignNormalTo(Vector3 normal, bool useTween = false)
    {
        if (!TryAlignNormal(Rotation, normal, out Quaternion targetRotation))
        {
            return;
        }

        MoveRotationTo(targetRotation, useTween);
    }

    #endregion

    #region Helpers

    /// <summary>
    /// 注視点位置を即時に更新して通知する。
    /// </summary>
    private void UpdatePosition(Vector3 value)
    {
        Position = value;
        EmitSignal(SignalName.PositionNotified);
    }

    /// <summary>
    /// 注視点回転を即時に更新して通知する。
    /// </summary>
    private void UpdateRotation(Quaternion value)
    {
        Rotation = new Basis(value).GetRotationQuaternion();
        EmitSignal(SignalName.RotationNotified);
    }

    /// <summary>
    /// カメラ距離を即時に更新して通知する。
    /// </summary>
    private void UpdateDistance(float value)
    {
        Distance = value;
        EmitSignal(SignalName.DistanceNotified);
    }

    /// <summary>
    /// 正投影サイズを即時に更新して通知する。
    /// </summary>
    private void UpdateSize(float value)
    {
        Size = value;
        EmitSignal(SignalName.SizeNotified);
    }

    /// <summary>
    /// 視野角を即時に更新して通知する。
    /// </summary>
    private void UpdateFov(float value)
    {
        Fov = value;
        EmitSignal(SignalName.FovNotified);
    }

    /// <summary>
    /// 投影方式を即時に更新して通知する。
    /// </summary>
    private void UpdateProjectionType(Camera3D.ProjectionType value)
    {
        ProjectionType = value;
        EmitSignal(SignalName.ProjectionTypeNotified);
    }

    /// <summary>
    /// 注視点位置をTweenで更新する。
    /// </summary>
    private void TweenPosition(Vector3 position)
    {
        Vector3 start = Position;
        BuildTween().TweenMethod(Callable.From<float>(t =>
            UpdatePosition(start.Lerp(position, t))), 0f, 1f,
            Application.Setting.Current.Camera.TweenDuration);
    }

    /// <summary>
    /// 注視点回転をTweenで更新する。
    /// </summary>
    private void TweenRotation(Quaternion rotation)
    {
        Quaternion start = Rotation;
        BuildTween().TweenMethod(Callable.From<float>(t =>
            UpdateRotation(start.Slerp(rotation, t))), 0f, 1f,
            Application.Setting.Current.Camera.TweenDuration);
    }

    /// <summary>
    /// カメラ距離をTweenで更新する。
    /// </summary>
    private void TweenDistance(float distance)
    {
        float start = Distance;
        BuildTween().TweenMethod(Callable.From<float>(value =>
            UpdateDistance(value)), start, distance,
            Application.Setting.Current.Camera.TweenDuration);
    }

    /// <summary>
    /// 正投影サイズをTweenで更新する。
    /// </summary>
    private void TweenSize(float size)
    {
        float start = Size;
        BuildTween().TweenMethod(Callable.From<float>(value =>
            UpdateSize(value)), start, size,
            Application.Setting.Current.Camera.TweenDuration);
    }

    /// <summary>
    /// 視野角をTweenで更新する。
    /// </summary>
    private void TweenFov(float fov)
    {
        float start = Fov;
        BuildTween().TweenMethod(Callable.From<float>(value =>
            UpdateFov(value)), start, fov,
            Application.Setting.Current.Camera.TweenDuration);
    }

    /// <summary>
    /// カメラ操作用のTweenを生成する。
    /// </summary>
    private Tween BuildTween()
    {
        return CreateTween()
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
    }

    /// <summary>
    /// 正投影サイズに相当する透視投影の距離を求める。
    /// </summary>
    private float GetPerspectiveDistanceFromOrthographicSize()
    {
        return GetPerspectiveDistance(Size, Fov);
    }

    /// <summary>
    /// 透視投影の距離に相当する正投影サイズを求める。
    /// </summary>
    private float GetOrthographicSizeFromPerspectiveDistance()
    {
        return GetOrthographicSize(Distance, Fov);
    }

    /// <summary>
    /// 指定座標系で平行移動した注視点位置を計算する。
    /// </summary>
    private static bool TryTranslate(
        Vector3 position,
        Quaternion rotation,
        Vector3 translation,
        SpaceMode spaceMode,
        out Vector3 targetPosition)
    {
        targetPosition = spaceMode switch
        {
            SpaceMode.World => position + translation,
            SpaceMode.FocalPoint or SpaceMode.Camera => position + new Basis(rotation) * translation,
            _ => default
        };
        return spaceMode is SpaceMode.World or SpaceMode.FocalPoint or SpaceMode.Camera;
    }

    /// <summary>
    /// 指定座標系で回転した姿勢と、カメラ位置を維持する注視点位置を計算する。
    /// </summary>
    private static bool TryRotate(
        Vector3 position,
        Quaternion currentRotation,
        Quaternion rotation,
        SpaceMode spaceMode,
        float cameraDistance,
        out Quaternion targetRotation,
        out Vector3 targetPosition)
    {
        targetRotation = currentRotation;
        targetPosition = position;
        switch (spaceMode)
        {
            case SpaceMode.World:
                targetRotation = rotation * currentRotation;
                return true;
            case SpaceMode.FocalPoint:
                targetRotation = currentRotation * rotation;
                return true;
            case SpaceMode.Camera:
                targetRotation = currentRotation * rotation;
                Vector3 distance = new Vector3(0, 0, cameraDistance);
                Vector3 cameraPosition = position + currentRotation * distance;
                targetPosition = cameraPosition - targetRotation * distance;
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// 投影方式とズーム指数から距離または正投影サイズを計算する。
    /// </summary>
    private static (float Distance, float Size) CalculateZoomValues(
        float exponent,
        float zoomBase,
        float minZoomValue,
        Camera3D.ProjectionType projectionType,
        float currentDistance,
        float currentSize)
    {
        float scale = Mathf.Pow(zoomBase, exponent);
        return projectionType == Camera3D.ProjectionType.Orthogonal
            ? (currentDistance, Mathf.Max(currentSize * scale, minZoomValue))
            : (Mathf.Max(currentDistance * scale, minZoomValue), currentSize);
    }

    /// <summary>
    /// 正投影サイズから同じ見かけの透視投影距離を計算する。
    /// </summary>
    private static float GetPerspectiveDistance(float orthographicSize, float fov)
    {
        return orthographicSize / CalculateSizeAtZ1(fov);
    }

    /// <summary>
    /// 透視投影距離から同じ見かけの正投影サイズを計算する。
    /// </summary>
    private static float GetOrthographicSize(float perspectiveDistance, float fov)
    {
        return Mathf.Abs(perspectiveDistance) * CalculateSizeAtZ1(fov);
    }

    /// <summary>
    /// 対象AABBが画角内に収まる距離または正投影サイズを計算する。
    /// </summary>
    private static float CalculateFitValue(
        Aabb worldAabb,
        Quaternion cameraRotation,
        Camera3D.ProjectionType projectionType,
        float fov,
        float aspect,
        float near,
        float fitPadding,
        float minZoomValue)
    {
        Vector3 center = worldAabb.Position + worldAabb.Size * 0.5f;
        Basis inverseBasis = new Basis(cameraRotation).Inverse();
        if (projectionType == Camera3D.ProjectionType.Perspective)
        {
            float halfVerticalFov = Mathf.DegToRad(fov) * 0.5f;
            float tanHalfY = Mathf.Max(Mathf.Tan(halfVerticalFov), 1e-5f);
            float tanHalfX = Mathf.Max(tanHalfY * aspect, 1e-5f);
            float maxZ = float.NegativeInfinity;
            float requiredDistance = 0f;

            // AABBの各頂点をカメラ座標へ移し、水平・垂直画角を満たす距離を求める。
            foreach (Vector3 corner in WorldAabbUtility.GetAabbCorners(worldAabb))
            {
                Vector3 local = inverseBasis * (corner - center);
                requiredDistance = Mathf.Max(requiredDistance, local.Z + Mathf.Abs(local.X) / tanHalfX);
                requiredDistance = Mathf.Max(requiredDistance, local.Z + Mathf.Abs(local.Y) / tanHalfY);
                maxZ = Mathf.Max(maxZ, local.Z);
            }

            requiredDistance = Mathf.Max(requiredDistance, maxZ + near * 1.5f);
            return Mathf.Max(requiredDistance * fitPadding, minZoomValue);
        }

        float maxAbsX = 0f;
        float maxAbsY = 0f;
        foreach (Vector3 corner in WorldAabbUtility.GetAabbCorners(worldAabb))
        {
            Vector3 local = inverseBasis * (corner - center);
            maxAbsX = Mathf.Max(maxAbsX, Mathf.Abs(local.X));
            maxAbsY = Mathf.Max(maxAbsY, Mathf.Abs(local.Y));
        }

        float requiredHeight = 2f * Mathf.Max(maxAbsY, maxAbsX / aspect);
        return Mathf.Max(requiredHeight * fitPadding, minZoomValue);
    }

    /// <summary>
    /// 法線方向へ向ける回転を計算し、法線が無効なら false を返す。
    /// </summary>
    private static bool TryAlignNormal(Quaternion currentRotation, Vector3 normal, out Quaternion rotation)
    {
        rotation = currentRotation;
        if (normal.LengthSquared() < Mathf.Epsilon)
        {
            return false;
        }

        Basis currentBasis = new Basis(currentRotation);
        Vector3 targetBack = normal.Normalized();
        Vector3 currentUp = currentBasis.Y.Normalized();
        Vector3 projectedUp = currentUp - targetBack * currentUp.Dot(targetBack);
        if (projectedUp.LengthSquared() < Mathf.Epsilon)
        {
            Vector3 currentRight = currentBasis.X.Normalized();
            Vector3 projectedRight = currentRight - targetBack * currentRight.Dot(targetBack);
            if (projectedRight.LengthSquared() < Mathf.Epsilon)
            {
                // 上方向が法線と平行な場合も姿勢を定義できるよう、別の基準軸を使う。
                projectedRight = Mathf.Abs(targetBack.Dot(Vector3.Up)) < 0.999f
                    ? Vector3.Up.Cross(targetBack)
                    : Vector3.Right.Cross(targetBack);
            }

            projectedUp = targetBack.Cross(projectedRight.Normalized());
        }

        Vector3 up = projectedUp.Normalized();
        Vector3 right = up.Cross(targetBack).Normalized();
        up = targetBack.Cross(right).Normalized();
        rotation = new Basis(right, up, targetBack).GetRotationQuaternion();
        return true;
    }

    /// <summary>
    /// 指定FOVにおけるカメラ距離1あたりの視野サイズを計算する。
    /// </summary>
    private static float CalculateSizeAtZ1(float fov)
    {
        return Mathf.Tan(Mathf.DegToRad(fov) / 2f) * 2f;
    }

    #endregion
}
