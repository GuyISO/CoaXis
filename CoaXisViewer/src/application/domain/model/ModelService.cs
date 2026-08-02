using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// モデルのロードや状態操作を管理する Autoload ノード
/// </summary>
public partial class ModelService : Node
{
	#region Fields

	private RootModelData _root = null!;
	
	#endregion

	#region Properties

	/// <summary>
	/// RootModel を取得する
	/// </summary>
	/// <remarks>RootModel が存在しない場合は動的に生成する</remarks>
	internal RootModelData Root
	{
		get
		{
			if (_root == null)
			{
				EnsureRootModel();
			}
			return _root;
		}
	}

	#endregion

	#region Lifecycle

	public override void _Ready()
	{
		EnsureRootModel();
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
		Application.Model.Event.ToggleModelVisibilityRequested += OnToggleModelVisibilityRequested;
		Application.Model.Event.ModelVisibilityStateNotified += OnModelVisibilityStateNotified;
		Application.Selection.Event.ModelStateNotified += OnModelSelectionStateNotified;
	}

	/// <summary>
	/// Applicationイベントの購読を解除する
	/// </summary>
	private void UnsubscribeApplicationEvents()
	{
		Application.Model.Event.ToggleModelVisibilityRequested -= OnToggleModelVisibilityRequested;
		Application.Model.Event.ModelVisibilityStateNotified -= OnModelVisibilityStateNotified;
		Application.Selection.Event.ModelStateNotified -= OnModelSelectionStateNotified;
	}

	/// <summary>
	/// モデルの表示状態切替がリクエストされたときに呼び出されるイベントハンドラ
	/// </summary>
	/// <param name="modelId">表示状態を切り替えるモデル識別子</param>
	private void OnToggleModelVisibilityRequested(string modelId)
	{
		if (!Guid.TryParse(modelId, out Guid parsedModelId) || parsedModelId == Guid.Empty)
		{
			Application.Log.Warn($"ModelService: invalid modelId for toggle request. modelId='{modelId}'");
			return;
		}

		ModelNode modelNode = FindModelNodeById(parsedModelId);
		if (modelNode == null)
		{
			Application.Log.Warn($"ModelService: toggle target not found. modelId='{parsedModelId}'");
			return;
		}

		var command = new SetModelVisibilityCommand([parsedModelId], !modelNode.Visible);
		Application.Command.Event.Execute(command);
	}

	/// <summary>
	/// モデルの表示状態が変更されたときに呼び出されるイベントハンドラ
	/// </summary>
	/// <param name="modelId">表示状態が変更されたモデル識別子</param>
	/// <param name="isVisible">モデルが表示されている場合はtrue、非表示の場合はfalse</param>
	private void OnModelVisibilityStateNotified(string modelId, bool isVisible)
	{
		if (!Guid.TryParse(modelId, out Guid parsedModelId) || parsedModelId == Guid.Empty)
		{
			Application.Log.Warn($"ModelService: invalid modelId for visibility notification. modelId='{modelId}'");
			return;
		}

		ModelNode modelNode = FindModelNodeById(parsedModelId);
		if (modelNode == null)
		{
			Application.Log.Warn($"ModelService: visibility target not found. modelId='{parsedModelId}'");
			return;
		}

		modelNode.Visible = isVisible;
	}

	/// <summary>
	/// モデルの選択状態が変更されたときに呼び出されるイベントハンドラ
	/// </summary>
	/// <param name="modelId">選択状態が変更されたモデル識別子</param>
	/// <param name="isSelected">モデルが選択されている場合はtrue、選択されていない場合はfalse</param>
	private void OnModelSelectionStateNotified(string modelId, bool isSelected)
	{
		if (!Guid.TryParse(modelId, out Guid parsedModelId) || parsedModelId == Guid.Empty)
		{
			Application.Log.Warn($"ModelService: invalid modelId for selection notification. modelId='{modelId}'");
			return;
		}

		ModelNode modelNode = FindModelNodeById(parsedModelId);
		if (modelNode == null)
		{
			Application.Log.Warn($"ModelService: highlight target not found. modelId='{parsedModelId}'");
			return;
		}

		HighLightModel(modelNode, isSelected);
	}

	#endregion

	#region Internal Helpers

