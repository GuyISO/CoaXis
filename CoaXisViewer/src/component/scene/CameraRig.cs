using Godot;

/// <summary>
/// ViewportHubのカメラ状態通知をGodotのCamera3DとNode3Dへ反映する。
/// </summary>
public partial class CameraRig : Node3D
{
    #region Fields

    private Camera3D _camera;

    #endregion

    #region Lifecycle

    /// <summary>
    /// 子カメラを取得してViewport状態通知の購読を開始する。
    /// </summary>
    public override void _Ready()
    {
        EnsureChildNodes();
        SubscribeApplicationEvents();
        InitializeFromHub();
    }

    /// <summary>
    /// Viewport状態通知の購読を解除する。
    /// </summary>
    public override void _ExitTree()
    {
        UnsubscribeApplicationEvents();

        base._ExitTree();
    }

    #endregion

    #region Events

    /// <summary>
    /// シーン上のカメラノードを解決する。
    /// </summary>
    private void EnsureChildNodes()
    {
        _camera = GetNode<Camera3D>("Camera3D");
    }

    /// <summary>
    /// ViewportHubの状態通知を購読する。
    /// </summary>
    private void SubscribeApplicationEvents()
    {
        Application.Viewport.Camera.PositionNotified += OnPositionNotified;
        Application.Viewport.Camera.RotationNotified += OnRotationNotified;
        Application.Viewport.Camera.DistanceNotified += OnDistanceNotified;
        Application.Viewport.Camera.SizeNotified += OnSizeNotified;
        Application.Viewport.Camera.FovNotified += OnFovNotified;
        Application.Viewport.Camera.ProjectionTypeNotified += OnProjectionTypeNotified;
        Application.Viewport.Camera.CameraNearNotified += OnCameraNearNotified;
        Application.Viewport.Camera.CameraFarNotified += OnCameraFarNotified;
        Application.Viewport.Display.LayerActivated += OnLayerNotified;
    }

    /// <summary>
    /// ViewportHubの状態通知購読を解除する。
    /// </summary>
    private void UnsubscribeApplicationEvents()
    {
        Application.Viewport.Camera.PositionNotified -= OnPositionNotified;
        Application.Viewport.Camera.RotationNotified -= OnRotationNotified;
        Application.Viewport.Camera.DistanceNotified -= OnDistanceNotified;
        Application.Viewport.Camera.SizeNotified -= OnSizeNotified;
        Application.Viewport.Camera.FovNotified -= OnFovNotified;
        Application.Viewport.Camera.ProjectionTypeNotified -= OnProjectionTypeNotified;
        Application.Viewport.Camera.CameraNearNotified -= OnCameraNearNotified;
        Application.Viewport.Camera.CameraFarNotified -= OnCameraFarNotified;
        Application.Viewport.Display.LayerActivated -= OnLayerNotified;
    }

    /// <summary>
    /// Hubが保持する現在状態を読み取り、シーン上のカメラへ初期反映する。
    /// </summary>
    private void InitializeFromHub()
    {
        OnPositionNotified(Application.Viewport.Camera.Position);
        OnRotationNotified(Application.Viewport.Camera.Rotation);
        OnDistanceNotified(Application.Viewport.Camera.Distance);
        OnSizeNotified(Application.Viewport.Camera.Size);
        OnFovNotified(Application.Viewport.Camera.Fov);
        OnProjectionTypeNotified(Application.Viewport.Camera.ProjectionType);
        OnCameraNearNotified(Application.Viewport.Camera.Near);
        OnCameraFarNotified(Application.Viewport.Camera.Far);
        _camera.CullMask = Application.Viewport.Display.ActiveLayers;
    }

    private void OnPositionNotified(Vector3 position)
    {
        Transform = new Transform3D(Transform.Basis, position);
    }

    private void OnRotationNotified(Quaternion rotation)
    {
        Transform = new Transform3D(new Basis(rotation), Transform.Origin);
    }

    private void OnDistanceNotified(float distance)
    {
        _camera.Position = new Vector3(0, 0, distance);
    }

    private void OnSizeNotified(float size)
    {
        _camera.Size = size;
    }

    private void OnFovNotified(float fov)
    {
        _camera.Fov = fov;
    }

    private void OnProjectionTypeNotified(Camera3D.ProjectionType projectionType)
    {
        _camera.Projection = projectionType;
    }

    private void OnCameraNearNotified(float near)
    {
        _camera.Near = near;
    }

    private void OnCameraFarNotified(float far)
    {
        _camera.Far = far;
    }

    private void OnLayerNotified(uint layer, bool isActive)
    {
        if (isActive)
        {
            _camera.CullMask |= layer;
        }
        else
        {
            _camera.CullMask &= ~layer;
        }
    }

    #endregion
}
