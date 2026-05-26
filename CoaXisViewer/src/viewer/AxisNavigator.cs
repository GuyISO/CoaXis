using Godot;
using System;
using System.Globalization;

public partial class AxisNavigator : Control
{
	
	[Export] private CameraController _cameraController;

	private SubViewportContainer _subViewportContainer;
	private SubViewport _subViewport;

	private Node3D _focalPoint;
	private Camera3D _camera;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_cameraController ??= (CameraController)GetNode("/root/Main/CameraController");

		_focalPoint = (Node3D)FindChild("FocalPoint");
		_camera = (Camera3D)_focalPoint.GetNode("Camera3D");

		_subViewportContainer = (SubViewportContainer)FindChild("SubViewportContainer");
		_subViewport = (SubViewport)_subViewportContainer.GetNode("SubViewport");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		_focalPoint.Rotation = _cameraController.FocalPoint.Rotation;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
		{
			var localMouse = mb.Position - _subViewportContainer.GlobalPosition;
			if (!new Rect2(Vector2.Zero, _subViewportContainer.Size).HasPoint(localMouse))
			{
				return;
			}

			var subViewportMouse = localMouse;
			if (_subViewportContainer.Size.X > 0 && _subViewportContainer.Size.Y > 0)
			{
				subViewportMouse = localMouse * ((Vector2)_subViewport.Size / _subViewportContainer.Size);
			}
			
			// マウス位置からレイを飛ばす
			var from = _camera.ProjectRayOrigin(subViewportMouse);
			var to = from + _camera.ProjectRayNormal(subViewportMouse) * 10f;

			var space = _camera.GetWorld3D().DirectSpaceState;
			var query = PhysicsRayQueryParameters3D.Create(from, to);
			query.CollisionMask = 1u << 1; // mask 2 only

			var result = space.IntersectRay(query);

			if (result.Count > 0)
			{
				var collider = (Node3D)result["collider"];
				OnColliderClicked(collider);
			}

			// イベントを消費して、他のノードに伝播しないようにする
			@event.Dispose();
		}
	}

	private void OnColliderClicked(Node3D node)
	{
		if (TryParseRotationDegreesFromName(node.Name, out Vector3 rotationDegrees))
		{
			// オイラー角をdegからradに変換し、クォータニオンに変換
			var quaternion = Quaternion.FromEuler(rotationDegrees * (Mathf.Pi / 180f));
			_cameraController.MoveFocalPoint(null, quaternion, true);
		}

	}

	private static bool TryParseRotationDegreesFromName(string name, out Vector3 rotationDegrees)
	{
		// ノード名が "x, y, z" の形式で回転角度を表していると仮定して、そこから回転角度をパースし、Quaternionに変換する
		rotationDegrees = Vector3.Zero;
		var parts = name.Split(',');
		if (parts.Length != 3)
		{
			return false;
		}

		if (!float.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var x))
		{
			return false;
		}

		if (!float.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var y))
		{
			return false;
		}

		if (!float.TryParse(parts[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var z))
		{
			return false;
		}

		rotationDegrees = new Vector3(x, y, z);
		return true;
	}

}
