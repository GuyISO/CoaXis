using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// ModelNode はモデル階層を管理するクラスで、内部構造は ModelComponents に委譲する
/// </summary>
public partial class ModelNode : Node3D
{
    #region Properties

    /// <summary>
    /// このモデルのデータを保持する ModelData
    /// </summary>
    public ModelData Data { get; private set; }

    /// <summary>
    /// このモデルの内部構造を保持するコンポーネントルート
    /// </summary>
    /// <returns>内部構造を保持する ModelComponents</returns>
    public ModelComponents Components { get; private set; }

    /// <summary>
    /// このモデルの親モデルを取得する、親モデルが存在しない場合は null を返す
    /// </summary>
    /// <returns>親モデル、存在しない場合は null</returns>
    public virtual ModelNode ParentModel => GetParentOrNull<ModelNode>();

    /// <summary>
    /// このモデルの子モデルのリストを取得する、子モデルが存在しない場合は空のリストを返す
    /// </summary>
    /// <returns>子モデルのリスト、存在しない場合は空のリスト</returns>
    public List<ModelNode> ChildModels => GetChildren().OfType<ModelNode>().ToList();

    #endregion

    #region Lifecycle

    public override void _Ready()
    {
        Components = CreateComponents();
        Components.Initialize();
        AddChild(Components);

        if (Data != null)
        {
            Data.Node = this;
        }
    }

    #endregion

    #region Internal Helpers

    /// <summary>
    /// このモデルに追加する内部構造を生成する
    /// </summary>
    /// <returns>内部構造のルートコンポーネント</returns>
    protected virtual ModelComponents CreateComponents()
    {
        return new ModelComponents();
    }

    #endregion
}