using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// モデルのロードや状態操作を管理する Autoload ノード
/// </summary>
public partial class ModelService : Node
{
	#region Fields

	private RootModelEntity _rootEntity = null!;
	private EmbededModelPropertyTree _embededModelPropertyTree = null!;

	#endregion

	#region Properties

	/// <summary>
	/// ルート ModelEntity を取得する
	/// </summary>
	/// <remarks>ルート ModelEntity が存在しない場合は動的に生成する</remarks>
	internal RootModelEntity RootEntity
	{
		get
		{
			if (_rootEntity == null)
			{
				EnsureRootEntity();
			}
			return _rootEntity;
		}
	}

	/// <summary>
	/// モデルの透明度を取得する
	/// </summary>
	internal float Transparency { get; private set; } = 0.0f;

	#endregion

	#region Lifecycle

	public override void _Ready()
	{
		EnsureRootEntity();
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
		Application.Model.Event.ModelStatusNotified += OnModelStatusNotified;
		Application.Selection.Event.ModelStateNotified += OnModelSelectionStateNotified;
	}

	/// <summary>
	/// Applicationイベントの購読を解除する
	/// </summary>
	private void UnsubscribeApplicationEvents()
	{
		Application.Model.Event.ToggleModelVisibilityRequested -= OnToggleModelVisibilityRequested;
		Application.Model.Event.ModelVisibilityStateNotified -= OnModelVisibilityStateNotified;
		Application.Model.Event.ModelStatusNotified -= OnModelStatusNotified;
		Application.Selection.Event.ModelStateNotified -= OnModelSelectionStateNotified;
	}

	/// <summary>
	/// モデルの表示状態切替がリクエストされたときに呼び出されるイベントハンドラ
	/// </summary>
	/// <param name="entityId">表示状態を切り替える ModelEntity の識別子</param>
	private void OnToggleModelVisibilityRequested(string entityId)
	{
		if (!Guid.TryParse(entityId, out Guid parsedEntityId) || parsedEntityId == Guid.Empty)
		{
			Application.Log.Warn($"ModelService: invalid entityId for toggle request. entityId='{entityId}'");
			return;
		}

		ModelEntity modelEntity = Application.Model.Registry.GetEntity(parsedEntityId);
		if (modelEntity == null)
		{
			Application.Log.Warn($"ModelService: toggle target not found. entityId='{parsedEntityId}'");
			return;
		}

		var command = new SetModelVisibilityCommand(
			[parsedEntityId],
			GetNextVisibility(modelEntity.Visibility));
		Application.Command.Event.Execute(command);
	}

	private static ModelVisibility GetNextVisibility(ModelVisibility visibility)
	{
		return visibility switch
		{
			ModelVisibility.Inherit => ModelVisibility.Visible,
			ModelVisibility.Visible => ModelVisibility.Invisible,
			_ => ModelVisibility.Inherit,
		};
	}

	/// <summary>
	/// モデルの表示状態が変更されたときに呼び出されるイベントハンドラ
	/// </summary>
	/// <param name="entityId">表示状態が変更された ModelEntity の識別子</param>
	/// <param name="isVisible">モデルが表示されている場合はtrue、非表示の場合はfalse</param>
	private void OnModelVisibilityStateNotified(string entityId, bool isVisible)
	{
		if (!Guid.TryParse(entityId, out Guid parsedEntityId) || parsedEntityId == Guid.Empty)
		{
			Application.Log.Warn($"ModelService: invalid entityId for visibility notification. entityId='{entityId}'");
			return;
		}

		ModelNode modelNode = Application.Model.Registry.GetEntity(parsedEntityId)?.Node;
		if (modelNode == null)
		{
			Application.Log.Warn($"ModelService: visibility target not found. entityId='{parsedEntityId}'");
			return;
		}

		modelNode.ApplyVisibilityLayer(isVisible);
	}

