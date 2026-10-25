using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// ModelNode 配下のメッシュ表面から、面積に比例して点をサンプリングするサービス
/// </summary>
public static class MeshSurfaceSampler
{
    #region Methods

    /// <summary>
    /// ModelNode 配下（子 ModelNode は除く）の可視メッシュ表面を面積比でサンプリングし、ModelNode ローカル座標の点列を返す
    /// </summary>
    /// <remarks>
    /// 三角形を面積で重み付けして選び、選んだ三角形の内部を一様にサンプリングする。
    /// サンプリングのたびに異なる点列を生成する。
    /// </remarks>
    /// <param name="modelNode">サンプリング対象の ModelNode</param>
    /// <param name="minCount">最小点数</param>
    /// <param name="countPerArea">表面積(ModelNode ローカル単位の面積)1あたりの点数</param>
    /// <param name="maxCount">最大点数</param>
    /// <returns>サンプリングした点列、サンプリング可能な三角形が無い場合は空配列</returns>
    public static Vector3[] Sample(ModelNode modelNode, int minCount, double countPerArea, int maxCount)
    {
        if (modelNode == null)
        {
            throw new ArgumentNullException(nameof(modelNode));
        }

        var triangles = new List<Triangle>();
        double totalArea = 0d;
        CollectTriangles(modelNode, modelNode, modelNode.GlobalTransform.AffineInverse(), triangles, ref totalArea);
        if (triangles.Count == 0 || totalArea <= 0d || maxCount <= 0)
        {
            return Array.Empty<Vector3>();
        }

        int count = (int)Math.Clamp(totalArea * countPerArea, Math.Min(minCount, maxCount), maxCount);

        var random = new RandomNumberGenerator();
        random.Randomize();
        var points = new Vector3[count];
        for (int i = 0; i < count; i++)
        {
            Triangle triangle = triangles[FindTriangleIndex(triangles, random.Randf() * totalArea)];

            // sqrt を使うことで三角形内を一様に分布させる
            float root = MathF.Sqrt(random.Randf());
            float weightB = root * (1f - random.Randf());
            float weightC = root - weightB;
            points[i] = triangle.A * (1f - root) + triangle.B * weightB + triangle.C * weightC;
        }

        return points;
    }

    #endregion

    #region Helpers

    /// <summary>
    /// 累積面積に基づいて、サンプリング対象となる三角形のインデックスを二分探索する
    /// </summary>
    private static int FindTriangleIndex(List<Triangle> triangles, double sampledArea)
    {
        int low = 0;
        int high = triangles.Count - 1;
        while (low < high)
        {
            int middle = low + (high - low) / 2;
            if (triangles[middle].CumulativeArea <= sampledArea)
            {
                low = middle + 1;
            }
            else
            {
                high = middle;
            }
        }

        return low;
    }

    /// <summary>
    /// 子 ModelNode 境界で止めながら、可視メッシュの三角形を ModelNode ローカル座標で収集する
    /// </summary>
    private static void CollectTriangles(Node node, ModelNode root, Transform3D worldToLocal, List<Triangle> triangles, ref double totalArea)
    {
        if ((node is ModelNode && node != root) || (node is Node3D node3D && !node3D.IsVisibleInTree()))
        {
            return;
        }

        if (node is MeshInstance3D meshInstance && meshInstance.Mesh != null)
        {
            Transform3D toLocal = worldToLocal * meshInstance.GlobalTransform;
            Mesh mesh = meshInstance.Mesh;
            for (int surface = 0; surface < mesh.GetSurfaceCount(); surface++)
            {
                CollectSurfaceTriangles(mesh.SurfaceGetArrays(surface), toLocal, triangles, ref totalArea);
            }
        }

        foreach (Node child in node.GetChildren())
        {
            CollectTriangles(child, root, worldToLocal, triangles, ref totalArea);
        }
    }

    /// <summary>
    /// 1サーフェス分の頂点配列から三角形を収集する。インデックスが無い場合は頂点を3つ組として扱う
    /// </summary>
    private static void CollectSurfaceTriangles(Godot.Collections.Array arrays, Transform3D toLocal, List<Triangle> triangles, ref double totalArea)
    {
        Variant vertexArray = arrays[(int)Mesh.ArrayType.Vertex];
        if (vertexArray.VariantType != Variant.Type.PackedVector3Array)
        {
            return;
        }

        Vector3[] vertices = vertexArray.AsVector3Array();
        Variant indexArray = arrays[(int)Mesh.ArrayType.Index];
        int[] indices = indexArray.VariantType == Variant.Type.PackedInt32Array
            ? indexArray.AsInt32Array()
            : Array.Empty<int>();

        if (indices.Length > 0)
        {
            for (int i = 0; i + 2 < indices.Length; i += 3)
            {
                AddTriangle(vertices, indices[i], indices[i + 1], indices[i + 2], toLocal, triangles, ref totalArea);
            }

            return;
        }

        for (int i = 0; i + 2 < vertices.Length; i += 3)
        {
            AddTriangle(vertices, i, i + 1, i + 2, toLocal, triangles, ref totalArea);
        }
    }

    /// <summary>
    /// 有効な三角形をローカル座標へ変換し、累積面積付きで追加する
    /// </summary>
    private static void AddTriangle(Vector3[] vertices, int indexA, int indexB, int indexC, Transform3D toLocal, List<Triangle> triangles, ref double totalArea)
    {
        if ((uint)indexA >= vertices.Length || (uint)indexB >= vertices.Length || (uint)indexC >= vertices.Length)
        {
            return;
        }

        Vector3 a = toLocal * vertices[indexA];
        Vector3 b = toLocal * vertices[indexB];
        Vector3 c = toLocal * vertices[indexC];
        double area = (b - a).Cross(c - a).Length() * 0.5d;
        if (area <= 0d || double.IsNaN(area) || double.IsInfinity(area))
        {
            return;
        }

        totalArea += area;
        triangles.Add(new Triangle(a, b, c, totalArea));
    }

    /// <summary>頂点座標と累積面積を持つ三角形</summary>
    private readonly struct Triangle
    {
        /// <summary>三角形の頂点A</summary>
        public Vector3 A { get; }

        /// <summary>三角形の頂点B</summary>
        public Vector3 B { get; }

        /// <summary>三角形の頂点C</summary>
        public Vector3 C { get; }

        /// <summary>この三角形までの累積面積</summary>
        public double CumulativeArea { get; }

        /// <summary>三角形データを初期化する</summary>
        public Triangle(Vector3 a, Vector3 b, Vector3 c, double cumulativeArea)
        {
            A = a;
            B = b;
            C = c;
            CumulativeArea = cumulativeArea;
        }
    }

    #endregion
}
