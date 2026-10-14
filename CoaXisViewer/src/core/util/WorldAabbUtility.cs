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

            if (!TryGetWorldAabb(node, out Aabb nodeAabb))
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
    private static bool TryGetWorldAabb(Node node, out Aabb aabb)
    {
        aabb = default;
        bool hasMeshAabb = false;
        GetWorldAabbRecursive(node, ref aabb, ref hasMeshAabb);
        return hasMeshAabb;
    }

    /// <summary>
    /// ノードの階層を再帰的に探索してワールド AABB を取得する
    /// </summary>
    /// <param name="node">対象ノード</param>
    /// <param name="aabb">取得した AABB</param>
    /// <param name="hasMeshAabb">有効な MeshInstance3D が存在するかどうか</param>
    private static void GetWorldAabbRecursive(Node node, ref Aabb aabb, ref bool hasMeshAabb)
    {
        if (node is Node3D node3D && !node3D.IsVisibleInTree())
        {
            return;
        }

        if (node is MeshInstance3D meshInstance && meshInstance.Mesh != null)
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
            GetWorldAabbRecursive(child, ref aabb, ref hasMeshAabb);
        }
    }

    #endregion
}
