using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// モデルの表示状態をModelNodeへ反映するAutoloadノード
/// </summary>
public partial class ModelVisualHub : BaseHub
{
	#region Properties

	/// <summary>
	/// モデルの透明度を取得する
	/// </summary>
	internal float Transparency { get; private set; } = 0.0f;

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

	#region --------------------------------------- Action ---------------------------------------

	#endregion

	#region --------------------------------------- Notification ---------------------------------------

	[Signal] public delegate void AddedEventHandler(string entityId, string parentEntityId);
	/// <summary>
	/// 登録済み部分木の追加を通知する
	/// </summary>
	/// <param name="entityId">追加する部分木ルート ModelEntity の識別子</param>
	/// <param name="parentEntityId">追加先の親 ModelEntity の識別子。Guid.Empty の場合はルートに追加される</param>
	internal void NotifyAdded(Guid entityId, Guid parentEntityId = default)
	{
		EmitSignal(SignalName.Added, entityId.ToString(), parentEntityId.ToString());
	}

	/// <summary>
	/// モデル集合が置換されたことを表す通知シグナル
	/// </summary>
	[Signal] public delegate void ModelSetReplacedEventHandler();
	/// <summary>
	/// Registryのモデル集合が置換されたことを通知する
	/// </summary>
	internal void NotifyModelSetReplaced()
	{
		EmitSignal(SignalName.ModelSetReplaced);
	}

	[Signal] public delegate void PositionNotifiedEventHandler(string entityId, Vector3 position);
	/// <summary>
	/// モデルの配置位置を通知する
	/// </summary>
	/// <param name="entityId">配置位置が変化した ModelEntity の識別子</param>
	/// <param name="position">変更後の配置位置（Godot座標系）</param>
	internal void NotifyPosition(Guid entityId, Vector3 position)
	{
		EmitSignal(SignalName.PositionNotified, entityId.ToString(), position);
	}

	[Signal] public delegate void RotationNotifiedEventHandler(string entityId, Quaternion rotation);
	/// <summary>
	/// モデルの回転を通知する
	/// </summary>
	/// <param name="entityId">回転が変化した ModelEntity の識別子</param>
	/// <param name="rotation">変更後の回転（Godot座標系）</param>
	internal void NotifyRotation(Guid entityId, Quaternion rotation)
	{
		EmitSignal(SignalName.RotationNotified, entityId.ToString(), rotation);
	}

	[Signal] public delegate void VisibilityNotifiedEventHandler(string entityId, ModelVisibility visibility);
	/// <summary>
	/// モデルの表示状態の通知を行う
	/// </summary>
	/// <param name="entityId">表示状態が変化した ModelEntity の識別子</param>
	/// <param name="visibility">変更後のモデル表示設定</param>
	internal void NotifyVisibility(Guid entityId, ModelVisibility visibility)
	{
		EmitSignal(SignalName.VisibilityNotified, entityId.ToString(), (int)visibility);
	}

	[Signal] public delegate void CollapsedEventHandler(string entityId, bool isCollapsed);
	/// <summary>
	/// モデルツリーの折りたたみを通知する
	/// </summary>
	/// <param name="entityId">折りたたまれた ModelEntity の識別子</param>
	/// <param name="isCollapsed">モデルツリーが折りたたまれている場合はtrue、展開されている場合はfalse</param>
	internal void NotifyCollapsed(Guid entityId, bool isCollapsed)
	{
		EmitSignal(SignalName.Collapsed, entityId.ToString(), isCollapsed);
	}

	[Signal] public delegate void StatusNotifiedEventHandler(string entityId, int status);
	/// <summary>
	/// モデルのロード状態が変化したことを通知する
	/// </summary>
	/// <param name="entityId">状態が変化した ModelEntity の識別子</param>
	/// <param name="status">新しい状態</param>
	internal void NotifyStatus(Guid entityId, ModelStatus status)
	{
		EmitSignal(SignalName.StatusNotified, entityId.ToString(), (int)status);
	}

	[Signal] public delegate void TransparencyNotifiedEventHandler(float transparency);
	/// <summary>
	/// モデルの透明度を通知する
	/// </summary>
	/// <param name="transparency">新しい透明度</param>
	internal void NotifyTransparency(float transparency)
	{
		EmitSignal(SignalName.TransparencyNotified, transparency);
	}

	[Signal] public delegate void RegistryClearedEventHandler();
	/// <summary>
	/// モデルレジストリがクリアされたことを通知する
	/// </summary>
	internal void NotifyRegistryCleared()
	{
		EmitSignal(SignalName.RegistryCleared);
	}

	#endregion
    
	#region Events

	/// <summary>
	/// Applicationイベントの購読を開始する
	/// </summary>
	private void SubscribeApplicationEvents()
	{
		Application.Model.State.PositionNotified += OnModelPositionNotified;
		Application.Model.State.RotationNotified += OnModelRotationNotified;
		Application.Model.State.VisibilityNotified += OnModelVisibilityNotified;
		Application.Model.Registry.Added += OnModelAdded;
		Application.Model.Registry.ModelSetReplaced += OnModelSetReplaced;
		Application.Model.State.StatusNotified += OnModelStatusNotified;
		Application.Selection.ModelStateNotified += OnModelSelectionStateNotified;
	}

