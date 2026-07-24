/// <summary>
/// 点モデルの標準実装
/// </summary>
public partial class PointModel : AnyModel
{
    #region Internal Helpers

    protected override AnyComponents CreateComponents()
    {
        return new PointComponents();
    }

    #endregion
}