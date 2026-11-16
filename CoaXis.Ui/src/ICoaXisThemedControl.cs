namespace CoaXis.Ui;

/// <summary>共通テーマを自身に適用するコントロールの契約。</summary>
public interface ICoaXisThemedControl
{
    /// <summary>指定されたテーマをコントロール自身に適用する。</summary>
    /// <param name="theme">適用するテーマ。</param>
    void ApplyTheme(Theme theme);
}