	/// <summary>
	/// Applicationイベントの購読を解除する
	/// </summary>
	private void UnsubscribeApplicationEvents()
	{
		Application.Model.State.PositionNotified -= OnModelPositionNotified;
		Application.Model.State.RotationNotified -= OnModelRotationNotified;
		Application.Model.State.VisibilityNotified -= OnModelVisibilityNotified;
		Application.Model.Registry.Added -= OnModelAdded;
		Application.Model.Registry.ModelSetReplaced -= OnModelSetReplaced;
		Application.Model.State.StatusNotified -= OnModelStatusNotified;
		Application.Selection.ModelStateNotified -= OnModelSelectionStateNotified;
	}

	/// <summary>
	/// モデルの配置位置が通知されたときに呼び出されるイベントハンドラ
	/// </summary>
	/// <param name="entityId">配置位置が変更された ModelEntity の識別子</param>
	/// <param name="position">変更後の配置位置（Godot座標系）</param>
	private void OnModelPositionNotified(string entityId, Vector3 position)
	{
		if (!Guid.TryParse(entityId, out Guid parsedEntityId) || parsedEntityId == Guid.Empty)
		{
			Application.Log.Warn($"ModelPresentationService: invalid entityId for position notification. entityId='{entityId}'");
			return;
		}

		ModelEntity modelEntity = Application.Model.Registry.GetEntity(parsedEntityId);
		if (modelEntity == null)
		{
			Application.Log.Warn($"ModelPresentationService: position target not found. entityId='{parsedEntityId}'");
			return;
		}

		// エンティティと描画ノードの座標を同時に更新し、再生成後も同じ配置を維持する。
		modelEntity.Position = position;
		if (modelEntity.Node != null && IsInstanceValid(modelEntity.Node))
		{
			modelEntity.Node.Position = position;
		}
	}

	/// <summary>
	/// モデルの回転が通知されたときに呼び出されるイベントハンドラ
	/// </summary>
	/// <param name="entityId">回転が変更された ModelEntity の識別子</param>
	/// <param name="rotation">変更後の回転（Godot座標系）</param>
	private void OnModelRotationNotified(string entityId, Quaternion rotation)
	{
		if (!Guid.TryParse(entityId, out Guid parsedEntityId) || parsedEntityId == Guid.Empty)
		{
			Application.Log.Warn($"ModelPresentationService: invalid entityId for rotation notification. entityId='{entityId}'");
			return;
		}

		ModelEntity modelEntity = Application.Model.Registry.GetEntity(parsedEntityId);
		if (modelEntity == null)
		{
			Application.Log.Warn($"ModelPresentationService: rotation target not found. entityId='{parsedEntityId}'");
			return;
		}

		// エンティティと描画ノードの姿勢を同時に更新し、再生成後も同じ回転を維持する。
		modelEntity.Rotation = rotation;
		if (modelEntity.Node != null && IsInstanceValid(modelEntity.Node))
		{
			modelEntity.Node.Quaternion = rotation;
		}
	}

	/// <summary>
	/// モデルの表示状態が変更されたときに呼び出されるイベントハンドラ
	/// </summary>
	/// <param name="entityId">表示状態が変更された ModelEntity の識別子</param>
	/// <param name="visibility">変更後のモデル表示設定</param>
	private void OnModelVisibilityNotified(string entityId, ModelVisibility visibility)
	{
		if (!Guid.TryParse(entityId, out Guid parsedEntityId) || parsedEntityId == Guid.Empty)
		{
			Application.Log.Warn($"ModelPresentationService: invalid entityId for visibility notification. entityId='{entityId}'");
			return;
		}

		// ModelEntity の内部的な表示状態を更新
		ModelEntity modelEntity = Application.Model.Registry.GetEntity(parsedEntityId);
		if (modelEntity == null)
		{
			Application.Log.Warn($"ModelPresentationService: visibility target not found. entityId='{parsedEntityId}'");
			return;
		}
		
		ApplyVisibility(modelEntity, visibility);
	}

	/// <summary>
	/// 追加された部分木の初期表示状態を反映する
	/// </summary>
	/// <param name="entityId">追加された部分木ルートの識別子</param>
	/// <param name="parentEntityId">追加先の親モデルの識別子</param>
	private void OnModelAdded(string entityId, string parentEntityId)
	{
		if (!Guid.TryParse(entityId, out Guid parsedEntityId) || parsedEntityId == Guid.Empty)
		{
			return;
		}

		ModelEntity modelEntity = Application.Model.Registry.GetEntity(parsedEntityId);
		if (modelEntity == null)
		{
			return;
		}

		ApplyVisibilitySubtree(modelEntity);
	}

	/// <summary>
	/// 全件置換後に登録済みモデルの初期表示状態を同期する
	/// </summary>
	private void OnModelSetReplaced()
	{
		foreach (ModelEntity modelEntity in Application.Model.Registry.Entities.Values)
		{
			if (modelEntity.Id != RootModelEntity.RootEntityId)
			{
				ApplyVisibility(modelEntity, modelEntity.Visibility);
			}
		}
	}

