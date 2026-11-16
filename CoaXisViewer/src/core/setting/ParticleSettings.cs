/// <summary>
/// パーティクル表示に適用する設定値。
/// </summary>
public sealed class ParticleSettings
{
    #region Properties

    private const int DefaultAmount = 12;

    /// <summary>
    /// ModelProperty を表現するパーティクルの数。
    /// </summary>
    public int Amount { get; set; } = DefaultAmount;

    #endregion

    #region Methods

    /// <summary>
    /// パーティクル設定の不正値を補正する。
    /// </summary>
    public void Normalize()
    {
        if (Amount <= 0)
        {
            Amount = DefaultAmount;
        }
    }

    #endregion
}
