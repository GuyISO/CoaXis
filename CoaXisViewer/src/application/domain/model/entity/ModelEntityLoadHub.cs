using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using CoaXis.Protocol.Viewer;

/// <summary>
/// 検証済みModelEntityの登録、置換、削除を管理するハブ。
/// </summary>
public partial class ModelEntityLoadHub : BaseHub
{
	#region Fields

	#endregion

	#region Properties

	#endregion

	#region Signals

	#endregion

	#region Lifecycle

	#endregion

	#region Events

	#endregion

	#region Methods

	/// <summary>
	/// 現在のEntityをクリアする。
	/// </summary>
	public void Clear()
	{
		// 旧世代を先に無効化し、古いノードのロード完了による状態更新を防ぐ。
		Application.Model.Entity.Scene.CancelPendingLoads();
		Application.Model.Entity.Registry.Clear();
	}

	/// <summary>
	/// 検証済みEntityを追加し、同じ識別子がある場合はEntityを置換する。
	/// </summary>
	/// <param name="entities">追加または置換するEntity</param>
	/// <exception cref="ArgumentNullException">entitiesがnullの場合</exception>
	/// <exception cref="ArgumentException">Root Entityが含まれる場合</exception>
	internal void AddOrReplacePrepared(IReadOnlyList<ModelEntity> entities)
	{
		if (entities == null)
		{
			throw new ArgumentNullException(nameof(entities));
		}

		var entityIds = new HashSet<Guid>();
		foreach (ModelEntity entity in entities)
		{
			if (entity == null)
			{
				throw new ArgumentException("ModelEntity must not be null.", nameof(entities));
			}

			if (entity.Id == ViewerConstants.RootModelEntityId)
			{
				throw new ArgumentException("The root ModelEntity cannot be added or replaced.", nameof(entities));
			}

			if (!entityIds.Add(entity.Id))
			{
				throw new ArgumentException($"Duplicate ModelEntity id '{entity.Id}'.", nameof(entities));
			}
		}

		Application.Model.Entity.Scene.CancelLoads(entityIds);
		foreach (Guid entityId in entityIds)
		{
			ModelEntity existingEntity = Application.Model.Entity.Registry.Get(entityId);
			if (existingEntity == null)
			{
				continue;
			}

			DetachChildNodes(existingEntity);
			if (Application.Model.Entity.Selection.Contains(entityId))
			{
				Application.Model.Entity.Selection.Remove(entityId);
			}

			if (existingEntity.Node != null && IsInstanceValid(existingEntity.Node))
			{
				existingEntity.Node.QueueFree();
			}

			Application.Model.Entity.Registry.Dispose(entityId);
		}

		foreach (ModelEntity entity in entities)
		{
			entity.InitializeForRegistration();
			Application.Model.Entity.Registry.Register(entity);
		}

		Application.Model.Entity.Registry.ResolveHierarchy();
		List<ModelEntity> registeredEntities = Application.Model.Entity.Registry.Items.Values.ToList();
		List<ModelEntity> rootConnectedEntities = registeredEntities
			.Where(entity =>
				entity.Id != ViewerConstants.RootModelEntityId &&
				Application.Model.Entity.Registry.IsRootConnected(entity.Id))
			.ToList();
		List<ModelEntity> disconnectedEntities = registeredEntities
			.Where(entity =>
				entity.Id != ViewerConstants.RootModelEntityId &&
				!Application.Model.Entity.Registry.IsRootConnected(entity.Id))
			.ToList();
		RemoveNodesFromDisconnectedEntities(disconnectedEntities);

		var entitiesToLoad = new List<ModelEntity>();
		foreach (ModelEntity entity in rootConnectedEntities.OrderBy(entity => GetDepth(entity.Id)))
		{
			bool needsLoad = entity.Node == null || !GodotObject.IsInstanceValid(entity.Node);
			EnsureNode(entity);
			if (needsLoad)
			{
				entitiesToLoad.Add(entity);
			}
		}

		ReparentRegisteredNodes(rootConnectedEntities);
		Application.Model.Entity.Scene.PrepareLoads(entitiesToLoad);
		Application.Model.Entity.Scene.StartPendingLoads();
	}

	/// <summary>
	/// 事前に展開されたEntity識別子を削除する。
	/// </summary>
	/// <param name="entityIds">削除するEntity識別子</param>
	internal void DeletePrepared(IReadOnlyCollection<Guid> entityIds)
	{
		if (entityIds == null)
		{
			throw new ArgumentNullException(nameof(entityIds));
		}

		Application.Model.Entity.Scene.CancelLoads(entityIds);
		Application.Model.Entity.Selection.Remove(new List<Guid>(entityIds).ToArray());

		foreach (ModelEntity entity in Application.Model.Entity.Registry.Items.Values)
		{
			if (entityIds.Contains(entity.Id) &&
				!entityIds.Contains(entity.ParentId) &&
				entity.Node != null &&
				IsInstanceValid(entity.Node))
			{
				entity.Node.QueueFree();
			}
		}

		foreach (Guid entityId in entityIds.OrderByDescending(GetDepth))
		{
			Application.Model.Entity.Registry.Dispose(entityId);
		}

		Application.Model.Entity.Registry.ResolveHierarchy();
	}

