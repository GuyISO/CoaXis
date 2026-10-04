// TODO: リファクタリング確認後に削除

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

    #region Methods

    internal void MovePositionTo(Vector3 position, bool useTween = false)
    {
        if (useTween)
        {
            TweenPosition(position);
            return;
        }

        UpdatePosition(position);
    }

    internal void MoveRotationTo(Quaternion rotation, bool useTween = false)
    {
        if (useTween)
        {
            TweenRotation(rotation);
            return;
        }

        UpdateRotation(rotation);
    }

    internal void SetDistance(float distance, bool useTween = false)
    {
        if (useTween)
        {
            TweenDistance(distance);
            return;
        }

        UpdateDistance(distance);
    }

    internal void SetSizeTo(float size, bool useTween = false)
    {
        if (useTween)
        {
            TweenSize(size);
            return;
        }

        UpdateSize(size);
    }

    internal void SetFov(float fov, bool useTween = false)
    {
        if (useTween)
        {
            TweenFov(fov);
            return;
        }

        UpdateFov(fov);
    }

    internal void SetProjectionType(Camera3D.ProjectionType projectionType)
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

    internal void Translate(Vector3 translation, SpaceMode spaceMode = SpaceMode.World, bool useTween = false)
    {
        if (!ViewportCameraUtility.TryTranslate(Position, Rotation, translation, spaceMode, out Vector3 position))
        {
            return;
        }

        MovePositionTo(position, useTween);
    }

    internal void Rotate(Quaternion rotation, SpaceMode spaceMode = SpaceMode.World, bool useTween = false)
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
            MovePositionTo(targetPosition, useTween);
        }

        MoveRotationTo(targetRotation, useTween);
    }

    internal void Zoom(float exponent, bool useTween = false)
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
            SetSizeTo(size, useTween);
            return;
        }

        SetDistance(distance, useTween);
    }

    internal void ToggleProjectionType()
    {
        Camera3D.ProjectionType nextType = ProjectionType == Camera3D.ProjectionType.Perspective
            ? Camera3D.ProjectionType.Orthogonal
            : Camera3D.ProjectionType.Perspective;
        SetProjectionType(nextType);
    }

    internal void Fit(Node3D[] targetNodes, bool useTween = false)
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
            SetDistance(fitValue, useTween);
        }
        else
        {
            SetSizeTo(fitValue, useTween);
        }
    }

    internal void AlignNormalTo(Vector3 normal, bool useTween = false)
    {
        if (!ViewportCameraUtility.TryAlignNormal(Rotation, normal, out Quaternion targetRotation))
        {
            return;
        }

        MoveRotationTo(targetRotation, useTween);
    }

    #endregion

    #region Methods

    internal void UpdatePosition(Vector3 value)
    {
        Position = value;
        EmitSignal(SignalName.PositionNotified);
    }

    internal void UpdateRotation(Quaternion value)
    {
        Rotation = new Basis(value).GetRotationQuaternion();
        EmitSignal(SignalName.RotationNotified);
    }

    internal void UpdateDistance(float value)
    {
        Distance = value;
        EmitSignal(SignalName.DistanceNotified);
    }

    internal void UpdateSize(float value)
    {
        Size = value;
        EmitSignal(SignalName.SizeNotified);
    }

    internal void UpdateFov(float value)
    {
        Fov = value;
        EmitSignal(SignalName.FovNotified);
    }

    internal void UpdateProjectionType(Camera3D.ProjectionType value)
    {
        ProjectionType = value;
        EmitSignal(SignalName.ProjectionTypeNotified);
    }

    #endregion

    #region Helpers

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

    private void OnPickResultNotified(PickResult pickResult)
    {
        if (Application.Model.Entity.Pick.HandlingMode != PickHandlingMode.NormalToFace || !pickResult.HasHit)
        {
            return;
        }

        MovePositionTo(pickResult.Position, true);
        AlignNormalTo(pickResult.Normal, true);
    }

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
