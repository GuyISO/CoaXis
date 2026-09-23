using CoaXis.Protocol.Viewer;
using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

/// <summary>
/// ModelEntityDto から ModelEntity と ModelNode を生成し、シーン読み込みキューを制御するファクトリ
/// </summary>
public partial class ModelEntityFactory : Node
{
    /// <summary>
    /// シーンロードを順番に処理するためのキュー
    /// シーン(scn/tscn) の読み込みは比較的重いため、ここで一件ずつ処理して UI と描画が詰まりにくくする
    /// </summary>
    private readonly Queue<SceneLoadQueueItem> _sceneLoadQueue = new();
    private readonly object _sceneLoadQueueLock = new();
    private bool _isSceneLoadQueueRunning;
    private long _sceneLoadGeneration;

    /// <summary>
    /// 1フレームあたりでキュー処理に割り当てる時間予算(ms)。超過分は次フレームへ回して描画を詰まらせない
    /// </summary>
    private const long SceneLoadFrameBudgetMs = 4;

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

            entities.Add(CreateModelEntity(dto));
        }

        // 親子関係の通知順序を親優先に並べ替える
        IReadOnlyList<ModelEntity> notificationOrder = OrderParentFirst(entities);

        // 全件を登録してから階層を解決することで、入力順に依存せず親子関係を確定する。
        foreach (ModelEntity modelEntity in entities)
        {
            UpdateModelStatus(modelEntity, ModelStatus.Initialized);
            Application.Model.Registry.RegisterEntity(modelEntity);
        }
        Application.Model.Registry.ResolveHierarchy();

        foreach (ModelEntity modelEntity in entities)
        {
            modelEntity.Node = EnsureNode(modelEntity);
        }

        foreach (ModelEntity modelEntity in entities)
        {
            if (!string.IsNullOrWhiteSpace(modelEntity.ScenePath))
            {
                QueueSceneLoad(modelEntity, false);
            }
            else
            {
                UpdateModelStatus(modelEntity, ModelStatus.Loaded);
            }
        }

        // TreeItem は親の通知時点で親が存在する必要があるため、通知だけ親先行にする。
        foreach (ModelEntity modelEntity in notificationOrder)
        {
            Application.Model.Event.NotifyVisibility(modelEntity.Id, modelEntity.Visibility);
            Application.Model.Event.NotifyAdded(modelEntity.Id, modelEntity.ParentId);
        }

        StartSceneLoadQueue();
        return entities;
    }

    /// <summary>
    /// Clear や再読み込み時に、既にキューに残っている非同期処理が旧データを更新しないように待機キューを停止する
    /// 旧モデルのロードが残ると、レジストリやツリーにゴミが残るため、ここで明示的に無効化する
    /// </summary>
    public void ClearPendingLoads()
    {
        lock (_sceneLoadQueueLock)
        {
            _sceneLoadQueue.Clear();
            _isSceneLoadQueueRunning = false;
            _sceneLoadGeneration++;
        }

        SceneAssetLoader.ClearCompletedCache();
    }

    #endregion

    #region Internal Helpers

    private static ModelEntity CreateModelEntity(ModelEntityDto dto)
    {
        Guid resolvedParentId = dto.ParentId ?? Guid.Empty;
        Vector3 convertedPosition = ConvertPosition(dto.Position);
        Quaternion convertedRotation = ConvertRotation(dto.Rotation);

        return new ModelEntity(
            dto.Id,
            resolvedParentId,
            dto.Type,
            dto.Name,
            convertedPosition,
            convertedRotation,
            ModelVisibilityResolver.Parse(dto.Visibility),
            dto.IsCollapsed,
            dto.IconPath,
            dto.ScenePath,
            dto.AlignToAabbCenter);
    }

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

    private void QueueSceneLoad(ModelEntity modelEntity, bool startProcessing = true)
    {
        if (modelEntity == null)
        {
            throw new ArgumentNullException(nameof(modelEntity));
        }

        if (!Application.Model.Registry.IsEntityRegistered(modelEntity.Id))
        {
            return;
        }

        // 実ファイルI/O・パースはここで即バックグラウンドへ投入し、他モデルの完了待ちで遅延させない
        SceneAssetLoader.RequestLoad(modelEntity.ScenePath);

        lock (_sceneLoadQueueLock)
        {
            long generation = _sceneLoadGeneration;
            _sceneLoadQueue.Enqueue(new SceneLoadQueueItem(modelEntity, generation));
            if (startProcessing)
            {
                StartSceneLoadQueue();
            }
        }
    }

    private void StartSceneLoadQueue()
    {
        lock (_sceneLoadQueueLock)
        {
            if (_isSceneLoadQueueRunning || _sceneLoadQueue.Count == 0)
            {
                return;
            }

            _isSceneLoadQueueRunning = true;
            _ = ProcessSceneLoadQueueAsync(_sceneLoadGeneration);
        }
    }

    private async Task ProcessSceneLoadQueueAsync(long generation)
    {
        try
        {
            // フレーム予算を使い切った時だけ ProcessFrame を挟む（未完了分は末尾に戻して次回ポーリングする）
            var stopwatch = Stopwatch.StartNew();
            while (true)
            {
                SceneLoadQueueItem nextItem;
                lock (_sceneLoadQueueLock)
                {
                    if (generation != _sceneLoadGeneration)
                    {
                        return;
                    }

                    if (_sceneLoadQueue.Count == 0)
                    {
                        _isSceneLoadQueueRunning = false;
                        return;
                    }

                    nextItem = _sceneLoadQueue.Dequeue();
                }

                if (nextItem.Generation != generation)
                {
                    continue;
                }

                if (IsActiveEntity(nextItem.ModelEntity) && !TryFinishSceneLoad(nextItem.ModelEntity))
                {
                    lock (_sceneLoadQueueLock)
                    {
                        if (generation == _sceneLoadGeneration)
                        {
                            _sceneLoadQueue.Enqueue(nextItem);
                        }
                    }
                }

                if (stopwatch.ElapsedMilliseconds >= SceneLoadFrameBudgetMs)
                {
                    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                    stopwatch.Restart();
                }
            }
        }
        catch (Exception exception)
        {
            Application.Log.Error($"ModelEntityFactory: scene load queue processing failed. {exception}");
        }
        finally
        {
            lock (_sceneLoadQueueLock)
            {
                // 旧世代ランナーが新世代ランナーの実行状態を上書きしないようにする
                if (generation == _sceneLoadGeneration)
                {
                    _isSceneLoadQueueRunning = false;
                }
            }
        }
    }

    /// <summary>
    /// SceneBuilderで作成されたシーン(ScenePath)のバックグラウンド読み込み完了を確認し、完了していれば反映する
    /// </summary>
    /// <returns>読み込みが完了（成功/失敗いずれか）した場合は true、まだ進行中の場合は false</returns>
    private bool TryFinishSceneLoad(ModelEntity modelEntity)
    {
        try
        {
            // クリア中や古いロードの残骸はスキップする
            // ここで先に弾かないと、レジストリから外れたモデルが後続処理で再利用される
            if (!IsActiveEntity(modelEntity))
            {
                return true;
            }

            ModelNode modelNode = modelEntity.Node;
            if (modelNode == null || !IsInstanceValid(modelNode))
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(modelEntity.ScenePath))
            {
                UpdateModelStatus(modelEntity, ModelStatus.Loaded);
                return true;
            }

            if (modelEntity.Status != ModelStatus.Loading)
            {
                UpdateModelStatus(modelEntity, ModelStatus.Loading);
            }

            // tscn(ScenePath) はシーン自身が StaticBody3D/CollisionShape3D を持つ想定のため、自動コライダー生成は行わない
            SceneLoadResult result = SceneAssetLoader.TryFinishLoad(modelNode, modelEntity.ScenePath);
            if (result == SceneLoadResult.InProgress)
            {
                return false;
            }

            if (!IsActiveEntity(modelEntity))
            {
                return true;
            }

            bool sceneLoaded = result == SceneLoadResult.Loaded;
            if (sceneLoaded && modelEntity.AlignToAabbCenter)
            {
                ModelMeshCenterAligner.AlignPivotToMeshCenter(modelNode);
            }

            modelNode.ApplyVisibilityLayer(ModelVisibilityResolver.IsVisible(modelEntity));
            UpdateModelStatus(modelEntity, sceneLoaded ? ModelStatus.Loaded : ModelStatus.LoadFailed);
            return true;
        }
        catch (Exception exception)
        {
            UpdateModelStatus(modelEntity, ModelStatus.LoadFailed);
            Application.Log.Error($"ModelEntityFactory: failed to load model assets for entityId='{modelEntity.Id}', scene='{modelEntity.ScenePath}'. {exception}");
            return true;
        }
    }

    private sealed class SceneLoadQueueItem
    {
        public SceneLoadQueueItem(ModelEntity modelEntity, long generation)
        {
            ModelEntity = modelEntity;
            Generation = generation;
        }

        public ModelEntity ModelEntity { get; }

        public long Generation { get; }
    }

    private static bool IsActiveEntity(ModelEntity modelEntity)
    {
        if (modelEntity == null)
        {
            return false;
        }

        if (modelEntity.Status == ModelStatus.Disposed)
        {
            return false;
        }

        return Application.Model.Registry.IsEntityRegistered(modelEntity.Id)
            && modelEntity.Node != null
            && IsInstanceValid(modelEntity.Node);
    }

    private static void UpdateModelStatus(ModelEntity modelEntity, ModelStatus nextStatus)
    {
        if (modelEntity == null)
        {
            return;
        }

        if (modelEntity.Status == ModelStatus.Disposed && nextStatus != ModelStatus.Disposed)
        {
            return;
        }

        modelEntity.Status = nextStatus;
        Application.Model.Event.NotifyStatus(modelEntity.Id, nextStatus);
    }

    private static Vector3 ConvertPosition(float[] position)
    {
        if (position == null || position.Length != 3)
        {
            return Vector3.Zero;
        }

        Vector3 catiaVector = new Vector3(position[0], position[1], position[2]);
        return CoordinateSystemUtility.CatiaToGodot(catiaVector);
    }

    private static Quaternion ConvertRotation(float[] rotation)
    {
        if (rotation == null || rotation.Length != 4)
        {
            return Quaternion.Identity;
        }

        Quaternion catiaQuaternion = new Quaternion(rotation[0], rotation[1], rotation[2], rotation[3]);
        Basis catiaBasis = new Basis(catiaQuaternion);
        Basis godotBasis = CoordinateSystemUtility.CatiaToGodot(catiaBasis);
        return godotBasis.GetRotationQuaternion();
    }

    #endregion
}
