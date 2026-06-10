using Godot;
using System;

/// <summary>
/// カメラ操作用 UI（投影切替、Fit、Roll、状態表示）を管理します。
/// </summary>
public partial class CameraUi : Control
{
	#region Fields

	[Export] private Node3D _defaultFitTargetNode;

	private bool _isInitialized = false; // カメラの全状態の通知をリクエストしてUIを初期化したかどうかのフラグ

	// 関連ノードのキャッシュ
	private Label _labelMode;
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

    #endregion

    #region Lifecycle

	public override void _Ready()
	{
		// 関連ノードのキャッシュ
		_labelMode = FindChild("LabelValueMode", true) as Label;
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

		// UIイベントの登録
		_buttonToggleProjection.Pressed += OnButtonToggleProjectionPressed;
		_buttonFitAllIn.Pressed += OnButtonFitAllInPressed;
		_buttonRollLeft.Pressed += OnButtonRollLeftPressed;
		_buttonRollRight.Pressed += OnButtonRollRightPressed;
		_sliderFov.ValueChanged += OnSliderFovValueChanged;

		// イベント購読の登録
		CameraEventHub.I.ControlModeNotified += OnControlModeNotified;
		CameraEventHub.I.PositionNotified += OnPositionNotified;
		CameraEventHub.I.RotationNotified += OnRotationNotified;
		CameraEventHub.I.DistanceNotified += OnDistanceNotified;
		CameraEventHub.I.SizeNotified += OnSizeNotified;
		CameraEventHub.I.FovNotified += OnFovNotified;
		CameraEventHub.I.ProjectionTypeNotified += OnProjectionTypeNotified;
	}

	public override void _ExitTree()
	{
		// UIイベントの解除
		_buttonToggleProjection.Pressed -= OnButtonToggleProjectionPressed;
		_buttonFitAllIn.Pressed -= OnButtonFitAllInPressed;
		_buttonRollLeft.Pressed -= OnButtonRollLeftPressed;
		_buttonRollRight.Pressed -= OnButtonRollRightPressed;
		_sliderFov.ValueChanged -= OnSliderFovValueChanged;

		// イベント購読の解除
		CameraEventHub.I.ControlModeNotified -= OnControlModeNotified;
		CameraEventHub.I.PositionNotified -= OnPositionNotified;
		CameraEventHub.I.RotationNotified -= OnRotationNotified;
		CameraEventHub.I.DistanceNotified -= OnDistanceNotified;
		CameraEventHub.I.SizeNotified -= OnSizeNotified;
		CameraEventHub.I.FovNotified -= OnFovNotified;
		CameraEventHub.I.ProjectionTypeNotified -= OnProjectionTypeNotified;
	}

	public override void _Process(double delta)
	{
		if (!_isInitialized)
		{
			CameraEventHub.I.RequestNotifyState(); // カメラの全状態の通知をリクエストして、UIを初期化する
			_isInitialized = true;
		}
	}

	#endregion

	#region Events

	/// <summary>
	/// 投影切替ボタンのクリックイベントハンドラです。カメラの投影タイプ切替をリクエストします。
	/// </summary>
	private void OnButtonToggleProjectionPressed()
	{
		CameraEventHub.I.RequestToggleProjectionType();
	}

	/// <summary>
	/// Fit All In ボタンのクリックイベントハンドラです。カメラのフィット操作をリクエストします。
	/// </summary>
	/// <remarks>
	/// フィットの対象は、まず _defaultFitTargetNode で、そこからさらに "../../Models" ノードを探して見つかった方を使用します。
	///  /// どちらも見つからない場合は警告を出してフィット操作をリクエストしません。
	/// </remarks>
	private void OnButtonFitAllInPressed()
	{
		Node3D targetNode = _defaultFitTargetNode;
		targetNode ??= GetNodeOrNull<Node3D>("../../Models");
		if (targetNode == null)
		{
			GD.PushWarning("CameraUi: fit target node is missing.");
			return;
		}

		CameraEventHub.I.RequestFit(targetNode, true);
	}

