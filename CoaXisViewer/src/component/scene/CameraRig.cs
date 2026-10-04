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
        Application.Viewport.Display.ActiveLayersNotified += OnActiveLayersNotified;
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
        Application.Viewport.Display.ActiveLayersNotified -= OnActiveLayersNotified;
    }

    /// <summary>
    /// Hubが保持する現在状態を読み取り、シーン上のカメラへ初期反映する。
    /// </summary>
    private void InitializeFromHub()
    {
        OnPositionNotified();
        OnRotationNotified();
        OnDistanceNotified();
        OnSizeNotified();
        OnFovNotified();
        OnProjectionTypeNotified();
        OnCameraNearNotified();
        OnCameraFarNotified();
        OnActiveLayersNotified();
    }

    private void OnPositionNotified()
    {
        Transform = new Transform3D(Transform.Basis, Application.Viewport.Camera.Position);
    }

    private void OnRotationNotified()
    {
        Transform = new Transform3D(new Basis(Application.Viewport.Camera.Rotation), Transform.Origin);
    }

    private void OnDistanceNotified()
    {
        _camera.Position = new Vector3(0, 0, Application.Viewport.Camera.Distance);
    }

    private void OnSizeNotified()
    {
        _camera.Size = Application.Viewport.Camera.Size;
    }

    private void OnFovNotified()
    {
        _camera.Fov = Application.Viewport.Camera.Fov;
    }

    private void OnProjectionTypeNotified()
    {
        _camera.Projection = Application.Viewport.Camera.ProjectionType;
    }

    private void OnCameraNearNotified()
    {
        _camera.Near = Application.Viewport.Camera.Near;
    }

    private void OnCameraFarNotified()
    {
        _camera.Far = Application.Viewport.Camera.Far;
    }

    private void OnActiveLayersNotified()
    {
        _camera.CullMask = Application.Viewport.Display.ActiveLayers;
    }

    #endregion
}
