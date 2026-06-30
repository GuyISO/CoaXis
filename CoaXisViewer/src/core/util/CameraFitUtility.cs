using Godot;
using System.Collections.Generic;

/// <summary>
/// カメラフィット用の AABB 計算を担うヘルパー
/// </summary>
public static class CameraFitUtility
{
    #region Public Methods

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

    /// <summary>
    /// AABB の 8 コーナーを返す
    /// </summary>
    /// <param name="aabb">対象 AABB </param>
    /// <returns>8 コーナーの配列</returns>
    public static Vector3[] GetAabbCorners(Aabb aabb)
    {
        Vector3 p = aabb.Position;
        Vector3 s = aabb.Size;
        return new[]
        {
            new Vector3(p.X, p.Y, p.Z),
            new Vector3(p.X + s.X, p.Y, p.Z),
            new Vector3(p.X, p.Y + s.Y, p.Z),
            new Vector3(p.X, p.Y, p.Z + s.Z),
            new Vector3(p.X + s.X, p.Y + s.Y, p.Z),
            new Vector3(p.X + s.X, p.Y, p.Z + s.Z),
            new Vector3(p.X, p.Y + s.Y, p.Z + s.Z),
            new Vector3(p.X + s.X, p.Y + s.Y, p.Z + s.Z)
        };
    }

    #endregion

    #region Internal Helpers

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
            Aabb meshAabb = BuildWorldAabb(meshInstance, meshInstance.Mesh.GetAabb());
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

    /// <summary>
    /// MeshInstance3D のローカル AABB をワールド AABB に変換する
    /// </summary>
    /// <param name="meshInstance">対象の MeshInstance3D</param>
    /// <param name="localAabb">対象のローカル AABB</param>
    /// <returns>ワールド AABB</returns>
    private static Aabb BuildWorldAabb(MeshInstance3D meshInstance, Aabb localAabb)
    {
        Vector3 min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        Vector3 max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

        foreach (Vector3 corner in GetAabbCorners(localAabb))
        {
            Vector3 world = meshInstance.ToGlobal(corner);
            min = new Vector3(Mathf.Min(min.X, world.X), Mathf.Min(min.Y, world.Y), Mathf.Min(min.Z, world.Z));
            max = new Vector3(Mathf.Max(max.X, world.X), Mathf.Max(max.Y, world.Y), Mathf.Max(max.Z, world.Z));
        }

        return new Aabb(min, max - min);
    }

    #endregion
}
