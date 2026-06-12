using Godot;

/// <summary>
/// シーン管理用のシングルトンクラスです。AutoLoadノードとしてシーンツリーに配置します。
/// </summary>
public partial class SceneManager : Node
{
	/// <summary>
	/// シングルトン参照です。
	/// </summary>
	public static SceneManager I { get; private set; }

	/// <summary>
	/// シーンツリー参加時にシングルトン参照を確立します。
	/// </summary>
	public override void _EnterTree()
	{
		// AutoLoad をデフォルト参照として維持するため、未設定時のみ I を確立する。
		if (I == null)
		{
			I = this;
		}
	}

	#region Public API

	/// <summary>
	/// 指定されたコマンド名に対応する処理を実行します。
	/// </summary>
	/// <param name="commandName">実行するコマンドの名前</param>
	public void ExecuteCommand(string commandName)
	{
		// コマンドの実行ロジックをここに実装
	}

	#endregion

}