	/// <summary>
	/// Roll Left ボタンのクリックイベントハンドラです。カメラの左ロール操作をリクエストします。
	/// </summary>
	private void OnButtonRollLeftPressed()
	{
		Quaternion rotation = new Quaternion(Vector3.Forward, Mathf.DegToRad(-90f));
		CameraEventHub.I.RequestRotate(rotation, true); // 90度左にロール
	}

	/// <summary>
	/// Roll Right ボタンのクリックイベントハンドラです。カメラの右ロール操作をリクエストします。
	/// </summary>
	private void OnButtonRollRightPressed()
	{
		Quaternion rotation = new Quaternion(Vector3.Forward, Mathf.DegToRad(90f));
		CameraEventHub.I.RequestRotate(rotation, true); // 90度右にロール
	}

	/// <summary>
	/// FOVスライダーの値変更イベントハンドラです。カメラのFOV設定をリクエストします。
	/// </summary>
	private void OnSliderFovValueChanged(double value)
	{
		CameraEventHub.I.RequestSetFov((float)value);
	}

	/// <summary>
	/// カメラの操作モードが通知されたときに呼び出されるイベントハンドラです。
	/// </summary>
	/// <param name="mode">カメラの操作モードです。</param>
	private void OnControlModeNotified(CameraControlMode mode)
	{
		_labelMode.Text = mode.ToString();
	}

	/// <summary>
	/// カメラの位置が通知されたときに呼び出されるイベントハンドラです。
	/// </summary>
	/// <param name="position">カメラの位置です。</param>
	private void OnPositionNotified(Vector3 position)
	{
		_labelPositionX.Text = (position.X * 1000f).ToString("F3");
		_labelPositionY.Text = (position.Y * 1000f).ToString("F3");
		_labelPositionZ.Text = (position.Z * 1000f).ToString("F3");
	}

	/// <summary>
	/// カメラの回転が通知されたときに呼び出されるイベントハンドラです。
	/// </summary>
	/// <param name="rotation">カメラの回転を表すクォータニオンです。</param>
	private void OnRotationNotified(Quaternion rotation)
	{
		Vector3 rotationDegrees = rotation.GetEuler() * (180f / Mathf.Pi);
		_labelRotationX.Text = rotationDegrees.X.ToString("F3");
		_labelRotationY.Text = rotationDegrees.Y.ToString("F3");
		_labelRotationZ.Text = rotationDegrees.Z.ToString("F3");
	}

	/// <summary>
	/// カメラの距離が通知されたときに呼び出されるイベントハンドラです。
	/// </summary>
	/// <param name="distance">カメラの距離です。</param>
	private void OnDistanceNotified(float distance)
	{
		_labelDistance.Text = (distance * 1000f).ToString("F3");
	}

	/// <summary>
	/// カメラのサイズが通知されたときに呼び出されるイベントハンドラです。
	/// </summary>
	/// <param name="size">カメラのサイズです。</param>
	private void OnSizeNotified(float size)
	{
		_labelSize.Text = (size * 1000f).ToString("F3");
	}

	/// <summary>
	/// カメラのFOVが通知されたときに呼び出されるイベントハンドラです。
	/// </summary>
	/// <param name="fov">カメラのFOVです。</param>
	private void OnFovNotified(float fov)
	{
		_labelFov.Text = fov.ToString("F1");
		_sliderFov.Value = fov;
	}

	/// <summary>
	/// カメラの投影タイプが通知されたときに呼び出されるイベントハンドラです。
	/// </summary>
	/// <param name="type">カメラの投影タイプです。</param>
	private void OnProjectionTypeNotified(Camera3D.ProjectionType type)
	{
		_labelProjection.Text = type.ToString();
	}

	#endregion
}



