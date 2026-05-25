using Godot;
using System;

public partial class CameraUi : Control
{
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

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_cameraController ??= (CameraController)GetNode("/root/Main/CameraController");

		// UI要素を取得
		_labelProjection = (Label)FindChild("LabelValueProjection", true);
		_buttonToggleProjection = (Button)FindChild("ButtonToggleProjection", true);
		_buttonFitAllIn = (Button)FindChild("ButtonFitAllIn", true);
		_buttonRollLeft = (Button)FindChild("ButtonRollLeft", true);
		_buttonRollRight = (Button)FindChild("ButtonRollRight", true);
		_labelPositionX = (Label)FindChild("LabelValuePositionX", true);
		_labelPositionY = (Label)FindChild("LabelValuePositionY", true);
		_labelPositionZ = (Label)FindChild("LabelValuePositionZ", true);
		_labelRotationX = (Label)FindChild("LabelValueRotationX", true);
		_labelRotationY = (Label)FindChild("LabelValueRotationY", true);
		_labelRotationZ = (Label)FindChild("LabelValueRotationZ", true);
		_labelSize = (Label)FindChild("LabelValueSize", true);
		_labelDistance = (Label)FindChild("LabelValueDistance", true);
		_labelFov = (Label)FindChild("LabelValueFov", true);
		_sliderFov = (HSlider)FindChild("HSliderFov", true);

		// 初期値をUIに反映
		_labelProjection.Text = _cameraController.Camera.Projection.ToString();
		_sliderFov.Value = _cameraController.Camera.Fov;
		_sliderFov.ValueChanged += OnSliderFovValueChanged;
		_buttonToggleProjection.Pressed += OnButtonToggleProjectionPressed;
		_buttonFitAllIn.Pressed += OnButtonFitAllInPressed;
		_buttonRollLeft.Pressed += OnButtonRollLeftPressed;
		_buttonRollRight.Pressed += OnButtonRollRightPressed;
		
	}

	public override void _ExitTree()
	{
		_buttonToggleProjection.Pressed -= OnButtonToggleProjectionPressed;
		_buttonFitAllIn.Pressed -= OnButtonFitAllInPressed;
		_buttonRollLeft.Pressed -= OnButtonRollLeftPressed;
		_buttonRollRight.Pressed -= OnButtonRollRightPressed;
		_sliderFov.ValueChanged -= OnSliderFovValueChanged;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		RefreshCameraInfo();
	}

	private void OnButtonToggleProjectionPressed()
	{
		_cameraController.ToggleProjectionType();
		_labelProjection.Text = _cameraController.Camera.Projection.ToString();
	}

	private void OnSliderFovValueChanged(double value)
	{
		_cameraController.Camera.Fov = (float)value;
	}

	private void OnButtonFitAllInPressed()
	{
		Node targetNode = _defaultFitTargetNode;
		targetNode ??= GetNode("/root/Main/Models");
		_cameraController.Fit(targetNode, true);
	}

	private void OnButtonRollLeftPressed()
	{
		_cameraController.Roll(-90f, true); // 90度左にロール
	}

	private void OnButtonRollRightPressed()
	{
		_cameraController.Roll(90f, true); // 90度右にロール
	}

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
}
