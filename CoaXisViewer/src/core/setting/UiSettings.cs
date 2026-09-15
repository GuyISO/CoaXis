/// <summary>
/// UI 表示に適用する設定値。
/// </summary>
public sealed class UiSettings
{
    public const int DefaultMessageMaxLines = 50;
    public const int MinMessageMaxLines = 1;

    /// <summary>
    /// メッセージパネルに保持する最大行数。
    /// </summary>
    public int MessageMaxLines { get; set; } = DefaultMessageMaxLines;

    /// <summary>
    /// UI 設定の不正値を補正する。
    /// </summary>
    public void Normalize()
    {
        if (MessageMaxLines < MinMessageMaxLines)
        {
            MessageMaxLines = DefaultMessageMaxLines;
        }
    }
}
