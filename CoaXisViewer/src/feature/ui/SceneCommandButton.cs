using Godot;
using System;

/// <summary>
/// Autoload のシングルトンクラス UiManager に対して UI 表示やシーン切り替えの指示を送るボタンへアタッチして使用する
/// </summary>
public partial class SceneCommandButton : Button
{

    #region Lifecycle

    public override void _Ready()
    {
        // UIイベントの登録
        Pressed += OnPressed;
    }

    #endregion

    #region Events

    /// <summary>
    /// ボタンが押されたとき UiManager に自身の Name を送信し、同名処理のコマンドを実行するよう指示する
    /// NodeのNameの先頭文字が"Button"の場合は、先頭の"Button"を除いた文字列をコマンド名として送信する
    /// </summary>
    private void OnPressed()
    {
        string commandName = Name;
        if (commandName.StartsWith("Button"))
        {
            commandName = commandName.Substring("Button".Length);
        }
        UiManager.Instance.ExecuteCommand(commandName, this);
    }

    #endregion

}
