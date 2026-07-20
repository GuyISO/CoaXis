using Godot;
using System.Collections.Generic;

/// <summary>
/// モデルのロードや状態操作を管理する Autoload ノード
/// </summary>
public partial class ModelService : Node
{
	#region Fields

	// TODO: ファイルパスの直接参照やめる
	private static Material _selectedMaterial = ResourceLoader.Load<Material>("res://assets/materials/selected.tres");

	#endregion

	#region Lifecycle

	public override void _Ready()
	{
		SubscribeApplicationEvents();
	}

	public override void _ExitTree()
	{
		UnsubscribeApplicationEvents();

		base._ExitTree();
	}

	#endregion

	#region Events

	/// <summary>
	/// Applicationイベントの購読を開始する
	/// </summary>
	private void SubscribeApplicationEvents()
	{
		Application.Selection.Event.ModelStateNotified += OnModelSelectionStateNotified;
		Application.Model.Event.ToggleModelVisibilityRequested += OnToggleModelVisibilityRequested;
		Application.Model.Event.ModelVisibilityStateNotified += OnModelVisibilityStateNotified;
	}

	/// <summary>
	/// Applicationイベントの購読を解除する
	/// </summary>
	private void UnsubscribeApplicationEvents()
	{
		Application.Selection.Event.ModelStateNotified -= OnModelSelectionStateNotified;
		Application.Model.Event.ToggleModelVisibilityRequested -= OnToggleModelVisibilityRequested;
		Application.Model.Event.ModelVisibilityStateNotified -= OnModelVisibilityStateNotified;
	}

	/// <summary>
	/// モデルの表示状態切替がリクエストされたときに呼び出されるイベントハンドラ
	/// </summary>
	/// <param name="model">表示状態を切り替えるモデル</param>
	private void OnToggleModelVisibilityRequested(AnyModel model)
	{
		var command = new SetModelVisibilityCommand([model], !model.Visible);
		Application.Command.Event.Execute(command);
	}

	/// <summary>
	/// モデルの選択状態が変更されたときに呼び出されるイベントハンドラ
	/// </summary>
	/// <param name="model">選択状態が変更されたモデル</param>
	/// <param name="isSelected">モデルが選択されている場合はtrue、選択されていない場合はfalse</param>
	private void OnModelSelectionStateNotified(AnyModel model, bool isSelected)
	{
		HighLightModel(model, isSelected);
	}

	/// <summary>
	/// モデルの表示状態が変更されたときに呼び出されるイベントハンドラ
	/// </summary>
	/// <param name="model">表示状態が変更されたモデル</param>
	/// <param name="isVisible">モデルが表示されている場合はtrue、非表示の場合はfalse</param>
	private void OnModelVisibilityStateNotified(AnyModel model, bool isVisible)
	{
		model.Visible = isVisible;
	}

	#endregion

	#region Internal Helpers

	/// <summary>
	/// 指定したモデルとその子孫のハイライト状態を切り替える
	/// </summary>
	/// <param name="model">切り替えるモデル</param>
	/// <param name="enable">ハイライトを有効にする場合はtrue、無効にする場合はfalse</param>
	private static void HighLightModel(AnyModel model, bool enable = true)
	{
		// 指定したモデルとその子孫のモデルすべてにハイライト状態を適用する
		var models = GetModelsRecursively(model);
		foreach (AnyModel targetModel in models)
		{
			HighlightMesh(targetModel, enable);
		}
	}

	/// <summary>
	/// 指定したモデルのハイライト状態を切り替える
	/// </summary>
	/// <param name="model">切り替えるモデル</param>
	/// <param name="enable">ハイライトを有効にする場合はtrue、ハイライトを解除する場合はfalse</param>
	private static void HighlightMesh(AnyModel model, bool enable = true)
	{
		if (enable)
		{
			// 選択状態の適用は子孫のMeshInstance3Dすべてにとりあえず適用すればよい
			var meshInstances = GetMeshInstancesRecursively(model);
			foreach (var meshInstance in meshInstances)
			{
				meshInstance.MaterialOverride = _selectedMaterial;
			}
		}
		else
		{
			// 選択状態の解除は、祖先のNode3Dに選択状態のものがいなければ子孫のMeshInstance3Dすべてから選択状態を解除する判定が必要
			if (!HasSelectedAncestor(model))
			{
				var meshInstances = GetMeshInstancesRecursively(model);
				foreach (var meshInstance in meshInstances)
				{
					meshInstance.MaterialOverride = null;
				}
			}
		}
	}

	/// <summary>
	/// 指定したモデルからモデルを再帰的に取得する
	/// </summary>
	/// <param name="model">取得対象のモデル</param>
	/// <returns>取得したモデルのリスト</returns>
	private static List<AnyModel> GetModelsRecursively(AnyModel model)
	{
		var models = new List<AnyModel>();

		if (model is AnyModel)
		{
			models.Add(model);
		}

		foreach (AnyModel childModel in model.ChildModels)
		{
			models.AddRange(GetModelsRecursively(childModel));
		}

		return models;
	}

	/// <summary>
	/// 指定したノードとその子孫からMeshInstance3Dを再帰的に取得する
	/// </summary>
	/// <param name="node">取得対象のノード</param>
	private static List<MeshInstance3D> GetMeshInstancesRecursively(Node node)
	{
		var meshInstances = new List<MeshInstance3D>();

		if (node is MeshInstance3D meshInstance)
		{
			meshInstances.Add(meshInstance);
		}

		foreach (Node childNode in node.GetChildren())
		{
			meshInstances.AddRange(GetMeshInstancesRecursively(childNode));
		}

		return meshInstances;
	}

	/// <summary>
	/// 指定したモデルの祖先に選択状態のモデルが存在するかどうかを判定する
	/// </summary>
	/// <param name="model">判定対象のモデル</param>
	private static bool HasSelectedAncestor(AnyModel model)
	{
		HashSet<AnyModel> visited = new HashSet<AnyModel>();

		while (model != null)
		{
			if (!visited.Add(model))
			{
				Application.Log.Warn($"HighlightService: detected cyclic ParentModel reference at '{model.Name}'.");
				return false;
			}

			if (Application.Selection.Service.Contains(model))
			{
				return true;
			}
			model = model.ParentModel;
		}
		return false;
	}

	#endregion
}