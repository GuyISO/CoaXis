using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// モデル関連のイベント集約ハブ
/// Autoloadに登録してシングルトン参照する
/// </summary>
public partial class ModelEventHub : Node
{
	/// <summary>
	/// シングルトン参照
	/// </summary>
	/// <returns>シングルトン参照</returns>
	public static ModelEventHub Instance { get; private set; }

	/// <summary>
	/// シーンツリー参加時にシングルトン参照を確立する
	/// </summary>
	public override void _EnterTree()
	{
		Instance = this;
	}

	/// <summary>
	/// シーンツリー離脱時に、シングルトン参照を破棄する
	/// </summary>
	public override void _ExitTree()
	{
		Instance = null;
	}

	#region --------------------------------------- Request ---------------------------------------

	[Signal] public delegate void SetMultiSelectModeRequestedEventHandler(bool enable);
	/// <summary>
	/// 複数選択モードの設定をリクエストする
	/// </summary>
	/// <param name="enable">複数選択モードを有効にする場合はtrue、無効にする場合はfalse</param>
	public static void RequestSetMultiSelectMode(bool enable)
	{
		Instance.EmitSignal(SignalName.SetMultiSelectModeRequested, enable);
	}

	[Signal] public delegate void SelectModelRequestedEventHandler(AnyModel model);
	/// <summary>
	/// モデルの選択をリクエストする
	/// </summary>
	/// <param name="model">選択するモデル</param>
	public static void RequestSelectModel(AnyModel model)
	{
		Instance.EmitSignal(SignalName.SelectModelRequested, model);
	}

	[Signal] public delegate void SelectModelsRequestedEventHandler(AnyModel[] models);
	/// <summary>
	/// 複数モデルの選択をリクエストする
	/// </summary>
	/// <param name="models">選択するモデルの配列</param>
	public static void RequestSelectModels(AnyModel[] models)
	{
		Instance.EmitSignal(SignalName.SelectModelsRequested, models);
	}

	[Signal] public delegate void ClearSelectionRequestedEventHandler();
	/// <summary>
	/// 選択のクリアをリクエストする
	/// </summary>
	public static void RequestClearSelection()
	{
		Instance.EmitSignal(SignalName.ClearSelectionRequested);
	}








	[Signal] public delegate void AddModelRequestedEventHandler(AnyModel childModel, AnyModel parentModel);
	/// <summary>
	/// モデルの追加をリクエストする
	/// </summary>
	/// <param name="childModel">追加するモデル</param>
	/// <param name="parentModel">追加先の親モデル、nullの場合はルートに追加される</param>
	public static void RequestAddModel(AnyModel childModel, AnyModel parentModel = null)
	{
		Instance.EmitSignal(SignalName.AddModelRequested, childModel, parentModel);
	}

	[Signal] public delegate void LoadModelRequestedEventHandler(string path);
	/// <summary>
	/// モデルのロードをリクエストする
	/// </summary>
	/// <param name="path">ロードするモデルのパス</param>
	public static void RequestLoadModel(string path)
	{
		Instance.EmitSignal(SignalName.LoadModelRequested, path);
	}

	#endregion

	#region --------------------------------------- Notification ---------------------------------------

	[Signal] public delegate void ModelSelectionStateNotifiedEventHandler(AnyModel model, bool isSelected);
	/// <summary>
	/// モデルの選択状態の通知を行う
	/// </summary>
	/// <param name="model">選択状態が変化したモデル</param>
	/// <param name="isSelected">モデルが選択されている場合はtrue、選択されていない場合はfalse</param>
	public static void NotifyModelSelectionState(AnyModel model, bool isSelected)
	{
		Instance.EmitSignal(SignalName.ModelSelectionStateNotified, model, isSelected);
	}

	#endregion
}