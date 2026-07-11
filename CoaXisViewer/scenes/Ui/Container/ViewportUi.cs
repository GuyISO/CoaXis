using Godot;
using System;

/// <summary>
/// メインのビューポート状態表示と操作用のパネル
/// </summary>
public partial class ViewportUi : PanelContainer
{
    #region Fields

    private RootModel _rootModel = null!;

    private bool _isInitialized = false; // 初回状態通知を受けたかだけを保持する

    private Label _labelMode = null!;
    private Label _labelProjection = null!;
    private Button _buttonToggleProjection = null!;
    private Button _buttonFitAllIn = null!;
    private Button _buttonFitToSelection = null!;
    private Button _buttonAlignNormal = null!;
    private Button _buttonRollLeft = null!;
    private Button _buttonRollRight = null!;
    private Label _labelPositionX = null!;
    private Label _labelPositionY = null!;
    private Label _labelPositionZ = null!;
    private Label _labelRotationX = null!;
    private Label _labelRotationY = null!;
    private Label _labelRotationZ = null!;
    private Label _labelSize = null!;
    private Label _labelDistance = null!;
    private Label _labelFov = null!;
    private HSlider _sliderFov = null!;

    #endregion

    #region Lifecycle

    public override void _Ready()
    {
        // シーン構造が変更される可能性があるため、名前探索で関連ノードを解決する
        _labelMode = (Label)FindChild("LabelValueMode");
        _labelProjection = (Label)FindChild("LabelValueProjection");
        _buttonToggleProjection = (Button)FindChild("ButtonToggleProjection");
        _buttonFitAllIn = (Button)FindChild("ButtonFitAllIn");
        _buttonFitToSelection = (Button)FindChild("ButtonFitToSelection");
        _buttonAlignNormal = (Button)FindChild("ButtonAlignNormal");
        _buttonRollLeft = (Button)FindChild("ButtonRollLeft");
        _buttonRollRight = (Button)FindChild("ButtonRollRight");
        _labelPositionX = (Label)FindChild("LabelValuePositionX");
        _labelPositionY = (Label)FindChild("LabelValuePositionY");
        _labelPositionZ = (Label)FindChild("LabelValuePositionZ");
        _labelRotationX = (Label)FindChild("LabelValueRotationX");
        _labelRotationY = (Label)FindChild("LabelValueRotationY");
        _labelRotationZ = (Label)FindChild("LabelValueRotationZ");
        _labelSize = (Label)FindChild("LabelValueSize");
        _labelDistance = (Label)FindChild("LabelValueDistance");
        _labelFov = (Label)FindChild("LabelValueFov");
        _sliderFov = (HSlider)FindChild("HSliderFov");

        // UIイベントの購読開始
        _buttonToggleProjection.Pressed += OnButtonToggleProjectionPressed;
        _buttonFitAllIn.Pressed += OnButtonFitAllInPressed;
        _buttonFitToSelection.Pressed += OnButtonFitToSelectionPressed;
        _buttonAlignNormal.Pressed += OnButtonAlignNormalPressed;
        _buttonRollLeft.Pressed += OnButtonRollLeftPressed;
        _buttonRollRight.Pressed += OnButtonRollRightPressed;
        _sliderFov.ValueChanged += OnSliderFovValueChanged;

        // イベントの購読開始
        Application.Model.RootModelNotified += OnRootModelNotified;
        Application.Viewport.InteractionModeNotified += OnInteractionModeNotified;
        Application.Viewport.PositionNotified += OnPositionNotified;
        Application.Viewport.RotationNotified += OnRotationNotified;
        Application.Viewport.DistanceNotified += OnDistanceNotified;
        Application.Viewport.SizeNotified += OnSizeNotified;
        Application.Viewport.FovNotified += OnFovNotified;
        Application.Viewport.ProjectionTypeNotified += OnProjectionTypeNotified;
    }

