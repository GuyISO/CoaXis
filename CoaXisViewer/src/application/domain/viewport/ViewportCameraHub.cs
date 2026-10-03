// TODO: リファクタリング確認後に削除

using Godot;

/// <summary>
/// カメラ状態を保持し、操作要求と状態通知を管理する。
/// </summary>
public partial class ViewportCameraHub : BaseHub
{
    #region Fields

    #endregion

    #region Properties

    /// <summary>カメラ注視点の位置を取得する。</summary>
    internal Vector3 Position { get; private set; } = Vector3.Zero;

    /// <summary>カメラ注視点の回転を取得する。</summary>
    internal Quaternion Rotation { get; private set; } = Quaternion.Identity;

    /// <summary>カメラ距離を取得する。</summary>
    internal float Distance { get; private set; } = 5f;

    /// <summary>正投影サイズを取得する。</summary>
    internal float Size { get; private set; } = 1f;

    /// <summary>カメラの視野角を取得する。</summary>
    internal float Fov { get; private set; } = 35f;

    /// <summary>カメラ投影方式を取得する。</summary>
    internal Camera3D.ProjectionType ProjectionType { get; private set; } = Camera3D.ProjectionType.Perspective;

    /// <summary>カメラのクリップ開始距離を取得する。</summary>
    internal float Near { get; private set; } = 0.001f;

    /// <summary>カメラのクリップ終了距離を取得する。</summary>
    internal float Far { get; private set; } = 4000f;

    #endregion

    #region Lifecycle

    /// <summary>
    /// カメラ操作要求の購読を開始する。
    /// </summary>
    public override void _Ready()
    {
        SubscribeEvents();
    }

    /// <summary>
    /// カメラ操作要求の購読を解除する。
    /// </summary>
    public override void _ExitTree()
    {
        UnsubscribeEvents();

        base._ExitTree();
    }

    #endregion

    #region Events

    /// <summary>
    /// カメラ操作要求の購読を開始する。
    /// </summary>
    private void SubscribeEvents()
    {
        Application.Pick.ResultNotified += OnPickResultNotified;
        MovePositionToRequested += OnMovePositionToRequested;
        MoveRotationToRequested += OnMoveRotationToRequested;
        SetDistanceRequested += OnSetDistanceRequested;
        SetSizeRequested += OnSetSizeRequested;
        SetFovRequested += OnSetFovRequested;
        SetProjectionTypeRequested += OnSetProjectionTypeRequested;
        TranslateRequested += OnTranslateRequested;
        RotateRequested += OnRotateRequested;
        ZoomRequested += OnZoomRequested;
        ToggleProjectionTypeRequested += OnToggleProjectionTypeRequested;
        FitRequested += OnFitRequested;
        AlignNormalToRequested += OnAlignNormalToRequested;
    }

    /// <summary>
    /// カメラ操作要求の購読を解除する。
    /// </summary>
    private void UnsubscribeEvents()
    {
        Application.Pick.ResultNotified -= OnPickResultNotified;
        MovePositionToRequested -= OnMovePositionToRequested;
        MoveRotationToRequested -= OnMoveRotationToRequested;
        SetDistanceRequested -= OnSetDistanceRequested;
        SetSizeRequested -= OnSetSizeRequested;
        SetFovRequested -= OnSetFovRequested;
        SetProjectionTypeRequested -= OnSetProjectionTypeRequested;
        TranslateRequested -= OnTranslateRequested;
        RotateRequested -= OnRotateRequested;
        ZoomRequested -= OnZoomRequested;
        ToggleProjectionTypeRequested -= OnToggleProjectionTypeRequested;
        FitRequested -= OnFitRequested;
        AlignNormalToRequested -= OnAlignNormalToRequested;
    }

