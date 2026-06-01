using Godot;
using System.Globalization;

/// <summary>
/// 画面隅の軸ナビゲータを制御し、クリックでカメラ向きを変更します。
/// </summary>
public partial class AxisNavigator : Control
{
	#region Fields

	[Export] private CameraController _cameraController;

	private SubViewportContainer _subViewportContainer = null!;
	private SubViewport _subViewport = null!;

	private Node3D _focalPoint = null!;
	private Camera3D _camera = null!;

	#endregion

	#region Lifecycle

	/// <summary>
	/// 依存ノードを解決して初期化します。
	/// </summary>
	public override void _Ready()
	{
		_cameraController = ResolveCameraController();

		_focalPoint = GetNodeOrNull<Node3D>("FocalPoint");
		_camera = _focalPoint?.GetNodeOrNull<Camera3D>("Camera3D");
		_subViewportContainer = FindChild("SubViewportContainer") as SubViewportContainer;
		_subViewport = _subViewportContainer?.GetNodeOrNull<SubViewport>("SubViewport");

		if (_cameraController == null || _focalPoint == null || _camera == null || _subViewportContainer == null || _subViewport == null)
		{
			GD.PushWarning("AxisNavigator: required nodes are missing. Axis navigation is disabled.");
		}
	}

	/// <summary>
	/// CameraController の回転を軸ナビゲータに同期します。
	/// </summary>
	/// <param name="delta">前フレームからの経過秒。</param>
	public override void _Process(double delta)
	{
		if (_cameraController == null || _focalPoint == null)
		{
			return;
		}

		_focalPoint.Rotation = _cameraController.FocalPoint.Rotation;
	}

	#endregion

	#region Events

	/// <summary>
	/// 軸ナビゲータのクリックを検知し、対応する向きへカメラを移動します。
	/// </summary>
	/// <param name="@event">未処理入力イベント。</param>
	public override void _UnhandledInput(InputEvent @event)
	{
		if (_subViewportContainer == null || _subViewport == null || _camera == null)
		{
			return;
		}

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

			// クリックを軸ナビゲータで処理したことを示し、他ノードへの誤伝播を防ぐ。
			GetViewport().SetInputAsHandled();
		}
	}

	#endregion

	#region Internal Helpers

	// クリック対象ノード名を角度定義として解釈し、対応する姿勢へカメラを回転させる。
	private void OnColliderClicked(Node3D node)
	{
		if (TryParseRotationDegreesFromName(node.Name, out Vector3 rotationDegrees))
		{
			// オイラー角をdegからradに変換し、クォータニオンに変換
			var quaternion = Quaternion.FromEuler(rotationDegrees * (Mathf.Pi / 180f));
			_cameraController.MoveFocalPoint(null, quaternion, true);
		}

	}

	// Export 未設定でも動作するよう、相対パス・絶対パス・全体探索の順で CameraController を解決する。
	private CameraController ResolveCameraController()
	{
		if (_cameraController != null)
		{
			return _cameraController;
		}

		CameraController controller = GetNodeOrNull<CameraController>("../SubViewportContainer/SubViewport/CameraController");
		if (controller != null)
		{
			return controller;
		}

		controller = GetNodeOrNull<CameraController>("/root/Main/Canvas/VBoxContainer/HBoxContainer/MainScreen/SubViewportContainer/SubViewport/CameraController");
		if (controller != null)
		{
			return controller;
		}

		return GetTree()?.Root?.FindChild("CameraController", true, false) as CameraController;
	}

	// ノード名の "x, y, z" 表記を回転角（度）として解釈する。
	private static bool TryParseRotationDegreesFromName(string name, out Vector3 rotationDegrees)
	{
		// ノード名が "x, y, z" 形式の回転角度を表す想定で、Quaternion に変換する。
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

	#endregion

}



