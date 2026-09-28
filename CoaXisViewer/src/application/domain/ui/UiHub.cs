/// <summary>
/// UI 関連モジュールの構成を管理するハブ。
/// </summary>
public partial class UiHub : BaseHub
{
	#region Fields

	#endregion

	#region Properties

	public UiMenuHub Menu { get; }
	public UiWindowHub Window { get; }

	#endregion

	#region Lifecycle

	/// <summary>
	/// UI 機能を担当する各モジュールを初期化する。
	/// </summary>
	public UiHub()
	{
		Menu = AddModule<UiMenuHub>("Menu");
		Window = AddModule<UiWindowHub>("Window");
	}

	#endregion

	#region Actions

	#endregion

	#region Notifications

	#endregion

	#region Events

	#endregion

	#region Methods

	#endregion

	#region Helpers

	#endregion
}