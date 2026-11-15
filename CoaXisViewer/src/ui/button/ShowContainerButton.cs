using Godot;

/// <summary>
/// Containerを継承したUIウィンドウの表示を切り替えるボタン。
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
        SetPressedNoSignal(Application.Ui.Window.IsContainerVisible(_container));
    }

    /// <summary>
    /// ボタンが押されたときに呼ばれるイベントハンドラ
    /// </summary>
    private void OnPressed()
    {
        if (_container == null)
        {
            Application.Log.Warn("ShowContainerButton: Container is not assigned.");
            SetPressedNoSignal(false);
            return;
        }

        if (Application.Ui.Window.IsContainerVisible(_container))
        {
            Application.Ui.Window.Close(_container);
        }
        else
        {
            Application.Ui.Window.Show(_container.Instantiate<Container>());
        }
    }

    #endregion
}
