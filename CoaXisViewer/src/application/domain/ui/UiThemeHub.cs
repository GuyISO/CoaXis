using Godot;
using System;

/// <summary>
/// Viewer全体のUIテーマを管理するハブ。
/// </summary>
public partial class UiThemeHub : BaseHub
{
    #region Fields

    /// <summary>ダークテーマのリソースパス。</summary>
    private const string DarkThemePath = "res://assets/theme/coaxis_dark.tres";

    /// <summary>ライトテーマのリソースパス。</summary>
    private const string LightThemePath = "res://assets/theme/coaxis_light.tres";

    /// <summary>Theme内でEnvironment用の色を保持する型名。</summary>
    private const string EnvironmentThemeType = "CoaXisEnvironment";

    /// <summary>Environmentの背景色を保持するTheme内の項目名。</summary>
    private const string EnvironmentBackgroundColorName = "background";

    /// <summary>ダークテーマのリソース。</summary>
    private Theme _darkTheme;

    /// <summary>ライトテーマのリソース。</summary>
    private Theme _lightTheme;

    #endregion

    #region Properties

    /// <summary>現在ライトテーマを使用しているかどうか。</summary>
    public bool IsLightTheme => Application.Setting.Current.Ui.UseLightTheme;

    /// <summary>現在適用中のテーマリソース。</summary>
    public Theme CurrentTheme { get; private set; }

    /// <summary>現在のテーマに設定されたEnvironment背景色。</summary>
    public Color EnvironmentBackgroundColor
    {
        get
        {
            if (CurrentTheme == null)
            {
                throw new InvalidOperationException("The current UI theme has not been loaded.");
            }

            if (!CurrentTheme.HasColor(EnvironmentBackgroundColorName, EnvironmentThemeType))
            {
                throw new InvalidOperationException(
                    $"Theme color '{EnvironmentBackgroundColorName}' is missing from '{EnvironmentThemeType}'.");
            }

            return CurrentTheme.GetColor(EnvironmentBackgroundColorName, EnvironmentThemeType);
        }
    }

    #endregion

    #region Signals

    /// <summary>UIテーマが切り替わったことを通知する。</summary>
    [Signal] public delegate void ThemeChangedEventHandler();

    #endregion

    #region Lifecycle

    /// <summary>
    /// テーマリソースを読み込み、保存済みのテーマを適用する。
    /// </summary>
    public override void _Ready()
    {
        _darkTheme = LoadTheme(DarkThemePath);
        _lightTheme = LoadTheme(LightThemePath);
        ApplyTheme(IsLightTheme);
    }

    #endregion

    #region Events

    #endregion

    #region Methods

    /// <summary>
    /// テーマを切り替えて設定を保存する。
    /// </summary>
    /// <param name="useLightTheme">ライトテーマを使用する場合は true。</param>
    /// <returns>設定を保存できた場合は true。</returns>
    public bool SetLightTheme(bool useLightTheme)
    {
        bool saved = Application.Setting.SetLightTheme(useLightTheme);
        ApplyTheme(useLightTheme);
        return saved;
    }

    /// <summary>
    /// 指定されたウィンドウに現在のテーマを適用する。
    /// </summary>
    /// <param name="window">テーマ適用先のウィンドウ。</param>
    public void ApplyTo(Window window)
    {
        if (window == null)
        {
            throw new ArgumentNullException(nameof(window));
        }

        window.Theme = CurrentTheme;
    }

    #endregion

    #region Helpers

    /// <summary>
    /// 選択テーマをViewport、メインウィンドウ、UIサブウィンドウへ適用する。
    /// </summary>
    /// <param name="useLightTheme">ライトテーマを使用する場合は true。</param>
    private void ApplyTheme(bool useLightTheme)
    {
        CurrentTheme = useLightTheme ? _lightTheme : _darkTheme;
        RenderingServer.SetDefaultClearColor(EnvironmentBackgroundColor);
        Window rootWindow = GetTree().Root;
        rootWindow.Theme = CurrentTheme;
        ApplyThemeToUiRoots(rootWindow);
        EmitSignal(SignalName.ThemeChanged);
    }

    /// <summary>
    /// SceneTree内のウィンドウと、親Controlを持たないUIルートへ現在のテーマを適用する。
    /// </summary>
    /// <param name="node">走査対象のノード。</param>
    private void ApplyThemeToUiRoots(Node node)
    {
        foreach (Node child in node.GetChildren())
        {
            if (child is Window window)
            {
                window.Theme = CurrentTheme;
            }
            else if (child is Control control && child.GetParent() is not Control)
            {
                control.Theme = CurrentTheme;
            }

            ApplyThemeToUiRoots(child);
        }
    }

    /// <summary>
    /// 指定パスのテーマを読み込む。
    /// </summary>
    /// <param name="path">テーマリソースのパス。</param>
    /// <returns>読み込んだテーマ。</returns>
    private static Theme LoadTheme(string path)
    {
        Theme theme = GD.Load<Theme>(path);
        if (theme == null)
        {
            throw new InvalidOperationException($"Theme resource could not be loaded: '{path}'.");
        }

        return theme;
    }

    #endregion
}
