using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// モデルの表示状態と描画ノードへの反映を管理するハブ。
/// </summary>
public partial class ModelEntityVisualHub : BaseHub
{
	#region Fields

	#endregion

	#region Properties

	/// <summary>モデル全体の透明度を取得する。</summary>
	public float Transparency { get; private set; } = 0.0f;

	#endregion

	#region Signals

	/// <summary>モデルの透明度の変更通知。</summary>
	[Signal] public delegate void TransparencyNotifiedEventHandler();

	/// <summary>Visibility設定を受けて実効表示の再解決と描画反映が完了した通知。</summary>
	/// <param name="entityId">解決・反映したModelEntityの識別子</param>
	[Signal] public delegate void VisibilityResolvedEventHandler(string entityId);

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
	/// Applicationイベントの購読を開始する。
	/// </summary>
	private void SubscribeApplicationEvents()
	{
		Application.Model.Entity.Registry.Registered += OnRegistered;
		Application.Model.Entity.State.VisibilityNotified += OnVisibilityNotified;
		Application.Model.Entity.State.StatusNotified += OnStatusNotified;
		Application.Model.Entity.Selection.Selected += OnSelected;
	}

	/// <summary>
	/// Applicationイベントの購読を解除する。
	/// </summary>
	private void UnsubscribeApplicationEvents()
	{
		Application.Model.Entity.Registry.Registered -= OnRegistered;
		Application.Model.Entity.State.VisibilityNotified -= OnVisibilityNotified;
		Application.Model.Entity.State.StatusNotified -= OnStatusNotified;
		Application.Model.Entity.Selection.Selected -= OnSelected;
	}

	/// <summary>
	/// 登録済みモデルの初期表示状態を同期する。
	/// </summary>
	private void OnRegistered()
	{
		foreach (ModelEntity modelEntity in Application.Model.Entity.Registry.Items.Values)
		{
			if (modelEntity.Id != RootModelEntity.RootEntityId)
			{
				ResolveAndApplyVisibility(modelEntity);
			}
		}
	}

	/// <summary>
	/// 表示状態の変更通知を受け、階層全体の実効表示を再解決してModelNodeへ反映する。
	/// </summary>
	/// <param name="entityId">表示状態が変更された ModelEntity の識別子</param>
	private void OnVisibilityNotified(string entityId)
	{
		if (!Guid.TryParse(entityId, out Guid parsedEntityId) || parsedEntityId == Guid.Empty)
		{
			Application.Log.Warn($"ModelEntityVisualHub: invalid entityId for visibility notification. entityId='{entityId}'");
			return;
		}

		// 設定変更の影響範囲をVisualHubが展開し、階層状態の再計算を一元化する。
		ModelEntity modelEntity = Application.Model.Entity.Registry.Get(parsedEntityId);
		if (modelEntity == null)
		{
			Application.Log.Warn($"ModelEntityVisualHub: visibility target not found. entityId='{parsedEntityId}'");
			return;
		}
		
		ResolveAndApplyVisibility(modelEntity);
		foreach (ModelEntity descendant in Application.Model.Entity.Registry.GetDescendants(modelEntity.Id))
		{
			ResolveAndApplyVisibility(descendant);
		}
	}

	/// <summary>
	/// モデルのロード完了通知を受けたときに透明度を適用する。
	/// </summary>
	/// <param name="entityId">ロード完了した ModelEntity の識別子</param>
	private void OnStatusNotified(string entityId)
	{
		if (!Guid.TryParse(entityId, out Guid parsedEntityId) || parsedEntityId == Guid.Empty)
		{
			return;
		}

		ModelEntity modelEntity = Application.Model.Entity.Registry.Get(parsedEntityId);
		if (modelEntity == null)
		{
			return;
		}

		if (modelEntity.Status == ModelStatus.Registered)
		{
			return;
		}

		ResolveAndApplyVisibility(modelEntity);

		ModelNode modelNode = modelEntity.Node;
		if (modelNode == null || !IsInstanceValid(modelNode))
		{
			return;
		}

		if (modelEntity.Status != ModelStatus.Loaded)
		{
			return;
		}

		ApplyModelTransparency(modelNode);
	}

