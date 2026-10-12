using Godot;

/// <summary>
/// ModelPropertyの対象位置をビューポート上のラベルとして表示する。
/// </summary>
public partial class ViewportPropertyOverlay : Control
{
    #region Fields

    private Camera3D _camera = null!;
    private Label _label = null!;

    #endregion

    #region Properties

    /// <summary>
    /// 表示対象のCATIA座標を取得または設定する。
    /// </summary>
    [Export]
    public Vector3 TargetPositionCatia { get; set; } = Vector3.Zero;

    #endregion

    #region Lifecycle

    /// <summary>
    /// 子ノードを解決し、カメラ状態と表示サイズの変更を購読する。
    /// </summary>
    public override void _Ready()
    {
        EnsureChildNodes();
        SubscribeEvents();
        UpdateLabelPosition();
    }

    /// <summary>
    /// 購読中のイベントを解除する。
    /// </summary>
    public override void _ExitTree()
    {
        UnsubscribeEvents();

        base._ExitTree();
    }

    #endregion

    #region Events

    /// <summary>
    /// 子ノードを解決する。
    /// </summary>
    private void EnsureChildNodes()
    {
        _camera = GetNode<Camera3D>("../FocalPoint/Camera3D");
        _label = GetNode<Label>("Label");
    }

    /// <summary>
    /// 表示位置の再計算が必要なイベントの購読を開始する。
    /// </summary>
    private void SubscribeEvents()
    {
        Resized += OnResized;
        Application.Viewport.Camera.PositionNotified += OnCameraChanged;
        Application.Viewport.Camera.RotationNotified += OnCameraChanged;
        Application.Viewport.Camera.DistanceNotified += OnCameraChanged;
        Application.Viewport.Camera.SizeNotified += OnCameraChanged;
        Application.Viewport.Camera.FovNotified += OnCameraChanged;
        Application.Viewport.Camera.ProjectionTypeNotified += OnCameraChanged;
        Application.Viewport.Camera.CameraNearNotified += OnCameraChanged;
        Application.Viewport.Camera.CameraFarNotified += OnCameraChanged;
    }

    /// <summary>
    /// 表示位置の再計算が必要なイベントの購読を解除する。
    /// </summary>
    private void UnsubscribeEvents()
    {
        Resized -= OnResized;
        Application.Viewport.Camera.PositionNotified -= OnCameraChanged;
        Application.Viewport.Camera.RotationNotified -= OnCameraChanged;
        Application.Viewport.Camera.DistanceNotified -= OnCameraChanged;
        Application.Viewport.Camera.SizeNotified -= OnCameraChanged;
        Application.Viewport.Camera.FovNotified -= OnCameraChanged;
        Application.Viewport.Camera.ProjectionTypeNotified -= OnCameraChanged;
        Application.Viewport.Camera.CameraNearNotified -= OnCameraChanged;
        Application.Viewport.Camera.CameraFarNotified -= OnCameraChanged;
    }

    /// <summary>
    /// オーバーレイの表示領域が変化したとき、ラベル位置を再計算する。
    /// </summary>
    private void OnResized()
    {
        UpdateLabelPosition();
    }

    /// <summary>
    /// カメラ状態が変化したとき、ラベル位置を再計算する。
    /// </summary>
    private void OnCameraChanged()
    {
        UpdateLabelPosition();
    }

    #endregion

    #region Helpers

    /// <summary>
    /// 対象位置をカメラ座標へ投影し、画面内の場合だけラベルを表示する。
    /// </summary>
    private void UpdateLabelPosition()
    {
        Vector3 targetPosition = CoordinateSystemUtility.CatiaToGodot(TargetPositionCatia);

        // カメラ背面または視錐台外の対象は、投影座標が不正になるため表示しない。
        if (_camera.IsPositionBehind(targetPosition) || !_camera.IsPositionInFrustum(targetPosition))
        {
            _label.Hide();
            return;
        }

        Vector2 screenPosition = _camera.UnprojectPosition(targetPosition);
        if (screenPosition.X < 0.0f || screenPosition.Y < 0.0f
            || screenPosition.X > Size.X || screenPosition.Y > Size.Y)
        {
            _label.Hide();
            return;
        }

        _label.Position = screenPosition;
        _label.Show();
    }

    #endregion
}
