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
        query.HitBackFaces = true; // 背面のメッシュもヒットさせるために true に設定
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
            entityId: result.ContainsKey("collider") ? GetParentEntityId((Node3D)result["collider"]) : Guid.Empty,
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
    /// <param name="collisionMask">クエリの衝突マスク、デフォルトはカメラのカリングマスク</param>
    /// <param name="excludeRids">クエリから除外するオブジェクトのRIDリスト</param>
    /// <returns>クエリのヒット情報を含む PickResult の配列</returns>
    /// <remarks>同一モデルに複数のコライダーがある場合は、最初に取得した結果だけを返す。形状クエリが位置・法線を返さない制約は維持する。</remarks>
    public static PickResult[] PickByShape(Camera3D camera, Shape3D shape, uint? collisionMask = null, List<Rid> excludeRids = null)
    {
        var space = camera.GetWorld3D().DirectSpaceState;
        var exclude = excludeRids != null
            ? new List<Rid>(excludeRids)
            : new List<Rid>();

        var pickResults = new List<PickResult>();
        var selectedEntityIds = new HashSet<Guid>();
        const int batchSize = 32;

        // Godot の IntersectShape はヒット位置や法線などの詳細情報を返さないため、ノード参照のみを PickResult に格納する
        // 1回のクエリで複数件を取得し、物理クエリの呼び出し回数と除外配列の再構築回数を減らす

        while (true)
        {
            var query = new PhysicsShapeQueryParameters3D
            {
                Shape = shape,
                Transform = Transform3D.Identity, // 形状のローカル原点をワールド空間のどこに配置するか、例えば、矩形選択の場合は、カメラの位置と向きに基づいて形状を配置するための Transform3D を使用する
                CollisionMask = collisionMask ?? camera.CullMask,
                CollideWithBodies = true,
                CollideWithAreas = true
            };

            if (exclude.Count > 0)
            {
                query.Exclude = new Array<Rid>(exclude);
            }

            // 上限件数に達した場合は未取得のヒットが残る可能性があるため、RIDを除外して次のバッチを取得する
            var results = space.IntersectShape(query, batchSize);
            if (results.Count == 0)
            {
                break;
            }

            bool hasInvalidRid = false;
            foreach (var result in results)
            {
                var collider = result.ContainsKey("collider") ? (Node3D)result["collider"] : null;
                var rid = result.ContainsKey("rid") ? (Rid)result["rid"] : default;

                if (!rid.IsValid && collider is CollisionObject3D collisionObject)
                {
                    rid = collisionObject.GetRid();
                }

                if (rid.IsValid)
                {
                    exclude.Add(rid);
                }
                else
                {
                    // 除外できない結果を次回も取得すると無限ループになるため、このバッチで終了する
                    hasInvalidRid = true;
                }

                Guid entityId = collider != null ? GetParentEntityId(collider) : Guid.Empty;
                if (entityId != Guid.Empty && !selectedEntityIds.Add(entityId))
                {
                    continue;
                }

                pickResults.Add(
                    new PickResult(
                        hasHit: true,
                        collider: collider,
                        rid: rid,
                        entityId: entityId,
                        position: Vector3.Zero, // IntersectShape は position を返さない
                        normal: Vector3.Zero,
                        distance: 0f
                    )
                );
            }

            if (hasInvalidRid || results.Count < batchSize)
            {
                break;
            }
        }

        return pickResults.ToArray();
    }

    /// <summary>
    /// ModelNode のみがわかっている状態から PickResult を生成する
    /// </summary>
    /// <param name="modelNode">選択対象のモデル</param>
    /// <returns>モデル情報を含む PickResult。位置・法線・距離は未設定のため Zero/0 を返す</returns>
    public static PickResult PickByModel(ModelNode modelNode)
    {
        if (modelNode == null)
        {
            return new PickResult();
        }

        // コライダーはシーン(scn/tscn)側が保持するため、ModelComponents 経由では取得できない
        return new PickResult(
            hasHit: false,
            collider: null,
            rid: default,
            entityId: modelNode.EntityId,
            position: Vector3.Zero,
            normal: Vector3.Zero,
            distance: 0f
        );
    }

    /// <summary>
    /// EntityId のみがわかっている状態から PickResult を生成する
    /// </summary>
    /// <param name="entityId">選択対象のモデル実体識別子</param>
    /// <returns>モデル情報を含む PickResult。対応する ModelNode が見つからない場合はヒットなしを返す</returns>
    public static PickResult PickByEntityId(Guid entityId)
    {
        ModelNode modelNode = Application.Model.Registry.GetEntity(entityId)?.Node;
        return PickByModel(modelNode);
    }

    /// <summary>
    /// ModelNode 群のみがわかっている状態から PickResult 配列を生成する
    /// </summary>
    /// <param name="modelNodes">選択対象のモデル配列</param>
    /// <returns>モデル情報を含む PickResult の配列</returns>
    public static PickResult[] PickByModels(IReadOnlyList<ModelNode> modelNodes)
    {
        if (modelNodes == null || modelNodes.Count == 0)
        {
            return System.Array.Empty<PickResult>();
        }

        var results = new List<PickResult>(modelNodes.Count);
        foreach (ModelNode modelNode in modelNodes)
        {
            if (modelNode == null)
            {
                continue;
            }

            results.Add(PickByModel(modelNode));
        }

        return results.ToArray();
    }

    /// <summary>
    /// EntityId 群のみがわかっている状態から PickResult 配列を生成する
    /// </summary>
    /// <param name="entityIds">選択対象のモデル実体識別子配列</param>
    /// <returns>モデル情報を含む PickResult の配列</returns>
    public static PickResult[] PickByEntityIds(IReadOnlyList<Guid> entityIds)
    {
        if (entityIds == null || entityIds.Count == 0)
        {
            return System.Array.Empty<PickResult>();
        }

        var results = new List<PickResult>(entityIds.Count);
        foreach (Guid entityId in entityIds)
        {
            if (entityId == Guid.Empty)
            {
                continue;
            }

            results.Add(PickByEntityId(entityId));
        }

        return results.ToArray();
    }

    #endregion

    #region Internal Helpers

    /// <summary>
    /// コライダーの親階層から EntityId を取得する
    /// </summary>
    /// <param name="node">取得対象のノードまたはコライダー</param>
    /// <returns>見つかった場合はそのモデルの EntityId、見つからない場合は Guid.Empty</returns>
    public static Guid GetParentEntityId(Node node)
    {
        Node current = node;

        while (current != null)
        {
            if (current is ModelNode modelNode)
            {
                return modelNode.EntityId;
            }

            current = current.GetParent();
        }

        return Guid.Empty;
    }

    #endregion

}