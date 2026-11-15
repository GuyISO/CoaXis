using Godot;

/// <summary>
/// Viewerのライトテーマとダークテーマを切り替えるボタン。
/// </summary>
public partial class ThemeToggleButton : Button
{
    #region Lifecycle

    /// <summary>
    /// 保存済みテーマをボタン状態へ反映して切替イベントを購読する。
    /// </summary>
    public override void _Ready()
    {
        ToggleMode = true;
        ButtonPressed = Application.Ui.Theme.CurrentThemeId == UiSettings.LightThemeId;
        UpdateDisplay(ButtonPressed);
        Toggled += OnToggled;
    }

    /// <summary>
    /// 切替イベントの購読を解除する。
    /// </summary>
    public override void _ExitTree()
    {
        Toggled -= OnToggled;

        base._ExitTree();
    }

    #endregion

    #region Events

    /// <summary>
    /// ボタン状態に応じてテーマを切り替える。
    /// </summary>
    /// <param name="useLightTheme">ライトテーマを使用する場合は true。</param>
    private void OnToggled(bool useLightTheme)
    {
        Application.Ui.Theme.SetTheme(
            useLightTheme ? UiSettings.LightThemeId : UiSettings.DefaultThemeId);
        UpdateDisplay(useLightTheme);
    }

    #endregion

    #region Helpers

    /// <summary>
    /// 現在のテーマをボタンに表示する。
    /// </summary>
    /// <param name="useLightTheme">ライトテーマを使用する場合は true。</param>
    private void UpdateDisplay(bool useLightTheme)
    {
        Text = useLightTheme ? "Light" : "Dark";
        TooltipText = useLightTheme
            ? "ライトテーマを使用中です。クリックでダークテーマに切り替えます"
            : "ダークテーマを使用中です。クリックでライトテーマに切り替えます";
    }

    #endregion
}
