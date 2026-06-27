using Godot;
using System;
using System.Linq;
using System.Collections.Generic;

/// <summary>
/// 選択管理クラスです。選択状態の管理と、選択変更イベントの発行を担当します。
/// Autoloadに登録してシングルトン参照します。
/// </summary>
public partial class Selection : Node
{
	#region  Fields

    public static Selection I { get; private set; }

	private static bool _isMultiSelectMode = false;
	private HashSet<AnyModel> _models = new HashSet<AnyModel>();

	#endregion

	#region Lifecycle

    public override void _Ready()
    {
        I = this;

		// イベントの購読開始
		ModelEventHub.I.SetMultiSelectModeRequested += OnSetMultiSelectModeRequested;
		ModelEventHub.I.SelectModelRequested += OnSelectModelRequested;
		ModelEventHub.I.SelectModelsRequested += OnSelectModelsRequested;
		ModelEventHub.I.ClearSelectionRequested += OnClearSelectionRequested;
    }

	public override void _ExitTree()
	{
		// イベントの購読解除
		ModelEventHub.I.SetMultiSelectModeRequested -= OnSetMultiSelectModeRequested;
		ModelEventHub.I.SelectModelRequested -= OnSelectModelRequested;
		ModelEventHub.I.SelectModelsRequested -= OnSelectModelsRequested;
		ModelEventHub.I.ClearSelectionRequested -= OnClearSelectionRequested;
	}

	#endregion

	#region Event

	private void OnSetMultiSelectModeRequested(bool enable)
	{
		_isMultiSelectMode = enable;
	}

	private void OnSelectModelRequested(AnyModel model)
	{
		if (_isMultiSelectMode)
		{
			Toggle(model);
		}
		else
		{
			Set(model);
		}
	}

	private void OnSelectModelsRequested(AnyModel[] models)
	{	
		if (_isMultiSelectMode)
		{
			Toggle(models);
		}
		else
		{
			Set(models);
		}
	}

	private void OnClearSelectionRequested()
	{
		Clear();
	}

	#endregion

	#region Public API

	/// <summary>
	/// 現在の選択モデルのコレクションを取得します。外部向けに読み取り専用で提供されます。
	/// </summary>
	public static IReadOnlyCollection<AnyModel> Models => I._models.ToList().AsReadOnly();

	/// <summary>
	/// 現在の選択モデルの数を取得します。
	/// </summary>
	public static int Count => I._models.Count;

	/// <summary>
	/// 指定したモデルが選択されているかどうかを確認します。
	/// </summary>
	/// <param name="model">確認するモデルです。</param>
	/// <returns>モデルが選択されている場合はtrue、それ以外の場合はfalseを返します。</returns>
	public static bool Contains(AnyModel model) => I._models.Contains(model);

	/// <summary>
	/// Fit処理に使用可能な選択モデルの配列を取得します。
	/// </summary>
	/// <remarks>
	/// 解放済みモデルやツリー外モデルを除外したスナップショットを返します。
	/// </remarks>
	public static AnyModel[] GetNodesArray()
	{
		return I._models
			.Where(model => model != null && GodotObject.IsInstanceValid(model) && model.IsInsideTree())
			.ToArray();
	}

	/// <summary>
	/// 指定したモデルのみの選択状態にします（既存の選択はすべて解除されます）。
	/// </summary>
	/// <param name="model">選択するモデルです。</param>
	public static void Set(AnyModel model)
	{
		Clear();
		Add(model);
	}

	/// <summary>
	/// 指定したモデル群のみの選択状態にします（既存の選択はすべて解除されます）。
	/// </summary>
	/// <param name="models">選択するモデルの配列です。</param>
	public static void Set(AnyModel[] models)
	{
		Clear();
		foreach (var model in models)
		{
			Add(model);
		}
	}

	/// <summary>
	/// 指定したモデルを選択状態にします。
	/// </summary> <param name="model">選択するモデルです。</param>
	/// <returns>モデルが新たに選択された場合はtrue、それ以外の場合はfalseを返します。</returns>
	/// <remarks>モデルがすでに選択されている場合は何も起こりません。</remarks>
    public static bool Add(AnyModel model)
	{
		if (I._models.Add(model))
		{
			ModelEventHub.NotifyModelSelectionState(model, true);
			LogHub.Info($"Selected: {model.Name}");
			return true;
		}
		return false;
	}

	/// <summary>
	/// 指定したモデル群を選択状態にします。
	/// </summary> <param name="models">選択するモデルの配列です。</param>
	public static void Add(AnyModel[] models)
	{
		foreach (var model in models)
		{
			Add(model);
		}
	}

	/// <summary>
	/// 指定したモデルを選択から外します。
	/// </summary> <param name="model">選択から外すモデルです。</param>
	/// <returns>モデルが選択から外された場合はtrue、それ以外の場合はfalseを返します。</returns>
	/// <remarks>モデルが選択されていない場合は何も起こりません。</remarks>
	public static bool Remove(AnyModel model)
	{
		if (I._models.Remove(model))
		{
			ModelEventHub.NotifyModelSelectionState(model, false);
			LogHub.Info($"Deselected: {model.Name}");
			return true;
		}
		return false;
	}

	/// <summary>
	/// 指定したモデル群を選択から外します。
	/// </summary> <param name="models">選択から外すモデルの配列です。</param>
	public static void Remove(AnyModel[] models)
	{
		foreach (var model in models)
		{
			Remove(model);
		}
	}

	/// <summary>
	/// 指定したモデルの選択状態を切り替えます。
	/// </summary> <param name="model">切り替えるモデルです。</param>
	public static void Toggle(AnyModel model)
	{
		if (I._models.Contains(model))
		{
			Remove(model);
		}
		else
		{
			Add(model);
		}
	}

	/// <summary>
	/// 指定したモデル群の選択状態を切り替えます。
	/// </summary> <param name="models">切り替えるモデルの列挙体です。</param>
	public static void Toggle(AnyModel[] models)
	{
		// 切り替えるモデルがない場合は何もしない
		if (models == null || models.Length == 0)
		{
			return;
		}

		foreach (var model in models)
		{
			Toggle(model);
		}
	}

	/// <summary>
	/// すべての選択を解除します。
	/// </summary>
	/// <returns>選択状態が変更された場合はtrue、それ以外の場合はfalseを返します。</returns>
	public static bool Clear()
	{
		if (I._models.Count == 0)
		{
			return false;
		}

		var modelsToDeselect = I._models.ToArray();
		
		// 先にクリアしてからシグナル発報することで、シグナルハンドラ内で選択状態確認した際の整合性を保つ
		I._models.Clear();

		// モデルの選択解除シグナルとハイライト解除は個々に行う
		foreach (var model in modelsToDeselect)
		{
			ModelEventHub.NotifyModelSelectionState(model, false);
			LogHub.Info($"Deselected: {model.Name}");
		}
		return true;
	}

	#endregion
}