    private void OnPickResultNotified(PickResult pickResult)
    {
        if (Application.Pick.HandlingMode != PickHandlingMode.NormalToFace || !pickResult.HasHit)
        {
            return;
        }

        MovePositionTo(pickResult.Position, true);
        ApplyAlignNormalToRequest(pickResult.Normal, true);
    }

    private void OnMovePositionToRequested(Vector3 position, bool useTween)
    {
        ApplyPosition(position, useTween);
    }

    private void OnMoveRotationToRequested(Quaternion rotation, bool useTween)
    {
        ApplyRotation(rotation, useTween);
    }

    private void OnSetDistanceRequested(float distance, bool useTween)
    {
        ApplyDistance(distance, useTween);
    }

    private void OnSetSizeRequested(float size, bool useTween)
    {
        ApplySize(size, useTween);
    }

    private void OnSetFovRequested(float fov, bool useTween)
    {
        ApplyFov(fov, useTween);
    }

    private void OnSetProjectionTypeRequested(Camera3D.ProjectionType projectionType)
    {
        ApplyProjectionType(projectionType);
    }

    private void OnTranslateRequested(Vector3 translation, SpaceMode spaceMode, bool useTween)
    {
        ApplyTranslationRequest(translation, spaceMode, useTween);
    }

    private void OnRotateRequested(Quaternion rotation, SpaceMode spaceMode, bool useTween)
    {
        ApplyRotationRequest(rotation, spaceMode, useTween);
    }

    private void OnZoomRequested(float exponent, bool useTween)
    {
        ApplyZoomRequest(exponent, useTween);
    }

    private void OnToggleProjectionTypeRequested()
    {
        Camera3D.ProjectionType nextType = ProjectionType == Camera3D.ProjectionType.Perspective
            ? Camera3D.ProjectionType.Orthogonal
            : Camera3D.ProjectionType.Perspective;
        ApplyProjectionType(nextType);
    }

    private void OnFitRequested(Node3D[] targetNodes, bool useTween)
    {
        ApplyFitRequest(targetNodes, useTween);
    }

    private void OnAlignNormalToRequested(Vector3 normal, bool useTween)
    {
        ApplyAlignNormalToRequest(normal, useTween);
    }

    #endregion

    #region Actions

    /// <summary>注視点位置の移動要求。</summary>
    /// <param name="position">移動先位置</param>
    /// <param name="useTween">補間を使用する場合は true</param>
    [Signal] public delegate void MovePositionToRequestedEventHandler(Vector3 position, bool useTween);
    /// <summary>注視点回転の変更要求。</summary>
    /// <param name="rotation">目標回転</param>
    /// <param name="useTween">補間を使用する場合は true</param>
    [Signal] public delegate void MoveRotationToRequestedEventHandler(Quaternion rotation, bool useTween);
    /// <summary>カメラ距離の変更要求。</summary>
    /// <param name="distance">目標距離</param>
    /// <param name="useTween">補間を使用する場合は true</param>
    [Signal] public delegate void SetDistanceRequestedEventHandler(float distance, bool useTween);
    /// <summary>正投影サイズの変更要求。</summary>
    /// <param name="size">目標サイズ</param>
    /// <param name="useTween">補間を使用する場合は true</param>
    [Signal] public delegate void SetSizeRequestedEventHandler(float size, bool useTween);
    /// <summary>視野角の変更要求。</summary>
    /// <param name="fov">目標視野角</param>
    /// <param name="useTween">補間を使用する場合は true</param>
    [Signal] public delegate void SetFovRequestedEventHandler(float fov, bool useTween);
    /// <summary>投影方式の変更要求。</summary>
    /// <param name="projectionType">目標投影方式</param>
    [Signal] public delegate void SetProjectionTypeRequestedEventHandler(Camera3D.ProjectionType projectionType);
    /// <summary>指定座標系での平行移動要求。</summary>
    /// <param name="translation">移動量</param>
    /// <param name="spaceMode">移動基準座標系</param>
    /// <param name="useTween">補間を使用する場合は true</param>
    [Signal] public delegate void TranslateRequestedEventHandler(Vector3 translation, SpaceMode spaceMode, bool useTween);
    /// <summary>指定座標系での回転要求。</summary>
    /// <param name="rotation">回転量</param>
    /// <param name="spaceMode">回転基準座標系</param>
    /// <param name="useTween">補間を使用する場合は true</param>
    [Signal] public delegate void RotateRequestedEventHandler(Quaternion rotation, SpaceMode spaceMode, bool useTween);
    /// <summary>カメラズーム要求。</summary>
    /// <param name="exponent">ズーム指数</param>
    /// <param name="useTween">補間を使用する場合は true</param>
    [Signal] public delegate void ZoomRequestedEventHandler(float exponent, bool useTween);
    /// <summary>投影方式の切替要求。</summary>
    [Signal] public delegate void ToggleProjectionTypeRequestedEventHandler();
    /// <summary>指定Node群を画角に収める要求。</summary>
    /// <param name="targetNodes">対象Node群</param>
    /// <param name="useTween">補間を使用する場合は true</param>
    [Signal] public delegate void FitRequestedEventHandler(Node3D[] targetNodes, bool useTween);
    /// <summary>指定法線へカメラを整列する要求。</summary>
    /// <param name="normal">整列対象の法線</param>
    /// <param name="useTween">補間を使用する場合は true</param>
    [Signal] public delegate void AlignNormalToRequestedEventHandler(Vector3 normal, bool useTween);

