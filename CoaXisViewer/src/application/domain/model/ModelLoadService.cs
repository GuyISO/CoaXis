using CoaXis.Protocol.Viewer;
using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// モデルDTOの置換ロードを統括するサービス
/// </summary>
public partial class ModelLoadService : Node
{
	#region Public API

	/// <summary>
	/// 現在のモデル集合をクリアし、指定したDTOからモデル集合を再構築する
	/// </summary>
	/// <param name="entityDtos">読み込むモデル実体DTOの集合</param>
	/// <returns>登録およびシーン反映を開始したModelEntityの一覧</returns>
	/// <exception cref="ArgumentNullException">entityDtosがnullの場合</exception>
	public IReadOnlyList<ModelEntity> ReplaceEntities(IReadOnlyList<ModelEntityDto> entityDtos)
	{
		if (entityDtos == null)
		{
			throw new ArgumentNullException(nameof(entityDtos));
		}

		ClearModels();
		IReadOnlyList<ModelEntity> entities = ModelEntityFactory.Create(entityDtos);
		IReadOnlyList<ModelEntity> notificationOrder = OrderParentFirst(entities);

		// 全件を登録してから階層を解決することで、入力順に依存せず親子関係を確定する。
		foreach (ModelEntity modelEntity in entities)
		{
			Application.Model.Scene.MarkInitialized(modelEntity);
			Application.Model.Registry.RegisterEntity(modelEntity);
		}
		Application.Model.Registry.ResolveHierarchy();

		foreach (ModelEntity modelEntity in entities)
		{
			modelEntity.Node = EnsureNode(modelEntity);
		}

		Application.Model.Scene.PrepareLoads(entities);

		// TreeItem は親の通知時点で親が存在する必要があるため、通知だけ親先行にする。
		foreach (ModelEntity modelEntity in notificationOrder)
		{
			Application.Model.Event.NotifyVisibility(modelEntity.Id, modelEntity.Visibility);
			Application.Model.Event.NotifyAdded(modelEntity.Id, modelEntity.ParentId);
		}

		Application.Model.Scene.StartPendingLoads();
		return entities;
	}

	/// <summary>
	/// 指定したDTOからモデル属性を生成し、Registryへ登録して階層を解決する
	/// </summary>
	/// <param name="propertyDtos">読み込むモデル属性DTOの集合</param>
	/// <returns>登録されたModelPropertyの一覧</returns>
	/// <exception cref="ArgumentNullException">propertyDtosがnullの場合</exception>
	public IReadOnlyList<ModelProperty> LoadProperties(IReadOnlyList<ModelPropertyDto> propertyDtos)
	{
		if (propertyDtos == null)
		{
			throw new ArgumentNullException(nameof(propertyDtos));
		}

		IReadOnlyList<ModelProperty> properties = ModelPropertyFactory.Create(propertyDtos);
		// 全件を登録してから階層を解決することで、入力順に依存せず親子関係を確定する。
		foreach (ModelProperty property in properties)
		{
			Application.Model.Registry.RegisterProperty(property);
		}
		Application.Model.Registry.ResolveHierarchy();
		return properties;
	}

	/// <summary>
	/// 現在のモデル集合と保留中のシーンロードをクリアする
	/// </summary>
	public void ClearModels()
	{
		// 旧世代を先に無効化してからRegistryをクリアし、遅延完了したロードが古いノードを更新しないようにする。
		Application.Model.Scene.CancelPendingLoads();
		Application.Model.Registry.Clear();
	}

	#endregion

	#region Internal Helpers

	private static IReadOnlyList<ModelEntity> OrderParentFirst(IReadOnlyList<ModelEntity> entities)
	{
		var ordered = new List<ModelEntity>(entities.Count);
		var entityById = new Dictionary<Guid, ModelEntity>(entities.Count);
		foreach (ModelEntity modelEntity in entities)
		{
			entityById[modelEntity.Id] = modelEntity;
		}

		var visiting = new HashSet<Guid>();
		var visited = new HashSet<Guid>();
		foreach (ModelEntity modelEntity in entities)
		{
			AddParentFirst(modelEntity, entityById, visiting, visited, ordered);
		}

		return ordered;
	}

	private static void AddParentFirst(
		ModelEntity modelEntity,
		IReadOnlyDictionary<Guid, ModelEntity> entityById,
		ISet<Guid> visiting,
		ISet<Guid> visited,
		ICollection<ModelEntity> ordered)
	{
		if (visited.Contains(modelEntity.Id))
		{
			return;
		}

		// 循環した入力は通知順序を確定できないため、登録前に明示的に拒否する。
		if (!visiting.Add(modelEntity.Id))
		{
			throw new ArgumentException($"Circular model hierarchy detected at '{modelEntity.Id}'.", nameof(entityById));
		}

		if (modelEntity.ParentId != Guid.Empty &&
			entityById.TryGetValue(modelEntity.ParentId, out ModelEntity parentEntity))
		{
			AddParentFirst(parentEntity, entityById, visiting, visited, ordered);
		}

		visiting.Remove(modelEntity.Id);
		visited.Add(modelEntity.Id);
		ordered.Add(modelEntity);
	}

	private static ModelNode EnsureNode(ModelEntity modelEntity)
	{
		if (modelEntity == null)
		{
			throw new ArgumentNullException(nameof(modelEntity));
		}

		// 既存ノードが有効なら再利用し、再読込時の重複生成を防ぐ。
		if (modelEntity.Node != null && GodotObject.IsInstanceValid(modelEntity.Node))
		{
			return modelEntity.Node;
		}

		var node = new ModelNode(modelEntity.Id);
		modelEntity.Node = node;
		node.Name = modelEntity.Id.ToString();
		node.Position = modelEntity.Position;
		node.Quaternion = modelEntity.Rotation;

		ModelNode parentNode = ResolveParentNode(modelEntity.ParentId);
		if (parentNode != null)
		{
			parentNode.AddChild(node);
		}
		else
		{
			ModelNode rootNode = Application.Model.Registry.RootEntity?.Node;
			if (rootNode != null)
			{
				rootNode.AddChild(node);
			}
			else
			{
				Application.Log.Warn($"ModelLoadService: parent node not found for entityId='{modelEntity.Id}'.");
			}
		}

		return node;
	}

	private static ModelNode ResolveParentNode(Guid parentId)
	{
		if (parentId == Guid.Empty)
		{
			return null;
		}

		ModelEntity parentEntity = Application.Model.Registry.GetEntity(parentId);
		if (parentEntity == null)
		{
			return null;
		}

		if (parentEntity.Node != null && GodotObject.IsInstanceValid(parentEntity.Node))
		{
			return parentEntity.Node;
		}

		return EnsureNode(parentEntity);
	}

	#endregion
}