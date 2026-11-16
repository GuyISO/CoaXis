/// <summary>
/// UI 表示に適用する設定値。
/// </summary>
public sealed class UiSettings
{
    #region Properties
    
    public const int DefaultMessageMaxLines = 50;
    public const int MinMessageMaxLines = 1;

    /// <summary>
    /// ダークテーマの識別子。
    /// </summary>
    public const string DarkThemeId = "dark";

    /// <summary>
    /// ライトテーマの識別子。
    /// </summary>
    public const string LightThemeId = "light";

    /// <summary>
    /// 既定テーマの識別子。
    /// </summary>
    public const string DefaultThemeId = DarkThemeId;

    /// <summary>
    /// メッセージパネルに保持する最大行数。
    /// </summary>
    public int MessageMaxLines { get; set; } = DefaultMessageMaxLines;

    /// <summary>
    /// 使用するUIテーマの識別子。
    /// </summary>
    public string ThemeId { get; set; } = DefaultThemeId;

    #endregion
    
    #region Methods

    /// <summary>
    /// UI 設定の不正値を補正する。
    /// </summary>
    public void Normalize()
    {
        if (string.IsNullOrWhiteSpace(ThemeId))
        {
            ThemeId = DefaultThemeId;
        }

        if (MessageMaxLines < MinMessageMaxLines)
        {
            MessageMaxLines = DefaultMessageMaxLines;
        }
    }

    #endregion
}