	#endregion

	#region Helpers

	/// <summary>
	/// 置換されるEntity配下の子Entity Nodeを親Nodeから切り離し、置換後も再利用できるようにする。
	/// </summary>
	private static void DetachChildNodes(ModelEntity entity)
	{
		ModelNode parentNode = entity.Node;
		if (parentNode == null || !IsInstanceValid(parentNode))
		{
			return;
		}

		foreach (ModelEntity child in Application.Model.Entity.Registry.Items.Values)
		{
			if (child.ParentId == entity.Id &&
				child.Node != null &&
				IsInstanceValid(child.Node) &&
				child.Node.GetParent() == parentNode)
			{
				parentNode.RemoveChild(child.Node);
			}
		}
	}

	/// <summary>
	/// 登録済みEntity Nodeを現在のEntity階層に合わせて接続する。
	/// </summary>
	private static void ReparentRegisteredNodes(IReadOnlyCollection<ModelEntity> entities)
	{
		foreach (ModelEntity entity in entities)
		{
			ModelNode node = entity.Node;
			if (node == null || !IsInstanceValid(node))
			{
				continue;
			}

			ModelNode parentNode = ResolveParentNode(entity.ParentId);
			if (parentNode == null || node.GetParent() == parentNode)
			{
				continue;
			}

			node.GetParent()?.RemoveChild(node);
			parentNode.AddChild(node);
			node.Position = entity.Position;
			node.Quaternion = entity.Rotation;
		}
	}

	/// <summary>
	/// 指定Entityの親階層の深さを取得する。
	/// </summary>
	private static int GetDepth(Guid entityId)
	{
		int depth = 0;
		ModelEntity entity = Application.Model.Entity.Registry.Get(entityId);
		var visited = new HashSet<Guid>();
		while (entity != null && entity.ParentId != Guid.Empty && visited.Add(entity.Id))
		{
			depth++;
			entity = Application.Model.Entity.Registry.Get(entity.ParentId);
		}

		return depth;
	}

	/// <summary>
	/// Entityに対応するModelNodeが無ければ生成して返す。
	/// </summary>
	private static ModelNode EnsureNode(ModelEntity modelEntity)
	{
		if (modelEntity == null)
		{
			throw new ArgumentNullException(nameof(modelEntity));
		}

		if (!Application.Model.Entity.Registry.IsRootConnected(modelEntity.Id))
		{
			return null;
		}

		// 既存ノードが有効なら再利用し、再読込時の重複生成を防ぐ。
		if (modelEntity.Node != null && GodotObject.IsInstanceValid(modelEntity.Node))
		{
			return modelEntity.Node;
		}

		ModelNode parentNode = ResolveParentNode(modelEntity.ParentId);
		if (parentNode == null)
		{
			throw new InvalidOperationException(
				$"Root-connected ModelEntity '{modelEntity.Id}' has no resolvable parent node.");
		}

		var node = new ModelNode(modelEntity.Id);
		modelEntity.Node = node;
		node.Name = modelEntity.Id.ToString();
		parentNode.AddChild(node);
		return node;
	}

	/// <summary>
	/// 親Entityの識別子から親ModelNodeを解決する。
	/// </summary>
	private static ModelNode ResolveParentNode(Guid parentId)
	{
		if (parentId == Guid.Empty)
		{
			return Application.Model.Entity.Registry.Root?.Node;
		}

		ModelEntity parentEntity = Application.Model.Entity.Registry.Get(parentId);
		if (parentEntity == null)
		{
			return null;
		}

		return parentEntity.Node != null && GodotObject.IsInstanceValid(parentEntity.Node)
			? parentEntity.Node
			: null;
	}

	/// <summary>
	/// Root未接続EntityのNodeとSceneロード状態を破棄し、後からRootへ接続された場合に再生成可能にする。
	/// </summary>
	private static void RemoveNodesFromDisconnectedEntities(IReadOnlyCollection<ModelEntity> entities)
	{
		if (entities.Count == 0)
		{
			return;
		}

		Application.Model.Entity.Scene.CancelLoads(entities.Select(entity => entity.Id).ToList());
		var disconnectedNodes = new HashSet<ModelNode>(
			entities
				.Select(entity => entity.Node)
				.Where(node => node != null && IsInstanceValid(node)));

		foreach (ModelEntity entity in entities)
		{
			ModelNode node = entity.Node;
			if (node != null &&
				IsInstanceValid(node) &&
				!disconnectedNodes.Contains(node.GetParent() as ModelNode))
			{
				node.GetParent()?.RemoveChild(node);
				node.QueueFree();
			}

			entity.Node = null;
			if (entity.Status != ModelStatus.Registered)
			{
				entity.ResetLoadState();
			}
		}
	}

	#endregion
}