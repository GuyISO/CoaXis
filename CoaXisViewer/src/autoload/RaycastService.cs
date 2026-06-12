using Godot;
using System;

public partial class RaycastService : Node
{
	/// <summary>
	/// 指定したカメラからスクリーン座標に向けてレイキャストを行い、ヒット情報を返します。
	/// </summary>
	/// <param name="camera">レイを発射するカメラ。</param>
	/// <param name="screenPos">レイのスクリーン座標。</param>
	/// <param name="collisionMask">レイキャストの衝突マスク。デフォルトはカメラのカリングマスク。</param>
	/// <returns>レイのヒット情報を含む RaycastHitInfo 構造体。</returns>
	public static RaycastHitInfo RaycastFromScreen(Camera3D camera, Vector2 screenPos, uint? collisionMask = null)
	{
		var origin = camera.ProjectRayOrigin(screenPos);
		var dir = camera.ProjectRayNormal(screenPos).Normalized();
		var end = origin + dir * camera.Far;

		var space = camera.GetWorld3D().DirectSpaceState;
		var query = PhysicsRayQueryParameters3D.Create(origin, end);
		query.CollisionMask = collisionMask ?? camera.CullMask; // カメラのカリングマスクを使用して衝突マスクを設定
		var result = space.IntersectRay(query);

		if (result.Count == 0)
			return new RaycastHitInfo { HasHit = false };

		return new RaycastHitInfo
		{
			HasHit = true,
			Position = (Vector3)result["position"],
			Normal = result.ContainsKey("normal") ? (Vector3)result["normal"] : Vector3.Zero,
			Collider = result.ContainsKey("collider") ? (Node3D)result["collider"] : null,
			Distance = origin.DistanceTo((Vector3)result["position"])
		};
	}
}