    internal void MovePositionTo(Vector3 position, bool useTween = false) =>
        EmitSignal(SignalName.MovePositionToRequested, position, useTween);
    internal void MoveRotationTo(Quaternion rotation, bool useTween = false) =>
        EmitSignal(SignalName.MoveRotationToRequested, rotation, useTween);
    internal void SetDistance(float distance, bool useTween = false) =>
        EmitSignal(SignalName.SetDistanceRequested, distance, useTween);
    internal void SetSizeTo(float size, bool useTween = false) =>
        EmitSignal(SignalName.SetSizeRequested, size, useTween);
    internal void SetFov(float fov, bool useTween = false) =>
        EmitSignal(SignalName.SetFovRequested, fov, useTween);
    internal void SetProjectionType(Camera3D.ProjectionType projectionType) =>
        EmitSignal(SignalName.SetProjectionTypeRequested, (int)projectionType);
    internal void Translate(Vector3 translation, SpaceMode spaceMode = SpaceMode.World, bool useTween = false) =>
        EmitSignal(SignalName.TranslateRequested, translation, (int)spaceMode, useTween);
    internal void Rotate(Quaternion rotation, SpaceMode spaceMode = SpaceMode.World, bool useTween = false) =>
        EmitSignal(SignalName.RotateRequested, rotation, (int)spaceMode, useTween);
    internal void Zoom(float exponent, bool useTween = false) =>
        EmitSignal(SignalName.ZoomRequested, exponent, useTween);
    internal void ToggleProjectionType() =>
        EmitSignal(SignalName.ToggleProjectionTypeRequested);
    internal void Fit(Node3D[] targetNodes, bool useTween = false) =>
        EmitSignal(SignalName.FitRequested, targetNodes, useTween);
    internal void AlignNormalTo(Vector3 normal, bool useTween = false) =>
        EmitSignal(SignalName.AlignNormalToRequested, normal, useTween);

    #endregion

    #region Notifications

