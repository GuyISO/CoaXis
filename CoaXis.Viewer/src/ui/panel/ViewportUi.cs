using Godot;
using System;

/// <summary>
/// メインのビューポート状態表示と操作用のパネル
/// </summary>
public partial class ViewportUi : PanelContainer
{
    #region Fields

    // 関連ノードのキャッシュ
    private Label _labelMode = null!;
    private Label _labelProjection = null!;
    private Button _buttonToggleProjection = null!;
    private Button _buttonFitAllIn = null!;
    private Button _buttonFitToSelection = null!;
    private Button _buttonAlignNormal = null!;
    private Button _buttonRollLeft = null!;
    private Button _buttonRollRight = null!;
    private Button _buttonLayerModelEntityVisible = null!;
    private Button _buttonLayerModelEntityInvisible = null!;
    private Button _buttonLayerModelPropertyVisible = null!;
    private Button _buttonLayerModelPropertyInvisible = null!;
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
        EnsureChildNodes();
        SubscribeUiEvents();
        SubscribeApplicationEvents();
        SyncInitialState();
    }

    public override void _ExitTree()
    {
        UnsubscribeUiEvents();
        UnsubscribeApplicationEvents();

        base._ExitTree();
    }

    #endregion

    #region Events

    /// <summary>
    /// 子ノードを解決し、フィールドに保持する
    /// </summary>
    private void EnsureChildNodes()
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
        _buttonLayerModelEntityVisible = (Button)FindChild("ButtonLayerModelEntityVisible");
        _buttonLayerModelEntityInvisible = (Button)FindChild("ButtonLayerModelEntityInvisible");
        _buttonLayerModelPropertyVisible = (Button)FindChild("ButtonLayerModelPropertyVisible");
        _buttonLayerModelPropertyInvisible = (Button)FindChild("ButtonLayerModelPropertyInvisible");
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
    }
    
    /// <summary>
    /// UIイベントの購読を開始する
    /// </summary>
    private void SubscribeUiEvents()
    {
        _buttonToggleProjection.Pressed += OnButtonToggleProjectionPressed;
        _buttonFitAllIn.Pressed += OnButtonFitAllInPressed;
        _buttonFitToSelection.Pressed += OnButtonFitToSelectionPressed;
        _buttonAlignNormal.Pressed += OnButtonAlignNormalPressed;
        _buttonRollLeft.Pressed += OnButtonRollLeftPressed;
        _buttonRollRight.Pressed += OnButtonRollRightPressed;
        _buttonLayerModelEntityVisible.Pressed += OnButtonLayerModelEntityVisiblePressed;
        _buttonLayerModelEntityInvisible.Pressed += OnButtonLayerModelEntityInvisiblePressed;
        _buttonLayerModelPropertyVisible.Pressed += OnButtonLayerModelPropertyVisiblePressed;
        _buttonLayerModelPropertyInvisible.Pressed += OnButtonLayerModelPropertyInvisiblePressed;
        _sliderFov.ValueChanged += OnSliderFovValueChanged;
    }

    /// <summary>
    /// UIイベントの購読を解除する
    /// </summary>
    private void UnsubscribeUiEvents()
    {
        _buttonToggleProjection.Pressed -= OnButtonToggleProjectionPressed;
        _buttonFitAllIn.Pressed -= OnButtonFitAllInPressed;
        _buttonFitToSelection.Pressed -= OnButtonFitToSelectionPressed;
        _buttonAlignNormal.Pressed -= OnButtonAlignNormalPressed;
        _buttonRollLeft.Pressed -= OnButtonRollLeftPressed;
        _buttonRollRight.Pressed -= OnButtonRollRightPressed;
        _buttonLayerModelEntityVisible.Pressed -= OnButtonLayerModelEntityVisiblePressed;
        _buttonLayerModelEntityInvisible.Pressed -= OnButtonLayerModelEntityInvisiblePressed;
        _buttonLayerModelPropertyVisible.Pressed -= OnButtonLayerModelPropertyVisiblePressed;
        _buttonLayerModelPropertyInvisible.Pressed -= OnButtonLayerModelPropertyInvisiblePressed;
        _sliderFov.ValueChanged -= OnSliderFovValueChanged;
    }

    /// <summary>
    /// Applicationイベントの購読を開始する
    /// </summary>
    private void SubscribeApplicationEvents()
    {
        Application.Model.Entity.Pick.HandlingModeNotified += OnPickHandlingModeNotified;
        Application.Viewport.Interaction.ModeNotified += OnInteractionModeNotified;
        Application.Viewport.Camera.PositionNotified += OnPositionNotified;
        Application.Viewport.Camera.RotationNotified += OnRotationNotified;
        Application.Viewport.Camera.DistanceNotified += OnDistanceNotified;
        Application.Viewport.Camera.SizeNotified += OnSizeNotified;
        Application.Viewport.Camera.FovNotified += OnFovNotified;
        Application.Viewport.Camera.ProjectionTypeNotified += OnProjectionTypeNotified;
        Application.Viewport.Display.ActiveLayersNotified += OnActiveLayersNotified;
    }

    /// <summary>
    /// Applicationイベントの購読を解除する
    /// </summary>
    private void UnsubscribeApplicationEvents()
    {
        Application.Model.Entity.Pick.HandlingModeNotified -= OnPickHandlingModeNotified;
        Application.Viewport.Interaction.ModeNotified -= OnInteractionModeNotified;
        Application.Viewport.Camera.PositionNotified -= OnPositionNotified;
        Application.Viewport.Camera.RotationNotified -= OnRotationNotified;
        Application.Viewport.Camera.DistanceNotified -= OnDistanceNotified;
        Application.Viewport.Camera.SizeNotified -= OnSizeNotified;
        Application.Viewport.Camera.FovNotified -= OnFovNotified;
        Application.Viewport.Camera.ProjectionTypeNotified -= OnProjectionTypeNotified;
        Application.Viewport.Display.ActiveLayersNotified -= OnActiveLayersNotified;
    }

    /// <summary>
    /// Hubが保持する現在状態を読み取り、UIへ初期反映する。
    /// </summary>
    private void SyncInitialState()
    {
        OnPickHandlingModeNotified();
        OnInteractionModeNotified();
        OnPositionNotified();
        OnRotationNotified();
        OnDistanceNotified();
        OnSizeNotified();
        OnFovNotified();
        OnProjectionTypeNotified();
        OnActiveLayersNotified();
    }

    /// <summary>
    /// ピック操作モードが通知されたときに呼び出されるイベントハンドラ
    /// </summary>
    private void OnPickHandlingModeNotified()
    {
        // ビューポート操作モードが NormalToFace の場合、Align Normal ボタンを押下状態にする
        _buttonAlignNormal.ButtonPressed = Application.Model.Entity.Pick.HandlingMode == PickHandlingMode.NormalToFace;
    }

    /// <summary>
    /// 投影切替ボタンのクリックイベントハンドラ、カメラの投影タイプ切替をリクエストする
    /// </summary>
    private void OnButtonToggleProjectionPressed()
    {
        Application.Log.Debug("ViewportUi: toggle projection requested.");
        Application.Viewport.Camera.ToggleProjectionType();
    }

    /// <summary>
    /// Fit All In ボタンのクリックイベントハンドラ、カメラのフィット操作をリクエストする
    /// </summary>
    private void OnButtonFitAllInPressed()
    {
        Node3D targetNode = Application.Model.Entity.Registry.Root.Node;
        if (targetNode == null)
        {
            Application.Log.Debug("ViewportUi: fit-all skipped (no root model).");
            return;
        }
        Application.Log.Debug($"ViewportUi: fit-all requested. target='{targetNode.Name}'");
        Application.Viewport.Camera.Fit(new[] { targetNode }, true);
    }

    /// <summary>
    /// Fit To Selection ボタンのクリックイベントハンドラ、選択中ノードへのFitをリクエストする
    /// </summary>
    private void OnButtonFitToSelectionPressed()
    {
        Node3D[] fitTargetNodes = Application.Model.Entity.Selection.GetModelNodeArray();
        if (fitTargetNodes.Length == 0)
        {
            Application.Log.Debug("ViewportUi: fit-to-selection skipped (no selected nodes).");
            return;
        }

        Application.Log.Debug($"ViewportUi: fit-to-selection requested. targets={fitTargetNodes.Length}");
        Application.Viewport.Camera.Fit(fitTargetNodes, true);
    }

    /// <summary>
    /// Align Normal ボタンのクリックイベントハンドラ、カメラの法線方向合わせ操作をリクエストする
    /// </summary>
    private void OnButtonAlignNormalPressed()
    {
        Application.Model.Entity.Pick.SetHandlingMode(PickHandlingMode.NormalToFace);
    }

    /// <summary>
    /// Roll Left ボタンのクリックイベントハンドラ、カメラの左ロール操作をリクエストする
    /// </summary>
    private void OnButtonRollLeftPressed()
    {
        Quaternion rotation = new Quaternion(Vector3.Forward, Mathf.DegToRad(-90f));
        Application.Log.Debug("ViewportUi: roll-left requested.");
        Application.Viewport.Camera.Rotate(rotation, SpaceMode.FocalPoint, true);
    }

    /// <summary>
    /// Roll Right ボタンのクリックイベントハンドラ、カメラの右ロール操作をリクエストする
    /// </summary>
    private void OnButtonRollRightPressed()
    {
        Quaternion rotation = new Quaternion(Vector3.Forward, Mathf.DegToRad(90f));
        Application.Log.Debug("ViewportUi: roll-right requested.");
        Application.Viewport.Camera.Rotate(rotation, SpaceMode.FocalPoint, true);
    }

    /// <summary>
    /// FOVスライダーの値変更イベントハンドラ、カメラのFOV設定をリクエストする
    /// </summary>
    /// <param name="value">新しい FOV 値</param>
    private void OnSliderFovValueChanged(double value)
    {
        Application.Log.Debug($"ViewportUi: set-fov requested. fov={value:F1}");
        Application.Viewport.Camera.SetFov((float)value);
    }

    /// <summary>
    /// Visible レイヤーの表示切替をリクエストする
    /// </summary>
    private void OnButtonLayerModelEntityVisiblePressed()
    {
        Application.Viewport.Display.SetLayerActive(
            (uint)ViewportLayer.VisibleEntity,
            _buttonLayerModelEntityVisible.ButtonPressed);
    }

    /// <summary>
    /// ModelEntity Invisible レイヤーの表示切替をリクエストする
    /// </summary>
    private void OnButtonLayerModelEntityInvisiblePressed()
    {
        Application.Viewport.Display.SetLayerActive(
            (uint)ViewportLayer.InvisibleEntity,
            _buttonLayerModelEntityInvisible.ButtonPressed);
    }

    /// <summary>
    /// ModelProperty Visible レイヤーの表示切替をリクエストする
    /// </summary>
    private void OnButtonLayerModelPropertyVisiblePressed()
    {
        Application.Viewport.Display.SetLayerActive(
            (uint)ViewportLayer.VisibleProperty,
            _buttonLayerModelPropertyVisible.ButtonPressed);
    }

    /// <summary>
    /// ModelProperty Invisible レイヤーの表示切替をリクエストする
    /// </summary>
    private void OnButtonLayerModelPropertyInvisiblePressed()
    {
        Application.Viewport.Display.SetLayerActive(
            (uint)ViewportLayer.InvisibleProperty,
            _buttonLayerModelPropertyInvisible.ButtonPressed);
    }

    /// <summary>
    /// ビューポートの操作モードが通知されたときに呼び出されるイベントハンドラ
    /// </summary>
    private void OnInteractionModeNotified()
    {
        _labelMode.Text = Application.Viewport.Interaction.Mode.ToString();
    }

    /// <summary>
    /// カメラの位置が通知されたときに呼び出されるイベントハンドラ
    /// </summary>
    private void OnPositionNotified()
    {
        Vector3 position = Application.Viewport.Camera.Position;
        Vector3 catiaPosition = CoordinateSystemUtility.GodotToCatia(position);
        _labelPositionX.Text = catiaPosition.X.ToString("F3");
        _labelPositionY.Text = catiaPosition.Y.ToString("F3");
        _labelPositionZ.Text = catiaPosition.Z.ToString("F3");
    }

    /// <summary>
    /// カメラの回転が通知されたときに呼び出されるイベントハンドラ
    /// </summary>
    private void OnRotationNotified()
    {
        Quaternion catiaRotation = CoordinateSystemUtility.GodotToCatia(Application.Viewport.Camera.Rotation);
        Vector3 rotationDegrees = catiaRotation.GetEuler() * (180f / Mathf.Pi);
        _labelRotationX.Text = rotationDegrees.X.ToString("F3");
        _labelRotationY.Text = rotationDegrees.Y.ToString("F3");
        _labelRotationZ.Text = rotationDegrees.Z.ToString("F3");
    }

    /// <summary>
    /// カメラの距離が通知されたときに呼び出されるイベントハンドラ
    /// </summary>
    private void OnDistanceNotified()
    {
        _labelDistance.Text = CoordinateSystemUtility.GodotDistanceToCatia(Application.Viewport.Camera.Distance).ToString("F3");
    }

    /// <summary>
    /// カメラのサイズが通知されたときに呼び出されるイベントハンドラ
    /// </summary>
    private void OnSizeNotified()
    {
        _labelSize.Text = CoordinateSystemUtility.GodotDistanceToCatia(Application.Viewport.Camera.Size).ToString("F3");
    }

    /// <summary>
    /// カメラのFOVが通知されたときに呼び出されるイベントハンドラ
    /// </summary>
    private void OnFovNotified()
    {
        float fov = Application.Viewport.Camera.Fov;
        _labelFov.Text = fov.ToString("F1");
        _sliderFov.Value = fov;
    }

    /// <summary>
    /// カメラの投影タイプが通知されたときに呼び出されるイベントハンドラ
    /// </summary>
    private void OnProjectionTypeNotified()
    {
        _labelProjection.Text = Application.Viewport.Camera.ProjectionType.ToString();
    }

    /// <summary>
    /// ビューポートレイヤーの状態通知を受けてボタンの押下状態を更新する
    /// </summary>
    private void OnActiveLayersNotified()
    {
        uint activeLayers = Application.Viewport.Display.ActiveLayers;
        _buttonLayerModelEntityVisible.ButtonPressed = (activeLayers & (uint)ViewportLayer.VisibleEntity) != 0;
        _buttonLayerModelEntityInvisible.ButtonPressed = (activeLayers & (uint)ViewportLayer.InvisibleEntity) != 0;
        _buttonLayerModelPropertyVisible.ButtonPressed = (activeLayers & (uint)ViewportLayer.VisibleProperty) != 0;
        _buttonLayerModelPropertyInvisible.ButtonPressed = (activeLayers & (uint)ViewportLayer.InvisibleProperty) != 0;
    }

    #endregion
}
