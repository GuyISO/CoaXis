using CoaXis.Protocol.Viewer;
using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// ModelEntityDto から ModelEntity と ModelNode を生成し、シーン読み込みキューを制御するファクトリ
/// </summary>
public partial class ModelEntityFactory : Node
{
    #region Public API

    /// <summary>
    /// ModelEntityDto の集合から ModelEntity を一括生成し、Registry と SceneTree に反映する。
    /// その後のシーンロード(ScenePath)は別キューで順番に処理する。
    /// </summary>
    /// <param name="entityDtos">生成元となる DTO の集合</param>
    /// <returns>生成された ModelEntity の一覧</returns>
    public IReadOnlyList<ModelEntity> CreateEntities(IReadOnlyList<ModelEntityDto> entityDtos)
    {
        if (entityDtos == null)
        {
            throw new ArgumentNullException(nameof(entityDtos));
        }

        var entities = new List<ModelEntity>(entityDtos.Count);
        var entityIds = new HashSet<Guid>();
        foreach (ModelEntityDto dto in entityDtos)
        {
            if (dto == null)
            {
                throw new ArgumentException("ModelEntityDto must not be null.", nameof(entityDtos));
            }

            if (dto.Id == Guid.Empty)
            {
                throw new ArgumentException("ModelEntityDto.Id must not be empty.", nameof(entityDtos));
            }

            if (!entityIds.Add(dto.Id))
            {
                throw new ArgumentException($"Duplicate ModelEntityDto.Id '{dto.Id}'.", nameof(entityDtos));
            }

            entities.Add(ModelEntityMapper.Map(dto));
        }

        // 親子関係の通知順序を親優先に並べ替える
        IReadOnlyList<ModelEntity> notificationOrder = OrderParentFirst(entities);

        // 全件を登録してから階層を解決することで、入力順に依存せず親子関係を確定する。
        foreach (ModelEntity modelEntity in entities)
        {
            Application.Model.SceneLoader.MarkInitialized(modelEntity);
            Application.Model.Registry.RegisterEntity(modelEntity);
        }
        Application.Model.Registry.ResolveHierarchy();

        foreach (ModelEntity modelEntity in entities)
        {
            modelEntity.Node = EnsureNode(modelEntity);
        }

        Application.Model.SceneLoader.PrepareLoads(entities);

        // TreeItem は親の通知時点で親が存在する必要があるため、通知だけ親先行にする。
        foreach (ModelEntity modelEntity in notificationOrder)
        {
            Application.Model.Event.NotifyVisibility(modelEntity.Id, modelEntity.Visibility);
            Application.Model.Event.NotifyAdded(modelEntity.Id, modelEntity.ParentId);
        }

        Application.Model.SceneLoader.StartPendingLoads();
        return entities;
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

        // 循環した入力でも通知処理を停止させず、循環の起点から順に処理を継続する。
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

    private ModelNode EnsureNode(ModelEntity modelEntity)
    {
        if (modelEntity == null)
        {
            throw new ArgumentNullException(nameof(modelEntity));
        }

        // 既存ノードが有効なら再利用し、破棄済みのノードや未生成のノードだけを作る
        // これにより CSV 再読み込み時にもノードが重複生成されにくくなる

        if (modelEntity.Node != null && IsInstanceValid(modelEntity.Node))
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
            var rootNode = Application.Model.Registry.RootEntity?.Node;
            if (rootNode != null)
            {
                rootNode.AddChild(node);
            }
            else
            {
                Application.Log.Warn($"ModelEntityFactory: parent node not found for entityId='{modelEntity.Id}'.");
            }
        }

        return node;
    }

    private ModelNode ResolveParentNode(Guid parentId)
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

        if (parentEntity.Node != null && IsInstanceValid(parentEntity.Node))
        {
            return parentEntity.Node;
        }

        return EnsureNode(parentEntity);
    }

    #endregion
}