	/// <summary>
	/// モデルの選択状態が変更されたときに呼び出されるイベントハンドラ。
	/// </summary>
	/// <param name="entityId">選択状態が変更された ModelEntity の識別子</param>
	private void OnSelected(string entityId)
	{
		if (!Guid.TryParse(entityId, out Guid parsedEntityId) || parsedEntityId == Guid.Empty)
		{
			Application.Log.Warn($"ModelEntityVisualHub: invalid entityId for selection notification. entityId='{entityId}'");
			return;
		}

		ModelNode modelNode = Application.Model.Entity.Registry.Get(parsedEntityId)?.Node;
		if (modelNode == null)
		{
			Application.Log.Warn($"ModelEntityVisualHub: highlight target not found. entityId='{parsedEntityId}'");
			return;
		}

		HighLightModel(modelNode, Application.Model.Entity.Selection.Contains(parsedEntityId));
	}

	#endregion

	#region Methods

	/// <summary>
	/// モデルメッシュの透明度を設定し、全ロード済みモデルに反映する。
	/// </summary>
	/// <param name="value">設定する透明度値 (0.0 - 1.0)</param>
	public void SetTransparency(float value)
	{
		Transparency = value;

		// ルート ModelEntity 配下のすべてのノードに透明度を適用
		RootModelEntity rootEntity = Application.Model.Entity.Registry.Root;
		if (rootEntity?.Node != null && IsInstanceValid(rootEntity.Node))
		{
			ApplyModelTransparency(rootEntity.Node);
		}

		EmitSignal(SignalName.TransparencyNotified);
	}

	#endregion

	#region Helpers

	/// <summary>
	/// ModelEntityの実効表示を導出し、描画Layerへ反映してUIへ通知する。
	/// </summary>
	private void ResolveAndApplyVisibility(ModelEntity modelEntity)
	{
		bool isVisible = modelEntity.IsVisible;
		ModelNode modelNode = modelEntity.Node;
		if (modelNode != null && IsInstanceValid(modelNode))
		{
			// 描画Layerは実効状態の投影であり、別の可視状態として読み戻さない。
			modelNode.ApplyVisibilityLayer(isVisible);
		}

		EmitSignal(SignalName.VisibilityResolved, modelEntity.Id.ToString());
	}

	/// <summary>
	/// 指定したモデルとその子孫のハイライト状態を切り替える。
	/// </summary>
	/// <param name="modelNode">切り替えるモデル</param>
	/// <param name="enable">ハイライトを有効にする場合はtrue、無効にする場合はfalse</param>
	private static void HighLightModel(ModelNode modelNode, bool enable = true)
	{
		ModelEntity modelEntity = Application.Model.Entity.Registry.Get(modelNode.EntityId);
		if (modelEntity == null)
		{
			return;
		}

		// 論理階層はRegistryで解決し、描画ノードへの反映だけをModelEntityVisualHubが担当する。
		var modelEntities = new List<ModelEntity> { modelEntity };
		modelEntities.AddRange(Application.Model.Entity.Registry.GetDescendants(modelEntity.Id));
		foreach (ModelEntity targetModelEntity in modelEntities)
		{
			if (targetModelEntity.Node != null && IsInstanceValid(targetModelEntity.Node))
			{
				HighlightMesh(targetModelEntity.Node, enable);
			}
		}
	}

	/// <summary>
	/// 指定したモデルのハイライト状態を切り替える。
	/// </summary>
	/// <param name="modelNode">切り替えるモデル</param>
	/// <param name="enable">ハイライトを有効にする場合はtrue、ハイライトを解除する場合はfalse</param>
	private static void HighlightMesh(ModelNode modelNode, bool enable = true)
	{
		Material selectedMaterial = Application.Asset.Material.GetSelected();

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
	/// 指定したノードとその子孫からMeshInstance3Dを再帰的に取得する。
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
	/// 指定モデル配下のうち、子モデル配下を除いた MeshInstance3D を再帰的に取得する。
	/// </summary>
	/// <param name="modelNode">取得対象のモデル</param>
	private static List<MeshInstance3D> GetMeshInstancesUnderModel(ModelNode modelNode)
	{
		var meshInstances = new List<MeshInstance3D>();
		CollectMeshInstancesUnderModel(modelNode, meshInstances, isRoot: true);
		return meshInstances;
	}

	/// <summary>
	/// 子モデル境界で探索を止めながら MeshInstance3D を収集する。
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
	/// ノード配下へ現在の透明度を再帰的に適用する。
	/// </summary>
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
	/// 指定したモデルの祖先に選択状態のモデルが存在するかどうかを判定する。
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

			if (Application.Model.Entity.Selection.Contains(modelNode.EntityId))
			{
				return true;
			}
			modelNode = modelNode.ParentModel;
		}
		return false;
	}

	#endregion
}