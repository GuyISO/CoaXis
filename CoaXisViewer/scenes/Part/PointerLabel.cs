using Godot;
using System;

/// <summary>
/// 3D遨ｺ髢謎ｸ翫↓豕ｨ險倥↑縺ｩ繧定｡ｨ遉ｺ縺吶ｋ縺溘ａ縺ｮ繝ｩ繝吶Ν
/// </summary>
public partial class PointerLabel : Node3D
{
	#region Fields

	// 髢｢騾｣繝弱・繝峨・繧ｭ繝｣繝・す繝･
	private Node3D _components;
	private MeshInstance3D _pointer;
	private Node3D _labels;
	private Label3D _labelLeft;
	private Label3D _labelRight;

	private float _rotationSpeedDegPerSec;

	#endregion

	#region Lifecycle

	public override void _Ready()
	{
		// 髢｢騾｣繝弱・繝峨・繧ｭ繝｣繝・す繝･
		_components = GetNode<Node3D>("Components");
		_pointer = _components.GetNode<MeshInstance3D>("Pointer");
		_labels = _components.GetNode<Node3D>("Labels");
		_labelLeft = _labels.GetNode<Label3D>("LabelLeft");
		_labelRight = _labels.GetNode<Label3D>("LabelRight");

		Application.Setting.Event.SettingsNotified += ApplySettings;
		ApplySettings();
	}

	public override void _ExitTree()
	{
		Application.Setting.Event.SettingsNotified -= ApplySettings;

		base._ExitTree();
	}

	public override void _Process(double delta)
	{
		_components.RotateZ(Mathf.DegToRad(_rotationSpeedDegPerSec) * (float)delta);
	}

	#endregion

	#region Public Methods

	/// <summary>
	/// 險ｭ螳壼､繧貞渚譏縺吶ｋ
	/// </summary>
	private void ApplySettings()
	{
		_rotationSpeedDegPerSec = Application.Setting.Service.Current.Input.PointerRotationSpeedDeg;
	}

	/// <summary>
	/// 繝昴う繝ｳ繧ｿ縺ｮ濶ｲ繧定ｨｭ螳壹☆繧・	/// </summary>
	/// <param name="color"></param>
	public void SetPointerColor(Color color)
	{
		_pointer.MaterialOverride = new StandardMaterial3D()
		{
			AlbedoColor = color
		};
	}

	/// <summary>
	/// 繝ｩ繝吶Ν縺ｮ繝・く繧ｹ繝医ｒ險ｭ螳壹☆繧・	/// </summary>
	/// <param name="text"></param>
	public void SetText(string text)
	{
		_labelLeft.Text = text;
		_labelRight.Text = text;
	}

	/// <summary>
	/// 繝ｩ繝吶Ν縺ｮ濶ｲ繧定ｨｭ螳壹☆繧・	/// </summary>
	/// <param name="color"></param>
	public void SetTextColor(Color color)
	{
		_labelLeft.Modulate = color;
		_labelRight.Modulate = color;
	}

	/// <summary>
	/// 貂｡縺輔ｌ縺滓ｳ慕ｷ壽婿蜷代∈繝ｩ繝吶Ν縺ｮ蜷代″繧定ｨｭ螳壹☆繧・	/// </summary>
	/// <param name="normal">蜷代″險ｭ螳壹↓菴ｿ縺・ｳ慕ｷ壹・繧ｯ繝医Ν</param>
	public void SetOrientationFromNormal(Vector3 normal)
	{
		if (normal.LengthSquared() <= Mathf.Epsilon)
		{
			return;
		}

		Vector3 forward = normal.Normalized();
		Vector3 up = Vector3.Up;

		// LookAt 縺ｮ target 縺ｨ up 縺後⊇縺ｼ蟷ｳ陦後↑蝣ｴ蜷医∝挨霆ｸ繧・up 縺ｨ縺励※菴ｿ縺・・		if (Mathf.Abs(forward.Dot(up)) > 0.999f)
		{
			up = Vector3.Right;
		}

		LookAt(GlobalPosition + forward, up, true);
	}

	#endregion
}
