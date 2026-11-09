using System;
using System.Drawing;
using System.Windows.Forms;

namespace CoaXis.Ui;

/// <summary>
/// CoaXis UI の配色を定義する。
/// </summary>
public abstract class Theme
{
    private static readonly Font DefaultStandardFont = new("Segoe UI", 9F, FontStyle.Regular);
    private static readonly Font DefaultButtonFont = new("Segoe UI", 9F, FontStyle.Bold);
    private static readonly Font DefaultBrandFont = new("Segoe UI", 16F, FontStyle.Bold);
    private static readonly Font DefaultTitleFont = new("Segoe UI", 15F, FontStyle.Bold);

    /// <summary>通常のコントロールのフォントを取得する。</summary>
    public virtual Font StandardFont => DefaultStandardFont;

    /// <summary>ボタンのフォントを取得する。</summary>
    public virtual Font ButtonFont => DefaultButtonFont;

    /// <summary>ブランド見出しのフォントを取得する。</summary>
    public virtual Font BrandFont => DefaultBrandFont;

    /// <summary>タイトルバーのアプリケーション名のフォントを取得する。</summary>
    public virtual Font TitleFont => DefaultTitleFont;

    /// <summary>アプリケーションの背景色を取得する。</summary>
    public abstract Color Background { get; }

    /// <summary>カードや入力欄の背景色を取得する。</summary>
    public abstract Color Surface { get; }

    /// <summary>テキストや選択項目を入力する領域の背景色を取得する。</summary>
    public virtual Color InputBackground => Surface;

    /// <summary>未選択のチェック欄にマウスを重ねたときの背景色を取得する。</summary>
    public virtual Color CheckBoxHoverBackground => InputBackground;

    /// <summary>読み取り専用の表示領域の背景色を取得する。</summary>
    public virtual Color DisplayBackground => Surface;

    /// <summary>境界線の色を取得する。</summary>
    public abstract Color Border { get; }

    /// <summary>通常の文字色を取得する。</summary>
    public abstract Color Text { get; }

    /// <summary>操作のアクセントカラーを取得する。</summary>
    public abstract Color Primary { get; }

    /// <summary>通常ボタンの背景色を取得する。</summary>
    public virtual Color SecondaryButton => ColorTranslator.FromHtml("#66717C");

    /// <summary>通常ボタンのホバー色を取得する。</summary>
    public virtual Color SecondaryButtonHover => ColorTranslator.FromHtml("#59636D");

    /// <summary>通常ボタンの押下色を取得する。</summary>
    public virtual Color SecondaryButtonPressed => ColorTranslator.FromHtml("#4D5660");

    /// <summary>補助情報の文字色を取得する。</summary>
    public abstract Color MutedText { get; }

    /// <summary>選択項目の背景色を取得する。</summary>
    public abstract Color Selection { get; }

    /// <summary>選択されていないグリッドヘッダーの背景色を取得する。</summary>
    public abstract Color GridHeader { get; }

    /// <summary>タイトルバーの背景色を取得する。</summary>
    public abstract Color Header { get; }

    /// <summary>タイトルバーのボタンに重ねるホバー色を取得する。</summary>
    public abstract Color HeaderHover { get; }

    /// <summary>スクロールバーに適用するWindowsのテーマ名を取得する。nullは既定のテーマを表す。</summary>
    public virtual string ScrollBarThemeName => null;
}

/// <summary>
/// 明るい配色を表す。
/// </summary>
public sealed class LightTheme : Theme
{
    /// <inheritdoc />
    public override Color Background => ColorTranslator.FromHtml("#E2E3E5");

    /// <inheritdoc />
    public override Color Surface => ColorTranslator.FromHtml("#DCDDE0");

    /// <inheritdoc />
    public override Color InputBackground => ColorTranslator.FromHtml("#F2F3F4");

    /// <inheritdoc />
    public override Color DisplayBackground => ColorTranslator.FromHtml("#CFD1D4");

    /// <inheritdoc />
    public override Color Border => ColorTranslator.FromHtml("#D2DAE2");

    /// <inheritdoc />
    public override Color Text => ColorTranslator.FromHtml("#202020");

    /// <inheritdoc />
    public override Color Primary => ColorTranslator.FromHtml("#4E95D9");

    /// <inheritdoc />
    public override Color SecondaryButton => ColorTranslator.FromHtml("#66717C");

    /// <inheritdoc />
    public override Color SecondaryButtonHover => ColorTranslator.FromHtml("#59636D");

