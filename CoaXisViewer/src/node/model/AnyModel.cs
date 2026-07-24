using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// AnyModel はモデル階層を管理するクラスで、内部構造は AnyComponents に委譲する
/// </summary>
public partial class AnyModel : Node3D
{
    #region Properties

    /// <summary>
    /// このモデルの内部構造を保持するコンポーネントルート
    /// </summary>
    /// <returns>内部構造を保持する AnyComponents</returns>
    public AnyComponents Components { get; private set; }

    /// <summary>
    /// このモデルを一意に識別する Guid
    /// </summary>
    public Guid Guid { get; } = Guid.NewGuid();

    /// <summary>
    /// このモデルの親モデルを取得する、親モデルが存在しない場合は null を返す
    /// </summary>
    /// <returns>親モデル、存在しない場合は null</returns>
    public virtual AnyModel ParentModel => GetParentOrNull<AnyModel>();

    /// <summary>
    /// このモデルの子モデルのリストを取得する、子モデルが存在しない場合は空のリストを返す
    /// </summary>
    /// <returns>子モデルのリスト、存在しない場合は空のリスト</returns>
    public List<AnyModel> ChildModels => GetChildren().OfType<AnyModel>().ToList();

    #endregion

    #region Lifecycle

    public override void _Ready()
    {
        Components = CreateComponents();
        Components.Initialize();
        AddChild(Components);
    }

    #endregion

    #region Internal Helpers

    /// <summary>
    /// このモデルに追加する内部構造を生成する
    /// </summary>
    /// <returns>内部構造のルートコンポーネント</returns>
    protected virtual AnyComponents CreateComponents()
    {
        return new AnyComponents();
    }

    #endregion
}
