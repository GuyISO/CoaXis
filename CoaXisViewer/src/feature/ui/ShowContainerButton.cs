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
        // UIイベントの登録
        Pressed += OnPressed;
    }

    #endregion

    public override void _ExitTree()
    {
        // UIイベントの解除
        Pressed -= OnPressed;
    }

    #region Events

    /// <summary>
    /// ボタンが押されたときに呼ばれるイベントハンドラ
    /// </summary>
    private void OnPressed()
    {
        if (_container == null)
        {
            LogHub.Warn("ShowContainerButton: Container is not assigned.");
            return;
        }

        // UiManager に対してコンテナの表示を要求する
        UiManager.Show(_container.Instantiate<Container>());
    }

    #endregion

}
