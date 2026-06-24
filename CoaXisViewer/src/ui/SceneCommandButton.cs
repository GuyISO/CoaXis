using Godot;
using System;

/// <summary>
/// Autoloadのシングルトンクラス、SceneManagerに対して、UIの表示やシーンの切り替えを行う指示を出すボタンにアタッチして使用する。
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
	/// ボタンが押されたとき、SceneManagerに対して、自身のNameを送信し、同名の処理をプロパティで指定されたコマンドを実行するよう指示します。
	/// NodeのNameの先頭文字が"Button"の場合は、先頭の"Button"を除いた文字列をコマンド名として送信します。
	/// </summary>
	private void OnPressed()
	{
		string commandName = Name;
		if (commandName.StartsWith("Button"))
		{
			commandName = commandName.Substring("Button".Length);
		}
		SceneManager.I.ExecuteCommand(commandName);
	}

	#endregion

}
