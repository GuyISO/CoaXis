/// <summary>
/// 自動的にRootModelからの階層ツリーの表示と操作を行うUIコンポーネント
/// </summary>
public partial class RootHierarchyTree : HierarchyTree
{
    #region Lifecycle

    public override void _Ready()
    {
        base._Ready();

        // 初期表示のためにRootModelを設定する
        SetRootModelData(Application.Model.Service.Root);
    }

    #endregion
}