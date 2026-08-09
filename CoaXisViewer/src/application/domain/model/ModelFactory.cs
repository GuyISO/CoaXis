using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// ModelDto から ModelData と ModelNode を生成するファクトリ
/// </summary>
public partial class ModelFactory : Node
{
    /// <summary>
    /// 先に見た目だけを順次読み込むための待ち行列
    /// </summary>
    private readonly Queue<ModelData> _visualAssetLoadQueue = new();
    private readonly object _visualAssetLoadQueueLock = new();
    private bool _isVisualAssetLoadQueueRunning;

    /// <summary>
    /// 表示完了後にまとめてコライダーを作るための待ち行列
    /// </summary>
    private readonly Queue<ModelData> _colliderBuildQueue = new();
    private readonly object _colliderBuildQueueLock = new();
    private bool _isColliderBuildQueueRunning;

    #region Public API

    /// <summary>
    /// ModelDto から ModelData を生成し、Registry に登録してノードを生成する。
    /// その後の見た目ロードとコライダー生成は別キューで順番に処理する。
    /// </summary>
    /// <param name="dto">生成元となる DTO</param>
    /// <param name="parentId">親モデルの ID。null または Guid.Empty の場合は Root 配下に追加する</param>
    /// <returns>生成された ModelData</returns>
    public ModelData CreateFromDto(ModelDto dto, Guid? parentId = null)
    {
        if (dto == null)
        {
            throw new ArgumentNullException(nameof(dto));
        }

        if (dto.Id == Guid.Empty)
        {
            throw new ArgumentException("ModelDto.Id must not be empty.", nameof(dto));
        }

        Guid resolvedParentId = parentId ?? dto.ParentId ?? Guid.Empty;
        Vector3 convertedPosition = ConvertPosition(dto.Position);
        Quaternion convertedRotation = ConvertRotation(dto.Rotation);

        var modelData = new ModelData(
            dto.Id,
            resolvedParentId,
            dto.Type,
            dto.Name,
            convertedPosition,
            convertedRotation,
            dto.IconFilePath,
            dto.GlbFilePath,
            dto.WrlFilePath);

        modelData.Status = ModelStatus.Initialized;

        Application.Model.Registry.Register(modelData);

        ModelNode node = EnsureNode(modelData);
        modelData.Node = node;

        Application.Model.Registry.ResolveHierarchy();
        bool hasGlb = !string.IsNullOrWhiteSpace(modelData.GlbPath);
        bool hasWrl = !string.IsNullOrWhiteSpace(modelData.WrlPath);
        if (!hasGlb && !hasWrl)
        {
            modelData.Status = ModelStatus.Loaded;
        }
        else
        {
            QueueVisualAssetLoad(modelData);
        }

        Application.Model.Event.NotifyModelAdded(modelData.Id, modelData.ParentId);

        return modelData;
    }

    #endregion

    #region Internal Helpers

