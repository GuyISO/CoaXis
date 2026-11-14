using Godot;
using System;

/// <summary>
/// UiService に対して Containerを継承したUIの表示を要求するボタンへアタッチして使用する
/// </summary>
public partial class ShowContainerButton : Button
{
    #region Fields

    [Export] private PackedScene _container = null;

    #endregion

    #region Lifecycle

    public override void _Ready()
    {
        ToggleMode = true;
        SubscribeUiEvents();
        OnWindowVisibilityNotified();
    }

    public override void _ExitTree()
    {
        UnsubscribeUiEvents();

        base._ExitTree();
    }

    #endregion

    #region Events

    private void SubscribeUiEvents()
    {
        Pressed += OnPressed;
        Application.Ui.Window.WindowVisibilityNotified += OnWindowVisibilityNotified;
    }

    private void UnsubscribeUiEvents()
    {
        Pressed -= OnPressed;
        Application.Ui.Window.WindowVisibilityNotified -= OnWindowVisibilityNotified;
    }

    /// <summary>
    /// 表示ウィンドウの状態通知を受けて、このボタンの押下状態を同期する。
    /// </summary>
    private void OnWindowVisibilityNotified()
    {
        ButtonPressed = Application.Ui.Window.IsContainerVisible(_container);
    }

    /// <summary>
    /// ボタンが押されたときに呼ばれるイベントハンドラ
    /// </summary>
    private void OnPressed()
    {
        if (_container == null)
        {
            Application.Log.Warn("ShowContainerButton: Container is not assigned.");
            return;
        }

        // UiWindowService に対してコンテナの表示を要求する
        Application.Ui.Window.Show(_container.Instantiate<Container>());
    }

    #endregion
}
