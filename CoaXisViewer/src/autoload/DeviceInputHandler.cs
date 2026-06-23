using Godot;
using System;

/// <summary>
/// ユーザーのキーボードやコントローラー入力を処理するノードです。Autoloadに登録され、常にシーンツリーに存在します。
/// </summary>
public partial class DeviceInputHandler : Node
{
	#region Fields

	[ExportGroup("Settings")]
	[Export] private float _translateSpeed = 8.0f;
	[Export] private float _rotateSpeedDegrees = 90.0f;
	[Export] private float _rollSpeedDegrees = 120.0f;

	private static DeviceInputHandler I;

	#endregion

	#region Lifecycle

	public override void _Ready()
	{
		I = this;
	}

	public override void _Process(double delta)
	{
		// マルチセレクトモードの状態を毎フレーム更新して通知
		Selection.IsMultiSelectMode = Input.IsActionPressed("select_multiple");

		float dt = (float)delta;
		HandleTranslationInput(dt);
		HandleRotationInput(dt);
	}

	#endregion

	#region Internal Helpers

	/// <summary>
	/// ユーザーの入力に基づいてカメラの平行移動をリクエストします。
	/// </summary>
	/// <param name="delta">前のフレームからの経過時間（秒）</param>
	private static void HandleTranslationInput(float delta)
	{
		float x = GetAxis("translate_camera_left", "translate_camera_right");
		float y = GetAxis("translate_camera_down", "translate_camera_up");
		float z = GetAxis("translate_camera_forward", "translate_camera_backward");

		Vector3 translationDirection = new Vector3(x, y, z);
		if (translationDirection.LengthSquared() <= Mathf.Epsilon)
		{
			return;
		}

		if (translationDirection.LengthSquared() > 1.0f)
		{
			translationDirection = translationDirection.Normalized();
		}

		Vector3 translation = translationDirection * (I._translateSpeed * delta);
		ViewportEventHub.RequestTranslate(translation, SpaceMode.Camera);
	}

	/// <summary>
	/// ユーザーの入力に基づいてカメラの回転をリクエストします。
	/// </summary>
	/// <param name="delta">前のフレームからの経過時間（秒）</param>
	private static void HandleRotationInput(float delta)
	{
		float yawInput = GetAxis("rotate_camera_right", "rotate_camera_left");
		float pitchInput = GetAxis("rotate_camera_down", "rotate_camera_up");
		float rollInput = GetAxis("rotate_camera_clockwise", "rotate_camera_counterclockwise");

		if (Mathf.IsZeroApprox(yawInput) && Mathf.IsZeroApprox(pitchInput) && Mathf.IsZeroApprox(rollInput))
		{
			return;
		}

		float yawAngle = Mathf.DegToRad(yawInput * I._rotateSpeedDegrees * delta);
		float pitchAngle = Mathf.DegToRad(pitchInput * I._rotateSpeedDegrees * delta);
		float rollAngle = Mathf.DegToRad(rollInput * I._rollSpeedDegrees * delta);
		Quaternion yaw = new Quaternion(Vector3.Up, yawAngle);
		Quaternion pitch = new Quaternion(Vector3.Right, pitchAngle);
		Quaternion roll = new Quaternion(Vector3.Forward, rollAngle);
		Quaternion rotation = yaw * pitch * roll;

		ViewportEventHub.RequestRotate(rotation, SpaceMode.Camera);
	}

	/// <summary>
	/// 指定されたアクションに基づいて軸の値を取得します。
	/// </summary>
	/// <param name="negativeAction">負の方向のアクション名</param>
	/// <param name="positiveAction">正の方向のアクション名</param>
	/// <returns>軸の値（-1.0から1.0の範囲）</returns>
	private static float GetAxis(string negativeAction, string positiveAction)
	{
		return Input.GetActionStrength(positiveAction) - Input.GetActionStrength(negativeAction);
	}

	#endregion
}
