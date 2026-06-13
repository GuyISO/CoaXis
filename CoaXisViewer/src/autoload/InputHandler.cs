using Godot;

public partial class InputHandler : Node
{
	#region Fields

	[ExportGroup("Settings")]
	[Export] private float _translateSpeed = 8.0f;
	[Export] private float _rotateSpeedDegrees = 90.0f;
	[Export] private float _rollSpeedDegrees = 120.0f;

	#endregion

	#region Lifecycle

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (CameraEventHub.I == null)
		{
			return;
		}

		float dt = (float)delta;
		HandleTranslationInput(dt);
		HandleRotationInput(dt);
	}

	#endregion

	#region Internal Helpers

	private void HandleTranslationInput(float delta)
	{
		float x = GetAxis("camera_translate_left", "camera_translate_right");
		float y = GetAxis("camera_translate_down", "camera_translate_up");
		float z = GetAxis("camera_translate_forward", "camera_translate_backward");

		Vector3 translationDirection = new Vector3(x, y, z);
		if (translationDirection.LengthSquared() <= Mathf.Epsilon)
		{
			return;
		}

		if (translationDirection.LengthSquared() > 1.0f)
		{
			translationDirection = translationDirection.Normalized();
		}

		Vector3 translation = translationDirection * (_translateSpeed * delta);
		CameraEventHub.I.RequestTranslate(translation, SpaceMode.Camera);
	}

	private void HandleRotationInput(float delta)
	{
		float yawInput = GetAxis("camera_rotate_right", "camera_rotate_left");
		float pitchInput = GetAxis("camera_rotate_down", "camera_rotate_up");
		float rollInput = GetAxis("camera_rotate_clockwise", "camera_rotate_counterclockwise");

		if (Mathf.IsZeroApprox(yawInput) && Mathf.IsZeroApprox(pitchInput) && Mathf.IsZeroApprox(rollInput))
		{
			return;
		}

		float yawAngle = Mathf.DegToRad(yawInput * _rotateSpeedDegrees * delta);
		float pitchAngle = Mathf.DegToRad(pitchInput * _rotateSpeedDegrees * delta);
		float rollAngle = Mathf.DegToRad(rollInput * _rollSpeedDegrees * delta);
		Quaternion yaw = new Quaternion(Vector3.Up, yawAngle);
		Quaternion pitch = new Quaternion(Vector3.Right, pitchAngle);
		Quaternion roll = new Quaternion(Vector3.Forward, rollAngle);
		Quaternion rotation = yaw * pitch * roll;

		CameraEventHub.I.RequestRotate(rotation, SpaceMode.Camera);
	}

	private static float GetAxis(string negativeAction, string positiveAction)
	{
		return Input.GetActionStrength(positiveAction) - Input.GetActionStrength(negativeAction);
	}

	#endregion
}