    /// <summary>注視点位置の変更通知。</summary>
    /// <param name="position">現在位置</param>
    [Signal] public delegate void PositionNotifiedEventHandler(Vector3 position);
    /// <summary>注視点回転の変更通知。</summary>
    /// <param name="rotation">現在回転</param>
    [Signal] public delegate void RotationNotifiedEventHandler(Quaternion rotation);
    /// <summary>カメラ距離の変更通知。</summary>
    /// <param name="distance">現在距離</param>
    [Signal] public delegate void DistanceNotifiedEventHandler(float distance);
    /// <summary>正投影サイズの変更通知。</summary>
    /// <param name="size">現在サイズ</param>
    [Signal] public delegate void SizeNotifiedEventHandler(float size);
    /// <summary>視野角の変更通知。</summary>
    /// <param name="fov">現在視野角</param>
    [Signal] public delegate void FovNotifiedEventHandler(float fov);
    /// <summary>投影方式の変更通知。</summary>
    /// <param name="projectionType">現在の投影方式</param>
    [Signal] public delegate void ProjectionTypeNotifiedEventHandler(Camera3D.ProjectionType projectionType);
    /// <summary>クリップ開始距離の通知。</summary>
    /// <param name="near">開始距離</param>
    [Signal] public delegate void CameraNearNotifiedEventHandler(float near);
    /// <summary>クリップ終了距離の通知。</summary>
    /// <param name="far">終了距離</param>
    [Signal] public delegate void CameraFarNotifiedEventHandler(float far);

    internal void UpdatePosition(Vector3 value)
    {
        Position = value;
        EmitSignal(SignalName.PositionNotified, value);
    }

    internal void UpdateRotation(Quaternion value)
    {
        Rotation = new Basis(value).GetRotationQuaternion();
        EmitSignal(SignalName.RotationNotified, Rotation);
    }

    internal void UpdateDistance(float value)
    {
        Distance = value;
        EmitSignal(SignalName.DistanceNotified, value);
    }

    internal void UpdateSize(float value)
    {
        Size = value;
        EmitSignal(SignalName.SizeNotified, value);
    }

    internal void UpdateFov(float value)
    {
        Fov = value;
        EmitSignal(SignalName.FovNotified, value);
    }

    internal void UpdateProjectionType(Camera3D.ProjectionType value)
    {
        ProjectionType = value;
        EmitSignal(SignalName.ProjectionTypeNotified, (int)value);
    }

    #endregion

    #region Methods

    private void ApplyPosition(Vector3 position, bool useTween)
    {
        if (useTween)
        {
            TweenPosition(position);
            return;
        }

        UpdatePosition(position);
    }

    private void ApplyRotation(Quaternion rotation, bool useTween)
    {
        if (useTween)
        {
            TweenRotation(rotation);
            return;
        }

        UpdateRotation(rotation);
    }

    private void ApplyDistance(float distance, bool useTween)
    {
        if (useTween)
        {
            TweenDistance(distance);
            return;
        }

        UpdateDistance(distance);
    }

    private void ApplySize(float size, bool useTween)
    {
        if (useTween)
        {
            TweenSize(size);
            return;
        }

        UpdateSize(size);
    }

    private void ApplyFov(float fov, bool useTween)
    {
        if (useTween)
        {
            TweenFov(fov);
            return;
        }

        UpdateFov(fov);
    }

    private void ApplyProjectionType(Camera3D.ProjectionType projectionType)
    {
        if (ProjectionType == projectionType)
        {
            return;
        }

        if (projectionType == Camera3D.ProjectionType.Perspective)
        {
            // 投影切替時に見かけのサイズが変わらないよう距離とサイズを相互変換する。
            ApplyDistance(GetPerspectiveDistanceFromOrthographicSize(), false);
        }
        else
        {
            ApplySize(GetOrthographicSizeFromPerspectiveDistance(), false);
            // 正投影は距離に依存しないため、クリップ範囲内にカメラを配置する。
            ApplyDistance((Near + Far) * 0.5f, false);
        }

        UpdateProjectionType(projectionType);
    }

    private void ApplyTranslationRequest(Vector3 translation, SpaceMode spaceMode, bool useTween)
    {
        if (!ViewportCameraUtility.TryTranslate(Position, Rotation, translation, spaceMode, out Vector3 position))
        {
            return;
        }

        ApplyPosition(position, useTween);
    }