	/// <summary>
	/// モデルの選択状態が変更されたときに呼び出されるイベントハンドラ
	/// </summary>
	/// <param name="entityId">選択状態が変更された ModelEntity の識別子</param>
	/// <param name="isSelected">モデルが選択されている場合はtrue、選択されていない場合はfalse</param>
	private void OnModelSelectionStateNotified(string entityId, bool isSelected)
	{
		if (!Guid.TryParse(entityId, out Guid parsedEntityId) || parsedEntityId == Guid.Empty)
		{
			Application.Log.Warn($"ModelService: invalid entityId for selection notification. entityId='{entityId}'");
			return;
		}

		ModelNode modelNode = Application.Model.Registry.GetEntity(parsedEntityId)?.Node;
		if (modelNode == null)
		{
			Application.Log.Warn($"ModelService: highlight target not found. entityId='{parsedEntityId}'");
			return;
		}

		HighLightModel(modelNode, isSelected);
	}

	/// <summary>
	/// モデルのロード完了通知を受けたときに透明度を適用する
	/// </summary>
	/// <param name="entityId">ロード完了した ModelEntity の識別子</param>
	/// <param name="status">通知されたモデルの状態</param>
	private void OnModelStatusNotified(string entityId, int status)
	{
		if ((ModelStatus)status != ModelStatus.Loaded
			|| !Guid.TryParse(entityId, out Guid parsedEntityId)
			|| parsedEntityId == Guid.Empty)
		{
			return;
		}

		ModelNode modelNode = Application.Model.Registry.GetEntity(parsedEntityId)?.Node;
		if (modelNode == null || !IsInstanceValid(modelNode))
		{
			return;
		}

		ApplyModelTransparency(modelNode);
	}

	#endregion

	#region Public Methods

	/// <summary>
	/// モデルレジストリがクリアされたときに呼び出されるイベントハンドラ
	/// </summary>
	public void Clear()
	{
		// 再読み込み時に古い非同期タスクが後からモデルを更新しないよう、
		// まず待機中のロード/コライダー処理を止めてから実体を削除する。
		Application.Model.EntityFactory.ClearPendingLoads();

		var entityIds = new List<Guid>(Application.Model.Registry.Entities.Keys);
		foreach (Guid entityId in entityIds)
		{
			// ルート ModelEntity は削除対象外とする
			if (entityId == RootModelEntity.RootEntityId)
			{
				continue;
			}

			ModelEntity modelEntity = Application.Model.Registry.GetEntity(entityId);
			if (modelEntity?.Node != null && IsInstanceValid(modelEntity.Node))
			{
				modelEntity.Node.QueueFree();
			}

			Application.Model.Registry.DisposeEntity(entityId);
		}

		if (_rootEntity != null)
		{
			_rootEntity.Clear();
		}

		Application.Model.Event.NotifyRegistryCleared();
	}

	/// <summary>
	/// モデルメッシュの透明度を設定し、全ロード済みモデルに反映する
	/// </summary>
	/// <param name="value">設定する透明度値 (0.0 - 1.0)</param>
	public void SetTransparency(float value)
	{
		Transparency = value;

		// ルート ModelEntity 配下のすべてのノードに透明度を適用
		if (_rootEntity?.Node != null && IsInstanceValid(_rootEntity.Node))
		{
			ApplyModelTransparency(_rootEntity.Node);
		}

		Application.Model.Event.NotifyTransparency(value);
	}

	/// <summary>
	/// 埋め込みモデルプロパティツリーを設定する
	/// </summary>
	/// <param name="embededModelPropertyTree">設定する埋め込みモデルプロパティツリー</param>
	public void SetEmbededModelPropertyTree(EmbededModelPropertyTree embededModelPropertyTree)
	{
		_embededModelPropertyTree = embededModelPropertyTree;
	}

	#endregion

	#region Internal Helpers

	/// <summary>
	/// ModelService 直下にルート ModelEntity を動的生成する
	/// </summary>
	private void EnsureRootEntity()
	{
		_rootEntity = new RootModelEntity();

		AddChild(_rootEntity.Node);
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

	private void ApplyModelTransparency(Node node)
	{
		if (node is MeshInstance3D meshInstance)
		{
			meshInstance.Transparency = Transparency;
		}

		foreach (Node childNode in node.GetChildren())
		{
			ApplyModelTransparency(childNode);
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

			if (Application.Selection.Service.Contains(modelNode.EntityId))
			{
				return true;
			}
			modelNode = modelNode.ParentModel;
		}
		return false;
	}
	
	#endregion
}