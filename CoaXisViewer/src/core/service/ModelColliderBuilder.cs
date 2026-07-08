using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// モデル配下のメッシュからコライダー形状を構築するヘルパー
/// </summary>
public static class ModelColliderBuilder
{
    /// <summary>
    /// 指定したモデル配下のメッシュから ConcavePolygonShape3D を構築する
    /// </summary>
    /// <param name="model">コライダーを追加する対象モデル</param>
    public static void AddCollider(AnyModel model)
    {
        Application.Instance.System.Log.Info($"Start adding collider for model: {model.Name}");
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        CollisionShape3D collisionShape = new CollisionShape3D();
        model.Collider.AddChild(collisionShape);

        List<MeshInstance3D> meshes = new List<MeshInstance3D>();
        CollectMeshes(model.Mesh, meshes);

        Vector3[] allFaces = new Vector3[meshes.Sum(meshInstance => meshInstance.Mesh?.GetFaces().Length ?? 0)];
        int index = 0;
        Transform3D inverseTransform = model.Collider.GlobalTransform.AffineInverse();

        foreach (MeshInstance3D meshInstance in meshes)
        {
            Mesh mesh = meshInstance.Mesh;
            if (mesh == null)
            {
                continue;
            }

            Vector3[] faces = mesh.GetFaces();
            Transform3D toBody = inverseTransform * meshInstance.GlobalTransform;
            for (int faceIndex = 0; faceIndex < faces.Length; faceIndex++)
            {
                allFaces[index++] = toBody * faces[faceIndex];
            }
        }

        if (index == 0)
        {
            Application.Instance.System.Log.Warn($"Skipped adding collider for model: {model.Name}, no mesh faces found.");
            return;
        }

        if (index < allFaces.Length)
        {
            Array.Resize(ref allFaces, index);
        }

        ConcavePolygonShape3D shape = new ConcavePolygonShape3D();
        shape.SetFaces(allFaces);
        collisionShape.Shape = shape;

        stopwatch.Stop();
        Application.Instance.System.Log.Info($"Finished adding collider for model: {model.Name} in {stopwatch.ElapsedMilliseconds} ms");
    }

    // モデル階層の深さに依存せずコライダーを作るため、MeshInstance3D を再帰収集する
    private static void CollectMeshes(Node node, List<MeshInstance3D> list)
    {
        if (node is MeshInstance3D meshInstance)
        {
            list.Add(meshInstance);
        }

        foreach (Node child in node.GetChildren())
        {
            CollectMeshes(child, list);
        }
    }
}
