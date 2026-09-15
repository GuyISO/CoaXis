using Godot;
using System;

public partial class EmbededModelPropertyTree : ModelPropertyTree
{
    #region Lifecycle

    public override void _Ready()
    {
        base._Ready();

        // 初期表示のためにRootModelを設定する
    }
    
    #endregion
}
