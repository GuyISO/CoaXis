using Godot;
using System;

/// <summary>
/// メインのビューポート状態表示と操作用のパネル
/// </summary>
public partial class MeasurementPanelContainer : PanelContainer
{
    #region Fields

    private RootModel _rootModel = null!;

    private bool _isInitialized = false; // 初回状態通知を受けたかだけを保持する

    private Label _labelMode = null!;
    private Label _labelProjection = null!;
    private Button _buttonToggleProjection = null!;
    private Button _buttonFitAllIn = null!;
    private Button _buttonFitToSelection = null!;
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
        // シーン構造が固定なので、名前探索ではなく明示パスで関連ノードを解決する
        _labelMode = GetNode<Label>("MarginContainer/VBoxContainer/GridContainer3/LabelValueMode");
        _labelProjection = GetNode<Label>("MarginContainer/VBoxContainer/GridContainer/LabelValueProjection");
        _buttonToggleProjection = GetNode<Button>("MarginContainer/VBoxContainer/ButtonToggleProjection");
        _buttonFitAllIn = GetNode<Button>("MarginContainer/VBoxContainer/ButtonFitAllIn");
        _buttonFitToSelection = GetNode<Button>("MarginContainer/VBoxContainer/ButtonFitToSelection");
        _buttonRollLeft = GetNode<Button>("MarginContainer/VBoxContainer/HBoxContainer/ButtonRollLeft");
        _buttonRollRight = GetNode<Button>("MarginContainer/VBoxContainer/HBoxContainer/ButtonRollRight");
        _labelPositionX = GetNode<Label>("MarginContainer/VBoxContainer/GridContainer2/LabelValuePositionX");
        _labelPositionY = GetNode<Label>("MarginContainer/VBoxContainer/GridContainer2/LabelValuePositionY");
        _labelPositionZ = GetNode<Label>("MarginContainer/VBoxContainer/GridContainer2/LabelValuePositionZ");
        _labelRotationX = GetNode<Label>("MarginContainer/VBoxContainer/GridContainer2/LabelValueRotationX");
        _labelRotationY = GetNode<Label>("MarginContainer/VBoxContainer/GridContainer2/LabelValueRotationY");
        _labelRotationZ = GetNode<Label>("MarginContainer/VBoxContainer/GridContainer2/LabelValueRotationZ");
        _labelSize = GetNode<Label>("MarginContainer/VBoxContainer/GridContainer2/LabelValueSize");
        _labelDistance = GetNode<Label>("MarginContainer/VBoxContainer/GridContainer2/LabelValueDistance");
        _labelFov = GetNode<Label>("MarginContainer/VBoxContainer/GridContainer2/LabelValueFov");
        _sliderFov = GetNode<HSlider>("MarginContainer/VBoxContainer/HSliderFov");

        // UIイベントの購読開始
        _buttonToggleProjection.Pressed += OnButtonToggleProjectionPressed;
        _buttonFitAllIn.Pressed += OnButtonFitAllInPressed;
        _buttonFitToSelection.Pressed += OnButtonFitToSelectionPressed;
        _buttonRollLeft.Pressed += OnButtonRollLeftPressed;
        _buttonRollRight.Pressed += OnButtonRollRightPressed;
        _sliderFov.ValueChanged += OnSliderFovValueChanged;