	/// <summary>
	/// ModelService 直下に RootModel を動的生成する
	/// </summary>
	private void EnsureRootModel()
	{
		_root = new RootModelData();

		AddChild(_root.Node);
	}

	/// <summary>
	/// 指定したモデルとその子孫のハイライト状態を切り替える
	/// </summary>
	/// <param name="modelNode">切り替えるモデル</param>
	/// <param name="enable">ハイライトを有効にする場合はtrue、無効にする場合はfalse</param>
	private static void HighLightModel(ModelNode modelNode, bool enable = true)
	{
		// 指定したモデルとその子孫のモデルすべてにハイライト状態を適用する
		var modelNodes = GetModelsRecursively(modelNode);
		foreach (ModelNode targetModelNode in modelNodes)
		{
			HighlightMesh(targetModelNode, enable);
		}
	}

	/// <summary>
	/// 指定したモデルのハイライト状態を切り替える
	/// </summary>
	/// <param name="modelNode">切り替えるモデル</param>
	/// <param name="enable">ハイライトを有効にする場合はtrue、ハイライトを解除する場合はfalse</param>
	private static void HighlightMesh(ModelNode modelNode, bool enable = true)
	{
		Material selectedMaterial = Application.Asset.Service.GetSelectedMaterial();

		if (enable)
		{
			if (selectedMaterial == null)
			{
				return;
			}

			// モデル自身のメッシュにのみ適用し、子モデル分は HighLightModel の再帰で処理する
			var meshInstances = GetMeshInstancesUnderModel(modelNode);
			foreach (var meshInstance in meshInstances)
			{
				meshInstance.MaterialOverride = selectedMaterial;
			}
		}
		else
		{
			// 選択解除時は子モデルを巻き込まず、モデル自身のメッシュのみ解除対象とする
			if (!HasSelectedAncestor(modelNode))
			{
				var meshInstances = GetMeshInstancesUnderModel(modelNode);
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
	/// <param name="modelNode">取得対象のモデル</param>
	/// <returns>取得したモデルのリスト</returns>
	private static List<ModelNode> GetModelsRecursively(ModelNode modelNode)
	{
		var modelNodes = new List<ModelNode>();

		if (modelNode is ModelNode)
		{
			modelNodes.Add(modelNode);
		}

		foreach (ModelNode childModelNode in modelNode.ChildModels)
		{
			modelNodes.AddRange(GetModelsRecursively(childModelNode));
		}

		return modelNodes;
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
	/// <param name="modelNode">取得対象のモデル</param>
	private static List<MeshInstance3D> GetMeshInstancesUnderModel(ModelNode modelNode)
	{
		var meshInstances = new List<MeshInstance3D>();
		CollectMeshInstancesUnderModel(modelNode, meshInstances, isRoot: true);
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
		if (!isRoot && node is ModelNode)
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
	/// <param name="modelNode">判定対象のモデル</param>
	private static bool HasSelectedAncestor(ModelNode modelNode)
	{
		HashSet<ModelNode> visited = new HashSet<ModelNode>();

		while (modelNode != null)
		{
			if (!visited.Add(modelNode))
			{
				Application.Log.Warn($"HighlightService: detected cyclic ParentModel reference at '{modelNode.Name}'.");
				return false;
			}

			if (Application.Selection.Service.Contains(modelNode.ModelId))
			{
				return true;
			}
			modelNode = modelNode.ParentModel;
		}
		return false;
	}

	private ModelNode FindModelNodeById(Guid modelId)
	{
		if (modelId == Guid.Empty)
		{
			return null;
		}

		RootModelNode rootModelNode = (RootModelNode)Root.Node;
		return FindModelNodeByIdRecursive(rootModelNode, modelId);
	}

	private static ModelNode FindModelNodeByIdRecursive(ModelNode modelNode, Guid modelId)
	{
		if (modelNode == null)
		{
			return null;
		}

		if (modelNode.ModelId == modelId)
		{
			return modelNode;
		}

		foreach (ModelNode childModelNode in modelNode.ChildModels)
		{
			ModelNode found = FindModelNodeByIdRecursive(childModelNode, modelId);
			if (found != null)
			{
				return found;
			}
		}

		return null;
	}

	#endregion
}