    private void ApplyRotationRequest(Quaternion rotation, SpaceMode spaceMode, bool useTween)
    {
        float cameraDistance = ProjectionType == Camera3D.ProjectionType.Perspective
            ? Distance
            : ViewportCameraUtility.GetPerspectiveDistance(Size, Fov);
        if (!ViewportCameraUtility.TryRotate(
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
            ApplyPosition(targetPosition, useTween);
        }

        ApplyRotation(targetRotation, useTween);
    }

    private void ApplyZoomRequest(float exponent, bool useTween)
    {
        CameraSettings settings = Application.Setting.Current.Camera;
        (float distance, float size) = ViewportCameraUtility.Zoom(
            exponent,
            settings.ZoomBase,
            settings.MinZoomValue,
            ProjectionType,
            Distance,
            Size);
        if (ProjectionType == Camera3D.ProjectionType.Orthogonal)
        {
            ApplySize(size, useTween);
            return;
        }

        ApplyDistance(distance, useTween);
    }

    private void TweenPosition(Vector3 position)
    {
        Vector3 start = Position;
        BuildTween().TweenMethod(Callable.From<float>(t =>
            UpdatePosition(start.Lerp(position, t))), 0f, 1f,
            Application.Setting.Current.Camera.TweenDuration);
    }

    private void TweenRotation(Quaternion rotation)
    {
        Quaternion start = Rotation;
        BuildTween().TweenMethod(Callable.From<float>(t =>
            UpdateRotation(start.Slerp(rotation, t))), 0f, 1f,
            Application.Setting.Current.Camera.TweenDuration);
    }

    private void TweenDistance(float distance)
    {
        float start = Distance;
        BuildTween().TweenMethod(Callable.From<float>(value =>
            UpdateDistance(value)), start, distance,
            Application.Setting.Current.Camera.TweenDuration);
    }

    private void TweenSize(float size)
    {
        float start = Size;
        BuildTween().TweenMethod(Callable.From<float>(value =>
            UpdateSize(value)), start, size,
            Application.Setting.Current.Camera.TweenDuration);
    }

    private void TweenFov(float fov)
    {
        float start = Fov;
        BuildTween().TweenMethod(Callable.From<float>(value =>
            UpdateFov(value)), start, fov,
            Application.Setting.Current.Camera.TweenDuration);
    }

    private void ApplyFitRequest(Node3D[] targetNodes, bool useTween)
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
        ApplyPosition(center, useTween);

        // 対象AABBをカメラ座標へ投影するため、対象Nodeが属するSubViewportの比率を使う。
        Basis inverseBasis = new Basis(Rotation).Inverse();
        Rect2 viewportRect = targetViewport.GetVisibleRect();
        float aspect = Mathf.Max(viewportRect.Size.X / Mathf.Max(viewportRect.Size.Y, 1.0f), 0.01f);
        float minZoomValue = settings.MinZoomValue;

        float fitValue = ViewportCameraUtility.CalculateFitValue(
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
            ApplyDistance(fitValue, useTween);
        }
        else
        {
            ApplySize(fitValue, useTween);
        }
    }

    private void ApplyAlignNormalToRequest(Vector3 normal, bool useTween)
    {
        if (!ViewportCameraUtility.TryAlignNormal(Rotation, normal, out Quaternion targetRotation))
        {
            return;
        }

        ApplyRotation(targetRotation, useTween);
    }

    #endregion

    #region Helpers

    private Tween BuildTween()
    {
        return CreateTween()
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
    }

    private float GetPerspectiveDistanceFromOrthographicSize()
    {
        return ViewportCameraUtility.GetPerspectiveDistance(Size, Fov);
    }

    private float GetOrthographicSizeFromPerspectiveDistance()
    {
        return ViewportCameraUtility.GetOrthographicSize(Distance, Fov);
    }

    #endregion
}
