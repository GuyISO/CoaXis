/// <summary>
/// 面モデルの標準実装
/// </summary>
public partial class SurfaceModel : AnyModel
{
    #region Internal Helpers

    protected override AnyComponents CreateComponents()
    {
        return new SurfaceComponents();
    }

    #endregion
}