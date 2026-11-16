using Godot;
using System.Collections.Generic;

/// <summary>
/// カメラフィット用の AABB 計算を担うヘルパー
/// </summary>
public static class WorldAabbUtility
{
    #region Methods

    /// <summary>
    /// 指定ノード群からワールド空間の AABB を合算して取得する
    /// </summary>
    /// <param name="nodes">AABB を取得する対象ノード群</param>
    /// <param name="aabb">合算されたワールド AABB </param>
    /// <returns>有効な MeshInstance3D が存在した場合は true を返す</returns>
    public static bool TryGetWorldAabb(IEnumerable<Node3D> nodes, out Aabb aabb)
    {
        return TryGetWorldAabb(nodes, uint.MaxValue, out aabb);
    }

    /// <summary>
    /// 指定ノード群から指定レイヤーに属するメッシュのワールド空間 AABB を合算して取得する。
    /// </summary>
    /// <param name="nodes">AABB を取得する対象ノード群</param>
    /// <param name="layerMask">対象に含めるレイヤーのビットマスク</param>
    /// <param name="aabb">合算されたワールド AABB</param>
    /// <returns>指定レイヤーに属する有効な MeshInstance3D が存在した場合は true</returns>
    public static bool TryGetWorldAabb(IEnumerable<Node3D> nodes, uint layerMask, out Aabb aabb)
    {
        aabb = default;
        bool hasMeshAabb = false;

        if (nodes == null)
        {
            return false;
        }

        foreach (Node3D node in nodes)
        {
            if (node == null)
            {
                continue;
            }

            if (!TryGetWorldAabb(node, layerMask, out Aabb nodeAabb))
            {
                continue;
            }

            aabb = hasMeshAabb ? aabb.Merge(nodeAabb) : nodeAabb;
            hasMeshAabb = true;
        }

        return hasMeshAabb;
    }

    #endregion

    #region Helpers

    /// <summary>
    /// AABB の 8 コーナーをワールド座標に変換して返す
    /// </summary>
    /// <param name="node">対象ノード</param>
    /// <param name="aabb">対象 AABB</param>
    private static bool TryGetWorldAabb(Node node, uint layerMask, out Aabb aabb)
    {
        aabb = default;
        bool hasMeshAabb = false;
        GetWorldAabbRecursive(node, layerMask, ref aabb, ref hasMeshAabb);
        return hasMeshAabb;
    }

    /// <summary>
    /// ノードの階層を再帰的に探索してワールド AABB を取得する
    /// </summary>
    /// <param name="node">対象ノード</param>
    /// <param name="layerMask">対象に含めるレイヤーのビットマスク</param>
    /// <param name="aabb">取得した AABB</param>
    /// <param name="hasMeshAabb">有効な MeshInstance3D が存在するかどうか</param>
    private static void GetWorldAabbRecursive(Node node, uint layerMask, ref Aabb aabb, ref bool hasMeshAabb)
    {
        if (node is Node3D node3D && !node3D.IsVisibleInTree())
        {
            return;
        }

        if (node is MeshInstance3D meshInstance &&
            meshInstance.Mesh != null &&
            (meshInstance.Layers & layerMask) != 0)
        {
            // Transform3D * Aabb は 8 コーナー変換と再構築を Godot 側で行う
            Aabb meshAabb = meshInstance.GlobalTransform * meshInstance.Mesh.GetAabb();
            if (!hasMeshAabb)
            {
                aabb = meshAabb;
                hasMeshAabb = true;
            }
            else
            {
                aabb = aabb.Merge(meshAabb);
            }
        }

        foreach (Node child in node.GetChildren())
        {
            GetWorldAabbRecursive(child, layerMask, ref aabb, ref hasMeshAabb);
        }
    }

    #endregion
}
