/// <summary>
/// UI 関連モジュールの構成を管理するハブ。
/// </summary>
public partial class UiHub : BaseHub
{
	#region Fields

	#endregion

	#region Properties

	/// <summary>メニューハブを取得する。</summary>
	public UiMenuHub Menu { get; }
	/// <summary>ModelEntityプロパティの表示要求を管理するハブを取得する。</summary>
	public UiPropertyHub Property { get; }
	/// <summary>ウィンドウハブを取得する。</summary>
	public UiWindowHub Window { get; }

	#endregion

	#region Signals

	#endregion

	#region Lifecycle

	/// <summary>
	/// UI 機能を担当する各モジュールを初期化する。
	/// </summary>
	public UiHub()
	{
		Menu = AddModule<UiMenuHub>("Menu");
		Property = AddModule<UiPropertyHub>("Property");
		Window = AddModule<UiWindowHub>("Window");
	}

	#endregion

	#region Events

	#endregion

	#region Methods

	#endregion

	#region Helpers

	#endregion
}