    public override void _ExitTree()
    {
        // UIイベントの購読解除
        _buttonToggleProjection.Pressed -= OnButtonToggleProjectionPressed;
        _buttonFitAllIn.Pressed -= OnButtonFitAllInPressed;
        _buttonFitToSelection.Pressed -= OnButtonFitToSelectionPressed;
        _buttonAlignNormal.Pressed -= OnButtonAlignNormalPressed;
        _buttonRollLeft.Pressed -= OnButtonRollLeftPressed;
        _buttonRollRight.Pressed -= OnButtonRollRightPressed;
        _sliderFov.ValueChanged -= OnSliderFovValueChanged;

        // イベントの購読解除
        Application.Model.RootModelNotified -= OnRootModelNotified;
        Application.Viewport.InteractionModeNotified -= OnInteractionModeNotified;
        Application.Viewport.PositionNotified -= OnPositionNotified;
        Application.Viewport.RotationNotified -= OnRotationNotified;
        Application.Viewport.DistanceNotified -= OnDistanceNotified;
        Application.Viewport.SizeNotified -= OnSizeNotified;
        Application.Viewport.FovNotified -= OnFovNotified;
        Application.Viewport.ProjectionTypeNotified -= OnProjectionTypeNotified;
    }

    public override void _Process(double delta)
    {
        // Readyで初期化処理を行うと、ほかのノードがまだReadyを完了していない場合に、初期状態通知を受け取れない可能性があるため、Processで初回通知をリクエストする
        if (_rootModel == null)
        {
            Application.Model.AskRootModel();
        }

        if (!_isInitialized)
        {
            Application.Viewport.AskState();
        }
    }

    #endregion

    #region Events

    /// <summary>
    /// 投影切替ボタンのクリックイベントハンドラ、カメラの投影タイプ切替をリクエストする
    /// </summary>
    private void OnButtonToggleProjectionPressed()
    {
        Application.Logger.Debug("ViewportUi: toggle projection requested.");
        Application.Viewport.ToggleProjectionType();
    }

    /// <summary>
    /// Fit All In ボタンのクリックイベントハンドラ、カメラのフィット操作をリクエストする
    /// </summary>
    private void OnButtonFitAllInPressed()
    {
        if (_rootModel == null)
        {
            GD.PushWarning("ViewportUi: fit target model is missing.");
            Application.Logger.Warn("ViewportUi: fit-all requested but default target is missing.");
            return;
        }

        Application.Logger.Debug($"ViewportUi: fit-all requested. target='{_rootModel.Name}'");
        Application.Viewport.Fit(new[] { _rootModel }, true);
    }

    /// <summary>
    /// Fit To Selection ボタンのクリックイベントハンドラ、選択中ノードへのFitをリクエストする
    /// </summary>
    private void OnButtonFitToSelectionPressed()
    {
        AnyModel[] fitTargets = Application.Selection.GetModelArray();
        if (fitTargets.Length == 0)
        {
            Application.Logger.Debug("ViewportUi: fit-to-selection skipped (no selected nodes).");
            return;
        }

        Application.Logger.Debug($"ViewportUi: fit-to-selection requested. targets={fitTargets.Length}");
        Application.Viewport.Fit(fitTargets, true);
    }

    /// <summary>
    /// Align Normal ボタンのクリックイベントハンドラ、カメラの法線方向合わせ操作をリクエストする
    /// </summary>
    private void OnButtonAlignNormalPressed()
    {
        Application.Pick.NotifyPickHandlingMode(PickHandlingMode.NormalToFace);
    }

    /// <summary>
    /// Roll Left ボタンのクリックイベントハンドラ、カメラの左ロール操作をリクエストする
    /// </summary>
    private void OnButtonRollLeftPressed()
    {
        Quaternion rotation = new Quaternion(Vector3.Forward, Mathf.DegToRad(-90f));
        Application.Logger.Debug("ViewportUi: roll-left requested.");
        Application.Viewport.Rotate(rotation, SpaceMode.FocalPoint, true);
    }