        // イベントの購読開始
        ModelEventHub.Instance.RootModelNotified += OnRootModelNotified;
        ViewportEventHub.Instance.InputModeNotified += OnInputModeNotified;
        ViewportEventHub.Instance.PositionNotified += OnPositionNotified;
        ViewportEventHub.Instance.RotationNotified += OnRotationNotified;
        ViewportEventHub.Instance.DistanceNotified += OnDistanceNotified;
        ViewportEventHub.Instance.SizeNotified += OnSizeNotified;
        ViewportEventHub.Instance.FovNotified += OnFovNotified;
        ViewportEventHub.Instance.ProjectionTypeNotified += OnProjectionTypeNotified;
    }

    public override void _ExitTree()
    {
        // UIイベントの購読解除
        _buttonToggleProjection.Pressed -= OnButtonToggleProjectionPressed;
        _buttonFitAllIn.Pressed -= OnButtonFitAllInPressed;
        _buttonFitToSelection.Pressed -= OnButtonFitToSelectionPressed;
        _buttonRollLeft.Pressed -= OnButtonRollLeftPressed;
        _buttonRollRight.Pressed -= OnButtonRollRightPressed;
        _sliderFov.ValueChanged -= OnSliderFovValueChanged;

        // イベントの購読解除
        ViewportEventHub.Instance.InputModeNotified -= OnInputModeNotified;
        ViewportEventHub.Instance.PositionNotified -= OnPositionNotified;
        ViewportEventHub.Instance.RotationNotified -= OnRotationNotified;
        ViewportEventHub.Instance.DistanceNotified -= OnDistanceNotified;
        ViewportEventHub.Instance.SizeNotified -= OnSizeNotified;
        ViewportEventHub.Instance.FovNotified -= OnFovNotified;
        ViewportEventHub.Instance.ProjectionTypeNotified -= OnProjectionTypeNotified;
    }

    public override void _Process(double delta)
    {
        // Readyで初期化処理を行うと、ほかのノードがまだReadyを完了していない場合に、初期状態通知を受け取れない可能性があるため、Processで初回通知をリクエストする
        if (_rootModel == null)
        {
            ModelEventHub.RequestNotifyRootModel();
        }

        if (!_isInitialized)
        {
            ViewportEventHub.RequestNotifyState();
        }
    }

    #endregion

    #region Events

    /// <summary>
    /// 投影切替ボタンのクリックイベントハンドラ、カメラの投影タイプ切替をリクエストする
    /// </summary>
    private void OnButtonToggleProjectionPressed()
    {
        LogHub.Debug("MeasurementPanel: toggle projection requested.");
        ViewportEventHub.RequestToggleProjectionType();
    }

    /// <summary>
    /// Fit All In ボタンのクリックイベントハンドラ、カメラのフィット操作をリクエストする
    /// </summary>
    private void OnButtonFitAllInPressed()
    {
        if (_rootModel == null)
        {
            GD.PushWarning("MeasurementPanel: fit target model is missing.");
            LogHub.Warn("MeasurementPanel: fit-all requested but default target is missing.");
            return;
        }

        LogHub.Debug($"MeasurementPanel: fit-all requested. target='{_rootModel.Name}'");
        ViewportEventHub.RequestFit(new[] { _rootModel }, true);
    }

    /// <summary>
    /// Fit To Selection ボタンのクリックイベントハンドラ、選択中ノードへのFitをリクエストする
    /// </summary>
    private void OnButtonFitToSelectionPressed()
    {
        AnyModel[] fitTargets = Selection.GetModelArray();
        if (fitTargets.Length == 0)
        {
            LogHub.Debug("MeasurementPanel: fit-to-selection skipped (no selected nodes).");
            return;
        }

        LogHub.Debug($"MeasurementPanel: fit-to-selection requested. targets={fitTargets.Length}");
        ViewportEventHub.RequestFit(fitTargets, true);
    }

    /// <summary>
    /// Roll Left ボタンのクリックイベントハンドラ、カメラの左ロール操作をリクエストする
    /// </summary>
    private void OnButtonRollLeftPressed()
    {
        Quaternion rotation = new Quaternion(Vector3.Forward, Mathf.DegToRad(-90f));
        LogHub.Debug("MeasurementPanel: roll-left requested.");
        ViewportEventHub.RequestRotate(rotation, SpaceMode.FocalPoint, true);
    }

    /// <summary>
    /// Roll Right ボタンのクリックイベントハンドラ、カメラの右ロール操作をリクエストする
    /// </summary>
    private void OnButtonRollRightPressed()
    {
        Quaternion rotation = new Quaternion(Vector3.Forward, Mathf.DegToRad(90f));
        LogHub.Debug("MeasurementPanel: roll-right requested.");
        ViewportEventHub.RequestRotate(rotation, SpaceMode.FocalPoint, true);
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
        LogHub.Debug($"MeasurementPanel: set-fov requested. fov={value:F1}");
        ViewportEventHub.RequestSetFov((float)value);
    }

    /// <summary>
    /// ビューポートへの入力モードが通知されたときに呼び出されるイベントハンドラ
    /// </summary>
    /// <param name="mode">ビューポートの入力モード</param>
    private void OnInputModeNotified(ViewportInputMode mode)
    {
        // ViewportEventHub.RequestNotifyState の呼び出しによる全情報通知のうちの一つと想定し、初回状態通知を受け取り済みフラグを立てる
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
