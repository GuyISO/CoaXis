using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// モデル関連のイベント集約ハブです。AutoLoadノードとしてシーンツリーに配置し、モデルの状態変更や操作のリクエストを通知するためのシグナルを提供します。これにより、モデル操作のロジックを分散させずに一元管理できます。
/// Autoloadに登録してシングルトン参照することを前提としています。
/// </summary>
public partial class ModelEventHub : Node
{
	/// <summary>
	/// シングルトン参照です。
	/// </summary>
	/// <returns>シングルトン参照</returns>
	public static ModelEventHub I { get; private set; }

	/// <summary>
	/// シーンツリー参加時にシングルトン参照を確立します。
	/// </summary>
	public override void _EnterTree()
	{
		I = this;
	}

	/// <summary>
	/// シーンツリー離脱時に、シングルトン参照を破棄します。
	/// </summary>
	public override void _ExitTree()
	{
		I = null;
	}

	#region --------------------------------------- Request ---------------------------------------

	[Signal] public delegate void SetMultiSelectModeRequestedEventHandler(bool enable);
	/// <summary>
	/// 複数選択モードの設定をリクエストします。
	/// </summary>
	/// <param name="enable">複数選択モードを有効にする場合はtrue、無効にする場合はfalseです。</param>
	public static void RequestSetMultiSelectMode(bool enable)
	{
		I.EmitSignal(SignalName.SetMultiSelectModeRequested, enable);
	}

	[Signal] public delegate void SelectModelRequestedEventHandler(AnyModel model);
	/// <summary>
	/// モデルの選択をリクエストします。
	/// </summary>
	/// <param name="model">選択するモデルです。</param>
	public static void RequestSelectModel(AnyModel model)
	{
		I.EmitSignal(SignalName.SelectModelRequested, model);
	}

	[Signal] public delegate void SelectModelsRequestedEventHandler(AnyModel[] models);
	/// <summary>
	/// 複数モデルの選択をリクエストします。
	/// </summary>
	/// <param name="models">選択するモデルの配列です。</param>
	public static void RequestSelectModels(AnyModel[] models)
	{
		I.EmitSignal(SignalName.SelectModelsRequested, models);
	}

	[Signal] public delegate void ClearSelectionRequestedEventHandler();
	/// <summary>
	/// 選択のクリアをリクエストします。
	/// </summary>
	public static void RequestClearSelection()
	{
		I.EmitSignal(SignalName.ClearSelectionRequested);
	}








	[Signal] public delegate void AddModelRequestedEventHandler(AnyModel childModel, AnyModel parentModel);
	/// <summary>
	/// モデルの追加をリクエストします。
	/// </summary>
	/// <param name="childModel">追加するモデルです。</param>
	/// <param name="parentModel">追加先の親モデルです。nullの場合はルートに追加されます。</param>
	public static void RequestAddModel(AnyModel childModel, AnyModel parentModel = null)
	{
		I.EmitSignal(SignalName.AddModelRequested, childModel, parentModel);
	}

	[Signal] public delegate void LoadModelRequestedEventHandler(string path);
	/// <summary>
	/// モデルのロードをリクエストします。
	/// </summary>
	/// <param name="path">ロードするモデルのパスです。</param>
	public static void RequestLoadModel(string path)
	{
		I.EmitSignal(SignalName.LoadModelRequested, path);
	}

	#endregion

	#region --------------------------------------- Notification ---------------------------------------

	[Signal] public delegate void ModelSelectionStateNotifiedEventHandler(AnyModel model, bool isSelected);
	/// <summary>
	/// モデルの選択状態の通知を行います。
	/// </summary>
	/// <param name="model">選択状態が変化したモデルです。</param>
	/// <param name="isSelected">モデルが選択されている場合はtrue、選択されていない場合はfalseです。</param>
	public static void NotifyModelSelectionState(AnyModel model, bool isSelected)
	{
		I.EmitSignal(SignalName.ModelSelectionStateNotified, model, isSelected);
	}

	#endregion
}