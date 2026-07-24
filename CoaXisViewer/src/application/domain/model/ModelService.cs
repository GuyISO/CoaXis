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
	private RootModel _rootModel;
	private readonly ModelGuidRegistry _modelGuidRegistry = new();

	#endregion

	#region Properties

	/// <summary>
	/// RootModel を取得する
	/// </summary>
	/// <remarks>RootModel が存在しない場合は動的に生成する</remarks>
	internal RootModel Root
	{
		get
		{
			if (_rootModel == null || !GodotObject.IsInstanceValid(_rootModel))
			{
				EnsureRootModel();
			}
			return _rootModel;
		}
	}

	/// <summary>
	/// AnyModel から Guid へのマッピング
	/// </summary>
	internal IReadOnlyDictionary<AnyModel, System.Guid> ModelToGuidMap => _modelGuidRegistry.ModelToGuidMap;

	/// <summary>
	/// Guid から AnyModel へのマッピング
	/// </summary>
	internal IReadOnlyDictionary<System.Guid, AnyModel> GuidToModelMap => _modelGuidRegistry.GuidToModelMap;

	#endregion

	#region Lifecycle

	public override void _Ready()
	{
		EnsureRootModel();
		SubscribeApplicationEvents();
		Application.Model.Event.NotifyRootModel(_rootModel);
	}

	public override void _ExitTree()
	{
		UnsubscribeApplicationEvents();
		_modelGuidRegistry.Clear();

		base._ExitTree();
	}

	#endregion

	#region Events

	/// <summary>
	/// Applicationイベントの購読を開始する
	/// </summary>
	private void SubscribeApplicationEvents()
	{
		Application.Model.Event.AskRootModelRequested += OnAskRootModelRequested;
		Application.Model.Event.AddModelRequested += OnAddModelRequested;
		Application.Model.Event.ToggleModelVisibilityRequested += OnToggleModelVisibilityRequested;
		Application.Model.Event.ModelVisibilityStateNotified += OnModelVisibilityStateNotified;
		Application.Selection.Event.ModelStateNotified += OnModelSelectionStateNotified;
	}

	/// <summary>
	/// Applicationイベントの購読を解除する
	/// </summary>
	private void UnsubscribeApplicationEvents()
	{
		Application.Model.Event.AskRootModelRequested -= OnAskRootModelRequested;
		Application.Model.Event.AddModelRequested -= OnAddModelRequested;
		Application.Model.Event.ToggleModelVisibilityRequested -= OnToggleModelVisibilityRequested;
		Application.Model.Event.ModelVisibilityStateNotified -= OnModelVisibilityStateNotified;
		Application.Selection.Event.ModelStateNotified -= OnModelSelectionStateNotified;
	}

	/// <summary>
	/// モデル追加通知を受けたときに Guid マッピングへ登録する
	/// </summary>
	private void OnAddModelRequested(AnyModel childModel, AnyModel parentModel)
	{
		_modelGuidRegistry.RegisterRecursively(childModel);
	}

	/// <summary>
	/// ルートモデル通知要求イベントのハンドラ
	/// </summary>
	private void OnAskRootModelRequested()
	{
		if (_rootModel == null || !GodotObject.IsInstanceValid(_rootModel))
		{
			EnsureRootModel();
		}

		Application.Model.Event.NotifyRootModel(_rootModel);
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
	/// モデルの表示状態が変更されたときに呼び出されるイベントハンドラ
	/// </summary>
	/// <param name="model">表示状態が変更されたモデル</param>
	/// <param name="isVisible">モデルが表示されている場合はtrue、非表示の場合はfalse</param>
	private void OnModelVisibilityStateNotified(AnyModel model, bool isVisible)
	{
		model.Visible = isVisible;
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
			// モデル自身のメッシュにのみ適用し、子モデル分は HighLightModel の再帰で処理する
			var meshInstances = GetMeshInstancesUnderModel(model);
			foreach (var meshInstance in meshInstances)
			{
				meshInstance.MaterialOverride = _selectedMaterial;
			}
		}
		else
		{
			// 選択解除時は子モデルを巻き込まず、モデル自身のメッシュのみ解除対象とする
			if (!HasSelectedAncestor(model))
			{
				var meshInstances = GetMeshInstancesUnderModel(model);
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
	/// 指定モデル配下のうち、子モデル配下を除いた MeshInstance3D を再帰的に取得する
	/// </summary>
	/// <param name="model">取得対象のモデル</param>
	private static List<MeshInstance3D> GetMeshInstancesUnderModel(AnyModel model)
	{
		var meshInstances = new List<MeshInstance3D>();
		CollectMeshInstancesUnderModel(model, meshInstances, isRoot: true);
		return meshInstances;
	}

	/// <summary>
	/// 子モデル境界で探索を止めながら MeshInstance3D を収集する
	/// </summary>
	/// <param name="node">探索対象ノード</param>
	/// <param name="results">収集先リスト</param>
	/// <param name="isRoot">探索開始ノードかどうか</param>
	private static void CollectMeshInstancesUnderModel(Node node, List<MeshInstance3D> results, bool isRoot = false)
	{
		if (!isRoot && node is AnyModel)
		{
			return;
		}

		if (node is MeshInstance3D meshInstance)
		{
			results.Add(meshInstance);
		}

		foreach (Node childNode in node.GetChildren())
		{
			CollectMeshInstancesUnderModel(childNode, results);
		}
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

	/// <summary>
	/// ModelService 直下に RootModel を動的生成する
	/// </summary>
	private void EnsureRootModel()
	{
		if (_rootModel != null && GodotObject.IsInstanceValid(_rootModel))
		{
			return;
		}

		_rootModel = new RootModel
		{
			Name = "RootModel"
		};

		AddChild(_rootModel);
		_modelGuidRegistry.RegisterRecursively(_rootModel);
	}

	#endregion
}