using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// AnyModel は 3Dモデルの基本構造を表す抽象クラスで、各モデルはメッシュと衝突形状を保持するための Node3D と StaticBody3D を持つ
/// </summary>
public partial class AnyModel : Node3D
{
    #region Properties

    /// <summary>
    /// このモデルのメッシュを保持する Node3D で、動的ロード後はこの中にメッシュが追加される
    /// </summary>
    /// <returns>メッシュを保持する Node3D</returns>
    public Node3D Mesh { get; private set; }

    /// <summary>
    /// このモデルがメッシュを持っているかどうかを示す
    /// </summary>
    /// <returns>メッシュが存在する場合は true、存在しない場合は false を返す</returns>
    public bool HasMesh => Mesh.GetChildCount() > 0;

    /// <summary>
    /// このモデルの衝突形状を保持する StaticBody3D 
    /// </summary>
    /// <returns>衝突形状を保持する StaticBody3D</returns>
    public StaticBody3D Collider { get; private set; }

    /// <summary>
    /// このモデルが衝突形状を持っているかどうかを示す
    /// </summary>
    /// <returns>衝突形状が存在する場合は true、存在しない場合は false を返す</returns>
    public bool HasCollider => Collider.GetChildCount() > 0;

    /// <summary>
    /// このモデルのエフェクトを保持する Node3D で、動的ロード後はこの中にエフェクトが追加される
    /// </summary>
    /// <returns>エフェクトを保持する Node3D</returns>
    public Node3D Effect { get; private set; }

    /// <summary>
    /// このモデルがエフェクトを持っているかどうかを示す
    /// </summary>
    /// <returns>エフェクトが存在する場合は true、存在しない場合は false を返す</returns>
    public bool HasEffect => Effect.GetChildCount() > 0;

    /// <summary>
    /// このモデルの親モデルを取得する、親モデルが存在しない場合は null を返す
    /// </summary>
    /// <returns>親モデル、存在しない場合は null</returns>
    public AnyModel ParentModel => GetParentOrNull<AnyModel>();

    /// <summary>
    /// このモデルの子モデルのリストを取得する、子モデルが存在しない場合は空のリストを返す
    /// </summary>
    /// <returns>子モデルのリスト、存在しない場合は空のリスト</returns>
    public List<AnyModel> ChildModels => GetChildren().OfType<AnyModel>().ToList();

    #endregion

    #region Lifecycle

    public override void _Ready()
    {
        Mesh = CreateNode<Node3D>("Mesh");
        Collider = CreateNode<StaticBody3D>("Collider");
        Effect = CreateNode<Node3D>("Effect");
    }

    #endregion

    #region Internal Helpers

    private T CreateNode<T>(string name) where T : Node, new()
    {
        var node = new T { Name = name };
        AddChild(node);
        return node;
    }

    #endregion
}
