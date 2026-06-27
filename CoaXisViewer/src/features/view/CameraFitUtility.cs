using Godot;
using System.Collections.Generic;

/// <summary>
/// カメラフィット用の AABB 計算を担うヘルパー
/// </summary>
public static class CameraFitUtility
{
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

    // フィット対象はモデル配下の MeshInstance3D のみとし、見た目上の境界を安定して算出する
    private static bool TryGetWorldAabb(Node node, out Aabb aabb)
    {
        aabb = default;
        bool hasMeshAabb = false;
        GetWorldAabbRecursive(node, ref aabb, ref hasMeshAabb);
        return hasMeshAabb;
    }

    private static void GetWorldAabbRecursive(Node node, ref Aabb aabb, ref bool hasMeshAabb)
    {
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
                aabb = MergeAabb(aabb, meshAabb);
            }
        }

        foreach (Node child in node.GetChildren())
        {
            GetWorldAabbRecursive(child, ref aabb, ref hasMeshAabb);
        }
    }

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

    private static Aabb MergeAabb(Aabb a, Aabb b)
    {
        Vector3 aMax = a.Position + a.Size;
        Vector3 bMax = b.Position + b.Size;
        Vector3 min = new Vector3(
            Mathf.Min(a.Position.X, b.Position.X),
            Mathf.Min(a.Position.Y, b.Position.Y),
            Mathf.Min(a.Position.Z, b.Position.Z));
        Vector3 max = new Vector3(
            Mathf.Max(aMax.X, bMax.X),
            Mathf.Max(aMax.Y, bMax.Y),
            Mathf.Max(aMax.Z, bMax.Z));

        return new Aabb(min, max - min);
    }
}