    /// <inheritdoc />
    public override Color SecondaryButtonPressed => ColorTranslator.FromHtml("#4D5660");

    /// <inheritdoc />
    public override Color MutedText => ColorTranslator.FromHtml("#606060");

    /// <inheritdoc />
    public override Color Selection => ColorTranslator.FromHtml("#DCEBFA");

    /// <inheritdoc />
    public override Color GridHeader => ColorTranslator.FromHtml("#EAF1F7");

    /// <inheritdoc />
    public override Color Header => ColorTranslator.FromHtml("#E5E6E8");

    /// <inheritdoc />
    public override Color HeaderHover => ColorTranslator.FromHtml("#E6F1FB");
}

/// <summary>
/// ダーク配色を表す。
/// </summary>
public sealed class DarkTheme : Theme
{
    /// <inheritdoc />
    public override Color Background => ColorTranslator.FromHtml("#202124");

    /// <inheritdoc />
    public override Color Surface => ColorTranslator.FromHtml("#252629");

    /// <inheritdoc />
    public override Color InputBackground => ColorTranslator.FromHtml("#17181A");

    /// <inheritdoc />
    public override Color CheckBoxHoverBackground => SecondaryButton;

    /// <inheritdoc />
    public override Color DisplayBackground => ColorTranslator.FromHtml("#323438");

    /// <inheritdoc />
    public override Color Border => ColorTranslator.FromHtml("#414347");

    /// <inheritdoc />
    public override Color Text => ColorTranslator.FromHtml("#F2F5F8");

    /// <inheritdoc />
    public override Color Primary => ColorTranslator.FromHtml("#4E95D9");

    /// <inheritdoc />
    public override Color SecondaryButton => ColorTranslator.FromHtml("#3A3D42");

    /// <inheritdoc />
    public override Color SecondaryButtonHover => ColorTranslator.FromHtml("#46494E");

    /// <inheritdoc />
    public override Color SecondaryButtonPressed => ColorTranslator.FromHtml("#303236");

    /// <inheritdoc />
    public override Color MutedText => ColorTranslator.FromHtml("#B0B0B0");

    /// <inheritdoc />
    public override Color Selection => ColorTranslator.FromHtml("#3A3D42");

    /// <inheritdoc />
    public override Color GridHeader => ColorTranslator.FromHtml("#252629");

    /// <inheritdoc />
    public override Color Header => ColorTranslator.FromHtml("#232427");

    /// <inheritdoc />
    public override Color HeaderHover => ColorTranslator.FromHtml("#34363A");

    /// <inheritdoc />
    public override string ScrollBarThemeName => "DarkMode_Explorer";
}

/// <summary>
/// フォームとその子コントロールへ共通テーマを適用する。
/// </summary>
public static class ThemeManager
{
    private static Theme _currentTheme = new DarkTheme();

    internal static Color BrandSecondary { get; } = ColorTranslator.FromHtml("#7F7F7F");

    /// <summary>通常のコントロールで使用する標準フォントを取得する。</summary>
    public static Font StandardFont => _currentTheme.StandardFont;

    /// <summary>ボタンの文字に使用するフォントを取得する。</summary>
    public static Font ButtonFont => _currentTheme.ButtonFont;

    /// <summary>ブランド見出しで使用するフォントを取得する。</summary>
    public static Font BrandFont => _currentTheme.BrandFont;

    internal static event EventHandler ThemeChanged;

    /// <summary>現在のテーマを取得する。</summary>
    public static Theme CurrentTheme => _currentTheme;

    /// <summary>以降のテーマ適用で使用するテーマを設定する。</summary>
    /// <param name="theme">適用するテーマ。</param>
    public static void SetTheme(Theme theme)
    {
        _currentTheme = theme ?? throw new ArgumentNullException(nameof(theme));
        ThemeChanged?.Invoke(null, EventArgs.Empty);
    }

    /// <summary>指定コントロールとその子孫へ現在のテーマを適用する。</summary>
    /// <param name="root">テーマ適用の起点となるコントロール。</param>
    public static void Apply(Control root)
    {
        ArgumentNullException.ThrowIfNull(root);
        ApplyControl(root);
    }

    private static void ApplyControl(Control control)
    {
        if (control is ICoaXisThemedControl themed)
        {
            themed.ApplyTheme(_currentTheme);
        }

        foreach (Control child in control.Controls)
        {
            ApplyControl(child);
        }
    }
}