	/// <summary>
	/// 部分木の各Entityへ初期表示状態を反映する
	/// </summary>
	/// <param name="rootEntity">反映対象の部分木ルート</param>
	private void ApplyVisibilitySubtree(ModelEntity rootEntity)
	{
		var pendingEntities = new Stack<ModelEntity>();
		var visitedEntityIds = new HashSet<Guid>();
		pendingEntities.Push(rootEntity);

		while (pendingEntities.Count > 0)
		{
			ModelEntity modelEntity = pendingEntities.Pop();
			if (modelEntity == null || !visitedEntityIds.Add(modelEntity.Id))
			{
				continue;
			}

			ApplyVisibility(modelEntity, modelEntity.Visibility);
			foreach (ModelEntity childEntity in modelEntity.Children)
			{
				pendingEntities.Push(childEntity);
			}
		}
	}

	private void ApplyVisibility(ModelEntity modelEntity, ModelVisibility visibility)
	{
		modelEntity.Visibility = visibility;
		ModelNode modelNode = modelEntity.Node;
		if (modelNode == null || !IsInstanceValid(modelNode))
		{
			Application.Log.Warn($"ModelPresentationService: visibility target not found. entityId='{modelEntity.Id}'");
			return;
		}

		// Inherit は階層全体が確定した後に親の設定から実効状態を解決する。
		bool isVisible = visibility switch
		{
			ModelVisibility.Visible => true,
			ModelVisibility.Invisible => false,
			_ => ModelVisibilityResolver.IsVisible(modelEntity),
		};
		modelNode.ApplyVisibilityLayer(isVisible);
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

	/// <summary>
	/// モデルの選択状態が変更されたときに呼び出されるイベントハンドラ
	/// </summary>
	/// <param name="entityId">選択状態が変更された ModelEntity の識別子</param>
	/// <param name="isSelected">モデルが選択されている場合はtrue、選択されていない場合はfalse</param>
	private void OnModelSelectionStateNotified(string entityId, bool isSelected)
	{
		if (!Guid.TryParse(entityId, out Guid parsedEntityId) || parsedEntityId == Guid.Empty)
		{
			Application.Log.Warn($"ModelPresentationService: invalid entityId for selection notification. entityId='{entityId}'");
			return;
		}

		ModelNode modelNode = Application.Model.Registry.GetEntity(parsedEntityId)?.Node;
		if (modelNode == null)
		{
			Application.Log.Warn($"ModelPresentationService: highlight target not found. entityId='{parsedEntityId}'");
			return;
		}

		HighLightModel(modelNode, isSelected);
	}

	#endregion

	#region Public Methods

	/// <summary>
	/// モデルメッシュの透明度を設定し、全ロード済みモデルに反映する
	/// </summary>
	/// <param name="value">設定する透明度値 (0.0 - 1.0)</param>
	public void SetTransparency(float value)
	{
		Transparency = value;

		// ルート ModelEntity 配下のすべてのノードに透明度を適用
		RootModelEntity rootEntity = Application.Model.Registry.RootEntity;
		if (rootEntity?.Node != null && IsInstanceValid(rootEntity.Node))
		{
			ApplyModelTransparency(rootEntity.Node);
		}

		Application.Model.State.NotifyTransparency(value);
	}

	#endregion

	#region Internal Helpers

	/// <summary>
	/// 指定したモデルとその子孫のハイライト状態を切り替える
	/// </summary>
	/// <param name="modelNode">切り替えるモデル</param>
	/// <param name="enable">ハイライトを有効にする場合はtrue、無効にする場合はfalse</param>
	private static void HighLightModel(ModelNode modelNode, bool enable = true)
	{
		ModelEntity modelEntity = Application.Model.Registry.GetEntity(modelNode.EntityId);
		if (modelEntity == null)
		{
			return;
		}

		// 論理階層はRegistryで解決し、描画ノードへの反映だけをModelPresentationServiceが担当する。
		var modelEntities = new List<ModelEntity> { modelEntity };
		modelEntities.AddRange(Application.Model.Registry.GetDescendantEntities(modelEntity.Id));
		foreach (ModelEntity targetModelEntity in modelEntities)
		{
			if (targetModelEntity.Node != null && IsInstanceValid(targetModelEntity.Node))
			{
				HighlightMesh(targetModelEntity.Node, enable);
			}
		}
	}

	/// <summary>
	/// 指定したモデルのハイライト状態を切り替える
	/// </summary>
	/// <param name="modelNode">切り替えるモデル</param>
	/// <param name="enable">ハイライトを有効にする場合はtrue、ハイライトを解除する場合はfalse</param>
	private static void HighlightMesh(ModelNode modelNode, bool enable = true)
	{
		Material selectedMaterial = Application.Asset.GetSelectedMaterial();

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

			if (Application.Selection.Contains(modelNode.EntityId))
			{
				return true;
			}
			modelNode = modelNode.ParentModel;
		}
		return false;
	}
	
	#endregion
}