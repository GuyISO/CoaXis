/// <summary>
/// 自動的にルート ModelEntity からの階層ツリーの表示と操作を行う UI コンポーネント
/// </summary>
public partial class RootModelEntityTree : ModelEntityTree
{
    #region Lifecycle

    public override void _Ready()
    {
        base._Ready();

        // 初期表示のためにルート ModelEntity を設定する
        SetRootModelEntity(Application.Model.Service.RootEntity);
    }
    
    #endregion
}