using Godot;
using System;

/// <summary>
/// UiManager に対して Containerを継承したUIの表示を要求するボタンへアタッチして使用する
/// </summary>
public partial class ShowContainerButton : Button
{
    #region Fields

    [Export] private PackedScene _container = null;

    #endregion

    #region Lifecycle

    public override void _Ready()
    {
        SubscribeUiEvents();
    }

    #endregion

    public override void _ExitTree()
    {
        UnsubscribeUiEvents();

        base._ExitTree();
    }

    #region Events

    private void SubscribeUiEvents()
    {
        Pressed += OnPressed;
    }

    private void UnsubscribeUiEvents()
    {
        Pressed -= OnPressed;
    }

    /// <summary>
    /// ボタンが押されたときに呼ばれるイベントハンドラ
    /// </summary>
    private void OnPressed()
    {
        if (_container == null)
        {
            Application.Log.Service.Warn("ShowContainerButton: Container is not assigned.");
            return;
        }

        // UiManager に対してコンテナの表示を要求する
        Application.Ui.Service.Show(_container.Instantiate<Container>());
    }

    #endregion

}
