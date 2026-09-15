using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// メッシュのAABB中心を基準に ModelComponents.Pivot の原点補正を行うヘルパー
/// </summary>
public static class ModelMeshCenterAligner
{
    /// <summary>
    /// メッシュのAABB中心を Pivot の原点へ合わせ、Pivot配下の各ノードは逆方向へ補正することで
    /// 見た目の位置を変えずに Emphasize/SpinAppeal のスケール・回転の基準点をメッシュ中心へ揃える
    /// </summary>
    /// <remarks>
    /// シーン(scn/tscn)の原点は作成元ツール依存で任意の点になりやすく、
    /// 原点のまま ModelComponents を拡縮・回転させると見た目の中心がずれて見える。
    /// このメソッドは ModelEntityDto.AlignToAabbCenter が true の場合にのみ呼び出す想定。
    /// ModelNode/ModelEntity の位置は CSV 由来の値と常に一致させる必要があるため、ここでは一切変更しない。
    /// </remarks>
    /// <param name="modelNode">補正対象のモデルノード</param>
    public static void AlignPivotToMeshCenter(ModelNode modelNode)
    {
        if (modelNode == null)
        {
            throw new ArgumentNullException(nameof(modelNode));
        }

        ModelComponents components = modelNode.Components;
        Node3D pivot = components?.Pivot;
        if (pivot == null || pivot.GetChildCount() == 0)
        {
            return;
        }

        List<VisualInstance3D> visuals = new List<VisualInstance3D>();
        CollectVisuals(pivot, visuals);
        if (visuals.Count == 0)
        {
            return;
        }

        // 各メッシュのAABBを Pivot ローカル空間へ変換して合算する
        // このタイミングでは Pivot は原点(Position=Vector3.Zero)のままなので、
        // GlobalTransform 同士の相対計算がそのまま Pivot ローカル座標になる
        Transform3D toPivot = pivot.GlobalTransform.AffineInverse();
        Aabb? mergedAabb = null;
        foreach (VisualInstance3D visual in visuals)
        {
            Transform3D visualToPivot = toPivot * visual.GlobalTransform;
            Aabb transformed = TransformAabb(visualToPivot, visual.GetAabb());
            mergedAabb = mergedAabb.HasValue ? mergedAabb.Value.Merge(transformed) : transformed;
        }

        if (!mergedAabb.HasValue)
        {
            return;
        }

        Vector3 center = mergedAabb.Value.GetCenter();
        if (center.IsZeroApprox())
        {
            return;
        }

        // Pivot をAABB中心へ移動し、既存の子（Content等）は逆方向へ動かして見た目の位置を維持する
        // ModelNode/ModelEntityの位置は一切動かさず、Pivotの原点のみがメッシュの実中心と一致する
        foreach (Node3D child in pivot.GetChildren())
        {
            child.Position -= center;
        }
        pivot.Position = center;
    }

    private static void CollectVisuals(Node node, List<VisualInstance3D> list)
    {
        if (node is VisualInstance3D visual)
        {
            list.Add(visual);
        }

        foreach (Node child in node.GetChildren())
        {
            CollectVisuals(child, list);
        }
    }

    private static Aabb TransformAabb(Transform3D transform, Aabb localAabb)
    {
        // Aabb には変換後の外接直方体を直接求める API がないため、8頂点を変換して包み直す
        Vector3 origin = localAabb.Position;
        Vector3 size = localAabb.Size;
        Aabb result = new Aabb(transform * origin, Vector3.Zero);

        for (int i = 1; i < 8; i++)
        {
            Vector3 corner = origin + new Vector3(
                (i & 1) != 0 ? size.X : 0f,
                (i & 2) != 0 ? size.Y : 0f,
                (i & 4) != 0 ? size.Z : 0f);
            result = result.Expand(transform * corner);
        }

        return result;
    }
}
