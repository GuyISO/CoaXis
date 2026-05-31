using Godot;
using System;

/// <summary>
/// カメラ操作用 UI（投影切替、Fit、Roll、状態表示）を管理します。
/// </summary>
public partial class CameraUi : Control
{
	#region Fields

	[Export] private CameraController _cameraController;
	[Export] private Node _defaultFitTargetNode;

	private Label _labelProjection;
	private Button _buttonToggleProjection;
	private Button _buttonFitAllIn;
	private Button _buttonRollLeft;
	private Button _buttonRollRight;
	private Label _labelPositionX;
	private Label _labelPositionY;
	private Label _labelPositionZ;
	private Label _labelRotationX;
	private Label _labelRotationY;
	private Label _labelRotationZ;
	private Label _labelSize;
	private Label _labelDistance;
	private Label _labelFov;
	private HSlider _sliderFov;
	private bool _isInitialized;

	#endregion

	#region Lifecycle

	/// <summary>
	/// UI要素を解決し、イベントを購読します。
	/// </summary>
	public override void _Ready()
	{
		_cameraController ??= GetNodeOrNull<CameraController>("../VBoxContainer/HBoxContainer/MainScreen/SubViewportContainer/SubViewport/CameraController");
		_cameraController ??= GetTree()?.Root?.FindChild("CameraController", true, false) as CameraController;

		// UI要素を取得する。
		_labelProjection = FindChild("LabelValueProjection", true) as Label;
		_buttonToggleProjection = FindChild("ButtonToggleProjection", true) as Button;
		_buttonFitAllIn = FindChild("ButtonFitAllIn", true) as Button;
		_buttonRollLeft = FindChild("ButtonRollLeft", true) as Button;
		_buttonRollRight = FindChild("ButtonRollRight", true) as Button;
		_labelPositionX = FindChild("LabelValuePositionX", true) as Label;
		_labelPositionY = FindChild("LabelValuePositionY", true) as Label;
		_labelPositionZ = FindChild("LabelValuePositionZ", true) as Label;
		_labelRotationX = FindChild("LabelValueRotationX", true) as Label;
		_labelRotationY = FindChild("LabelValueRotationY", true) as Label;
		_labelRotationZ = FindChild("LabelValueRotationZ", true) as Label;
		_labelSize = FindChild("LabelValueSize", true) as Label;
		_labelDistance = FindChild("LabelValueDistance", true) as Label;
		_labelFov = FindChild("LabelValueFov", true) as Label;
		_sliderFov = FindChild("HSliderFov", true) as HSlider;

		if (_cameraController == null
			|| _labelProjection == null
			|| _buttonToggleProjection == null
			|| _buttonFitAllIn == null
			|| _buttonRollLeft == null
			|| _buttonRollRight == null
			|| _labelPositionX == null
			|| _labelPositionY == null
			|| _labelPositionZ == null
			|| _labelRotationX == null
			|| _labelRotationY == null
			|| _labelRotationZ == null
			|| _labelSize == null
			|| _labelDistance == null
			|| _labelFov == null
			|| _sliderFov == null)
		{
			GD.PushWarning("CameraUi: required nodes are missing. Camera UI initialization skipped.");
			_isInitialized = false;
			return;
		}

		// 初期値をUIに反映
		_labelProjection.Text = _cameraController.Camera.Projection.ToString();
		_sliderFov.Value = _cameraController.Camera.Fov;
		_sliderFov.ValueChanged += OnSliderFovValueChanged;
		_buttonToggleProjection.Pressed += OnButtonToggleProjectionPressed;
		_buttonFitAllIn.Pressed += OnButtonFitAllInPressed;
		_buttonRollLeft.Pressed += OnButtonRollLeftPressed;
		_buttonRollRight.Pressed += OnButtonRollRightPressed;
		_isInitialized = true;
		
	}

	/// <summary>
	/// 購読した UI イベントを解除します。
	/// </summary>
	public override void _ExitTree()
	{
		if (!_isInitialized)
		{
			return;
		}

		_buttonToggleProjection.Pressed -= OnButtonToggleProjectionPressed;
		_buttonFitAllIn.Pressed -= OnButtonFitAllInPressed;
		_buttonRollLeft.Pressed -= OnButtonRollLeftPressed;
		_buttonRollRight.Pressed -= OnButtonRollRightPressed;
		_sliderFov.ValueChanged -= OnSliderFovValueChanged;
	}

	/// <summary>
	/// 毎フレームのカメラ状態を UI へ反映します。
	/// </summary>
	/// <param name="delta">前フレームからの経過秒。</param>
	public override void _Process(double delta)
	{
		if (!_isInitialized)
		{
			return;
		}

		RefreshCameraInfo();
	}

	#endregion

	#region Internal Helpers

	// UI操作で投影方式を切り替えた直後に表示ラベルを同期し、表示の遅れを防ぐ。
	private void OnButtonToggleProjectionPressed()
	{
		_cameraController.ToggleProjectionType();
		_labelProjection.Text = _cameraController.Camera.Projection.ToString();
	}

	// スライダー値をそのまま FOV に反映し、調整中のレスポンスを優先する。
	private void OnSliderFovValueChanged(double value)
	{
		_cameraController.Camera.Fov = (float)value;
	}

	// Fit対象は明示指定を優先し、未指定時は Main/Models にフォールバックする。
	private void OnButtonFitAllInPressed()
	{
		Node targetNode = _defaultFitTargetNode;
		targetNode ??= GetNodeOrNull("../../Models");
		if (targetNode == null)
		{
			GD.PushWarning("CameraUi: fit target node is missing.");
			return;
		}

		_cameraController.Fit(targetNode, true);
	}

	// CATIA風操作に合わせ、ロールは 90 度単位で固定する。
	private void OnButtonRollLeftPressed()
	{
		_cameraController.Roll(-90f, true); // 90度左にロール
	}

	// CATIA風操作に合わせ、ロールは 90 度単位で固定する。
	private void OnButtonRollRightPressed()
	{
		_cameraController.Roll(90f, true); // 90度右にロール
	}

	#endregion

	#region Internal Helpers

	// 監視ラベルを毎フレーム更新し、デバッグ時にカメラ状態を即時確認できるようにする。
	private void RefreshCameraInfo()
	{
		var position = _cameraController.FocalPoint.Position;
		var rotation = _cameraController.FocalPoint.RotationDegrees;
		var size = _cameraController.Camera.Size;
		var distance = _cameraController.Camera.Position.Z;
		var fov = _cameraController.Camera.Fov;

		_labelPositionX.Text = (position.X * 1000f).ToString("F3");
		_labelPositionY.Text = (position.Y * 1000f).ToString("F3");
		_labelPositionZ.Text = (position.Z * 1000f).ToString("F3");
		_labelRotationX.Text = rotation.X.ToString("F3");
		_labelRotationY.Text = rotation.Y.ToString("F3");
		_labelRotationZ.Text = rotation.Z.ToString("F3");
		_labelSize.Text = (size * 1000f).ToString("F3");
		_labelDistance.Text = (distance * 1000f).ToString("F3");
		_labelFov.Text = fov.ToString("F3");
	}

	#endregion
}



