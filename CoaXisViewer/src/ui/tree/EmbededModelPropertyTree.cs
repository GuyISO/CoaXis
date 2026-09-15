using Godot;
using System;

public partial class EmbededModelPropertyTree : ModelPropertyTree
{
    #region Lifecycle

    public override void _Ready()
    {
        base._Ready();

        // Serviceが参照するために自身を設定する
        Application.Model.Service.SetEmbededModelPropertyTree(this);
    }
    
    #endregion
}
