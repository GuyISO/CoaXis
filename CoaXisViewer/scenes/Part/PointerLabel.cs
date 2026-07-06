using Godot;
using System;

/// <summary>
/// 3D空間上に注記などを表示するためのラベル
/// </summary>
public partial class PointerLabel : Node3D
{
	#region Fields

	// 関連ノードのキャッシュ
	private Node3D _components;
	private MeshInstance3D _pointer;
	private Node3D _labels;
	private Label3D _labelLeft;
	private Label3D _labelRight;

	[Export]
	private float _rotationSpeedDegPerSec = 90.0f;

	#endregion

	#region Lifecycle

	public override void _Ready()
	{
		// 関連ノードのキャッシュ
		_components = GetNode<Node3D>("Components");
		_pointer = _components.GetNode<MeshInstance3D>("Pointer");
		_labels = _components.GetNode<Node3D>("Labels");
		_labelLeft = _labels.GetNode<Label3D>("LabelLeft");
		_labelRight = _labels.GetNode<Label3D>("LabelRight");
	}

	public override void _Process(double delta)
	{
		_components.RotateZ(Mathf.DegToRad(_rotationSpeedDegPerSec) * (float)delta);
	}

	#endregion

	#region Public Methods

	/// <summary>
	/// ポインタの色を設定する
	/// </summary>
	/// <param name="color"></param>
	public void SetPointerColor(Color color)
	{
		_pointer.MaterialOverride = new StandardMaterial3D()
		{
			AlbedoColor = color
		};
	}

	/// <summary>
	/// ラベルのテキストを設定する
	/// </summary>
	/// <param name="text"></param>
	public void SetText(string text)
	{
		_labelLeft.Text = text;
		_labelRight.Text = text;
	}

	/// <summary>
	/// ラベルの色を設定する
	/// </summary>
	/// <param name="color"></param>
	public void SetTextColor(Color color)
	{
		_labelLeft.Modulate = color;
		_labelRight.Modulate = color;
	}

	#endregion
}
