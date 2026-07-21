using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;

/// <summary>
/// レイキャストや形状クエリを行うためのユーティリティ
/// </summary>
public static class PickUtility
{
    #region Public Methods

    /// <summary>
    /// 指定したカメラからスクリーン座標に向けてレイキャストを行い、ヒット情報を返す
    /// </summary>
    /// <param name="camera">レイを発射するカメラ</param>
    /// <param name="screenPosition">レイのスクリーン座標</param>
    /// <param name="collisionMask">レイキャストの衝突マスク、デフォルトはカメラのカリングマスク</param>
    /// <param name="excludeRids">レイキャストから除外するオブジェクトのRIDリスト</param>
    /// <returns>レイのヒット情報を含む PickResult 構造体</returns>
    public static PickResult PickByRay(Camera3D camera, Vector2 screenPosition, uint? collisionMask = null, List<Rid> excludeRids = null)
    {
        var origin = camera.ProjectRayOrigin(screenPosition);
        var dir = camera.ProjectRayNormal(screenPosition).Normalized();
        var end = origin + dir * camera.Far;

        var space = camera.GetWorld3D().DirectSpaceState;
        var query = PhysicsRayQueryParameters3D.Create(origin, end);
        query.CollisionMask = collisionMask ?? camera.CullMask; // カメラのカリングマスクを使用して衝突マスクを設定
        if (excludeRids != null)
        {
            query.Exclude = new Array<Rid>(excludeRids);
        }
        var result = space.IntersectRay(query);

        if (result.Count == 0)
            return new PickResult();

        return new PickResult(
            hasHit: true,
            collider: result.ContainsKey("collider") ? (Node3D)result["collider"] : null,
            rid: result.ContainsKey("rid") ? (Rid)result["rid"] : default,
            model: result.ContainsKey("collider") ? ((Node3D)result["collider"]).GetParentOrNull<AnyModel>() : null,
            position: (Vector3)result["position"],
            normal: result.ContainsKey("normal") ? (Vector3)result["normal"] : Vector3.Zero,
            distance: origin.DistanceTo((Vector3)result["position"])
        );
    }

    /// <summary>
    /// 指定したカメラからスクリーン座標に向けてレイキャストを行い、すべてのヒット情報をリストで返す
    /// </summary>
    /// <param name="camera">レイを発射するカメラ</param>
    /// <param name="screenPosition">レイのスクリーン座標</param>
    /// <param name="collisionMask">レイキャストの衝突マスク、デフォルトはカメラのカリングマスク</param>
    /// <param name="excludeRids">レイキャストから除外するオブジェクトのRIDリスト</param>
    /// <returns>レイのヒット情報を含む PickResult の配列</returns>
    /// <remarks>Godotには貫通レイキャストがないため単体レイキャストを繰り返し呼び出し、すべてのヒットを収集することで実装している</remarks>
    public static PickResult[] PickAllByRay(Camera3D camera, Vector2 screenPosition, uint? collisionMask = null, List<Rid> excludeRids = null)
    {
        // 引数で受け取った除外リストに、レイキャスト情報を取得するたびに除外品目を追加していくため複製して使用
        var exclude = excludeRids != null
            ? new List<Rid>(excludeRids)
            : new List<Rid>();

        var results = new List<PickResult>();

        while (true)
        {
            PickResult result = PickByRay(camera, screenPosition, collisionMask, exclude);

            if (!result.HasHit)
            {
                break; // ヒットがなくなったら終了
            }

            results.Add(result);

            // 次のレイキャストでこのヒットを除外
            if (result.Rid.IsValid)
                exclude.Add(result.Rid);
            else
                break; // 除外できない謎のものにヒットしたなら無限ループ防止のため終了
        }

        // 距離順で取得されているはずだが、念のためヒット距離でソート
        results.Sort((a, b) => a.Distance.CompareTo(b.Distance));

        return results.ToArray();
    }

    /// <summary>
    /// 指定したカメラから、指定した形状を使用して空間内のオブジェクトを取得する
    /// </summary>
    /// <param name="camera">形状クエリを行うカメラ</param>
    /// <param name="shape">使用する形状、ワールド座標系での配置を想定</param>
    /// <param name="requireFullContainment">形状に完全内包されたオブジェクトのみを取得するかどうか、true の場合は完全内包のみをヒットとみなし false の場合は形状と交差していればヒットとみなす</param>
    /// <param name="collisionMask">クエリの衝突マスク、デフォルトはカメラのカリングマスク</param>
    /// <param name="excludeRids">クエリから除外するオブジェクトのRIDリスト</param>
    /// <returns>クエリのヒット情報を含む PickResult の配列</returns>
    public static PickResult[] PickByShape(Camera3D camera, Shape3D shape, bool requireFullContainment, uint? collisionMask = null, List<Rid> excludeRids = null)
    {
        var space = camera.GetWorld3D().DirectSpaceState;

        var query = new PhysicsShapeQueryParameters3D
        {
            Shape = shape,
            Transform = Transform3D.Identity, // 形状のローカル原点をワールド空間のどこに配置するか、例えば、矩形選択の場合は、カメラの位置と向きに基づいて形状を配置するための Transform3D を使用する
            CollisionMask = collisionMask ?? camera.CullMask
        };

        if (excludeRids != null)
        {
            query.Exclude = new Array<Rid>(excludeRids);
        }
        var results = space.IntersectShape(query);

        // Godot の IntersectShape はヒット位置や法線などの詳細情報を返さないため、ノード参照のみを PickResult に格納する
        // 指定したShapeと交差したオブジェクトがすべて取得される
        var pickResults = new List<PickResult>();
        foreach (var result in results)
        {
            pickResults.Add(
                new PickResult(
                    hasHit: true,
                    collider: result.ContainsKey("collider") ? (Node3D)result["collider"] : null,
                    rid: result.ContainsKey("rid") ? (Rid)result["rid"] : default,
                    model: result.ContainsKey("collider") ? ((Node3D)result["collider"]).GetParentOrNull<AnyModel>() : null,
                    position: Vector3.Zero, // IntersectShape は position を返さない
                    normal: Vector3.Zero,
                    distance: 0f
                )
            );
        }

        // requireFullContainment が true の場合、さらにフィルタリングして完全に内包されているオブジェクトのみを残す
        if (requireFullContainment)
        {
            // TODO: なんか難しくて未実装なので、いつか実装したい
        }

        return pickResults.ToArray();
    }

    /// <summary>
    /// AnyModel のみがわかっている状態から PickResult を生成する
    /// </summary>
    /// <param name="model">選択対象のモデル</param>
    /// <returns>モデル情報を含む PickResult。位置・法線・距離は未設定のため Zero/0 を返す</returns>
    public static PickResult PickByModel(AnyModel model)
    {
        if (model == null)
        {
            return new PickResult();
        }

        Node3D collider = null;
        Rid rid = default;

        if (model.Collider != null && GodotObject.IsInstanceValid(model.Collider))
        {
            collider = model.Collider;
            rid = model.Collider.GetRid();
        }

        return new PickResult(
            hasHit: true,
            collider: collider,
            rid: rid,
            model: model,
            position: Vector3.Zero,
            normal: Vector3.Zero,
            distance: 0f
        );
    }

    #endregion
}