    /// <summary>
    /// Roll Right ボタンのクリックイベントハンドラ、カメラの右ロール操作をリクエストする
    /// </summary>
    private void OnButtonRollRightPressed()
    {
        Quaternion rotation = new Quaternion(Vector3.Forward, Mathf.DegToRad(90f));
        Application.Logger.Debug("ViewportUi: roll-right requested.");
        Application.Viewport.Rotate(rotation, SpaceMode.FocalPoint, true);
    }

    /// <summary>
    /// RootModel が通知されたときに呼び出されるイベントハンドラ、キャッシュを更新する
    /// </summary>
    /// <param name="rootModel">通知されたルートモデル</param>
    private void OnRootModelNotified(RootModel rootModel)
    {
        _rootModel = rootModel;
    }

    /// <summary>
    /// FOVスライダーの値変更イベントハンドラ、カメラのFOV設定をリクエストする
    /// </summary>
    /// <param name="value">新しい FOV 値</param>
    private void OnSliderFovValueChanged(double value)
    {
        Application.Logger.Debug($"ViewportUi: set-fov requested. fov={value:F1}");
        Application.Viewport.SetFov((float)value);
    }

    /// <summary>
    /// ビューポートの操作モードが通知されたときに呼び出されるイベントハンドラ
    /// </summary>
    /// <param name="mode">ビューポートの操作モード</param>
    private void OnInteractionModeNotified(ViewportInteractionMode mode)
    {
        // Application.Viewport.AskState の呼び出しによる全情報通知のうちの一つと想定し、初回状態通知を受け取り済みフラグを立てる
        _isInitialized = true;

        _labelMode.Text = mode.ToString();
    }

    /// <summary>
    /// カメラの位置が通知されたときに呼び出されるイベントハンドラ
    /// </summary>
    /// <param name="position">カメラの位置</param>
    private void OnPositionNotified(Vector3 position)
    {
        Vector3 catiaPosition = CoordinateSystemUtility.GodotToCatia(position);
        _labelPositionX.Text = (catiaPosition.X * 1000f).ToString("F3");
        _labelPositionY.Text = (catiaPosition.Y * 1000f).ToString("F3");
        _labelPositionZ.Text = (catiaPosition.Z * 1000f).ToString("F3");
    }

    /// <summary>
    /// カメラの回転が通知されたときに呼び出されるイベントハンドラ
    /// </summary>
    /// <param name="rotation">カメラの回転を表すクォータニオン</param>
    private void OnRotationNotified(Quaternion rotation)
    {
        Quaternion catiaRotation = CoordinateSystemUtility.GodotToCatia(rotation);
        Vector3 rotationDegrees = catiaRotation.GetEuler() * (180f / Mathf.Pi);
        _labelRotationX.Text = rotationDegrees.X.ToString("F3");
        _labelRotationY.Text = rotationDegrees.Y.ToString("F3");
        _labelRotationZ.Text = rotationDegrees.Z.ToString("F3");
    }

    /// <summary>
    /// カメラの距離が通知されたときに呼び出されるイベントハンドラ
    /// </summary>
    /// <param name="distance">カメラの距離</param>
    private void OnDistanceNotified(float distance)
    {
        _labelDistance.Text = (distance * 1000f).ToString("F3");
    }

    /// <summary>
    /// カメラのサイズが通知されたときに呼び出されるイベントハンドラ
    /// </summary>
    /// <param name="size">カメラのサイズ</param>
    private void OnSizeNotified(float size)
    {
        _labelSize.Text = (size * 1000f).ToString("F3");
    }

    /// <summary>
    /// カメラのFOVが通知されたときに呼び出されるイベントハンドラ
    /// </summary>
    /// <param name="fov">カメラのFOV</param>
    private void OnFovNotified(float fov)
    {
        _labelFov.Text = fov.ToString("F1");
        _sliderFov.Value = fov;
    }

    /// <summary>
    /// カメラの投影タイプが通知されたときに呼び出されるイベントハンドラ
    /// </summary>
    /// <param name="type">カメラの投影タイプ</param>
    private void OnProjectionTypeNotified(Camera3D.ProjectionType type)
    {
        _labelProjection.Text = type.ToString();
    }

    #endregion
}