    private ModelNode EnsureNode(ModelData modelData)
    {
        if (modelData == null)
        {
            throw new ArgumentNullException(nameof(modelData));
        }

        if (modelData.Node != null && IsInstanceValid(modelData.Node))
        {
            return modelData.Node;
        }

        var node = new ModelNode(modelData.Id);
        modelData.Node = node;

        node.Position = modelData.Position;
        node.Quaternion = modelData.Rotation;

        ModelNode parentNode = ResolveParentNode(modelData.ParentId);
        if (parentNode != null)
        {
            parentNode.AddChild(node);
        }
        else
        {
            var rootNode = Application.Model.Service.Root?.Node;
            if (rootNode != null)
            {
                rootNode.AddChild(node);
            }
            else
            {
                Application.Log.Warn($"ModelFactory: parent node not found for modelId='{modelData.Id}'.");
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

        ModelData parentData = Application.Model.Registry.GetModelData(parentId);
        if (parentData == null)
        {
            return null;
        }

        if (parentData.Node != null && IsInstanceValid(parentData.Node))
        {
            return parentData.Node;
        }

        return EnsureNode(parentData);
    }

    private void QueueVisualAssetLoad(ModelData modelData)
    {
        if (modelData == null)
        {
            throw new ArgumentNullException(nameof(modelData));
        }

        lock (_visualAssetLoadQueueLock)
        {
            _visualAssetLoadQueue.Enqueue(modelData);
            if (_isVisualAssetLoadQueueRunning)
            {
                return;
            }

            _isVisualAssetLoadQueueRunning = true;
        }

        _ = ProcessVisualAssetQueueAsync();
    }

    private async Task ProcessVisualAssetQueueAsync()
    {
        try
        {
            while (true)
            {
                ModelData nextModelData;
                lock (_visualAssetLoadQueueLock)
                {
                    if (_visualAssetLoadQueue.Count == 0)
                    {
                        _isVisualAssetLoadQueueRunning = false;
                        return;
                    }

                    nextModelData = _visualAssetLoadQueue.Dequeue();
                }

                await LoadVisualAssetsAsync(nextModelData);
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }
        }
        catch (Exception exception)
        {
            Application.Log.Error($"ModelFactory: visual asset queue processing failed. {exception}");
        }
        finally
        {
            // 可視アセットの読み込みが完全に終わった段階でのみコライダー生成を開始する
            lock (_visualAssetLoadQueueLock)
            {
                _isVisualAssetLoadQueueRunning = false;
            }

            StartColliderBuildQueueIfNeeded();
        }
    }

    /// <summary>
    /// モデルの見た目に関わるアセットを読み込む
    /// </summary>
    private async Task LoadVisualAssetsAsync(ModelData modelData)
    {
        try
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

            ModelNode modelNode = modelData.Node;
            if (modelNode == null || !IsInstanceValid(modelNode))
            {
                return;
            }

            bool glbLoaded = true;
            bool wrlLoaded = true;
            bool hasLoadedVisual = false;

            if (!string.IsNullOrWhiteSpace(modelData.GlbPath))
            {
                modelData.Status = ModelStatus.GlbLoading;
                glbLoaded = await GlbMeshLoader.LoadModelAsync(modelNode, modelData.GlbPath);
                if (glbLoaded)
                {
                    modelData.Status = ModelStatus.GlbLoaded;
                    hasLoadedVisual = true;
                }
            }

            if (!string.IsNullOrWhiteSpace(modelData.WrlPath))
            {
                modelData.Status = ModelStatus.WrlLoading;
                wrlLoaded = WrlLineParser.LoadLines(modelNode, modelData.WrlPath);
                if (wrlLoaded)
                {
                    modelData.Status = ModelStatus.WrlLoaded;
                    hasLoadedVisual = true;
                }
            }

            if (hasLoadedVisual)
            {
                // 見た目が揃ったモデルだけを後続のコライダー生成対象に載せる
                QueueColliderBuild(modelData);
            }

            modelData.Status = glbLoaded && wrlLoaded ? ModelStatus.Loaded : ModelStatus.LoadFailed;
        }
        catch (Exception exception)
        {
            modelData.Status = ModelStatus.LoadFailed;
            Application.Log.Error($"ModelFactory: failed to load model assets for modelId='{modelData.Id}', glb='{modelData.GlbPath}', wrl='{modelData.WrlPath}'. {exception}");
        }
    }

    /// <summary>
    /// コライダー生成対象を後続キューに積む
    /// </summary>
    private void QueueColliderBuild(ModelData modelData)
    {
        if (modelData == null)
        {
            throw new ArgumentNullException(nameof(modelData));
        }

        lock (_colliderBuildQueueLock)
        {
            _colliderBuildQueue.Enqueue(modelData);
        }
    }

    /// <summary>
    /// 可視アセットの読み込みが終わっている場合にだけコライダー処理を開始する
    /// </summary>
    private void StartColliderBuildQueueIfNeeded()
    {
        lock (_colliderBuildQueueLock)
        {
            if (_isColliderBuildQueueRunning || _colliderBuildQueue.Count == 0)
            {
                return;
            }

            _isColliderBuildQueueRunning = true;
        }

        _ = ProcessColliderBuildQueueAsync();
    }

    /// <summary>
    /// コライダーを順番に生成する
    /// </summary>
    private async Task ProcessColliderBuildQueueAsync()
    {
        try
        {
            while (true)
            {
                ModelData nextModelData;
                lock (_colliderBuildQueueLock)
                {
                    if (_colliderBuildQueue.Count == 0)
                    {
                        _isColliderBuildQueueRunning = false;
                        return;
                    }

                    nextModelData = _colliderBuildQueue.Dequeue();
                }

                await BuildColliderAsync(nextModelData);
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }
        }
        catch (Exception exception)
        {
            Application.Log.Error($"ModelFactory: collider queue processing failed. {exception}");
        }
        finally
        {
            lock (_colliderBuildQueueLock)
            {
                _isColliderBuildQueueRunning = false;
            }
        }
    }

    /// <summary>
    /// 1件分のコライダーを生成する
    /// </summary>
    private static Task BuildColliderAsync(ModelData modelData)
    {
        if (modelData == null)
        {
            throw new ArgumentNullException(nameof(modelData));
        }

        ModelNode modelNode = modelData.Node;
        if (modelNode == null || !IsInstanceValid(modelNode))
        {
            return Task.CompletedTask;
        }

        modelData.Status = ModelStatus.ColliderCreating;
        ModelColliderBuilder.AddCollider(modelNode);
        modelData.Status = ModelStatus.ColliderCreated;
        return Task.CompletedTask;
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
