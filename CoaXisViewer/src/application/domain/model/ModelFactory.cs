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
    /// 見た目のロードを順番に処理するためのキュー
    /// GLB/WRL の読み込みは比較的重いため、ここで一件ずつ処理して UI と描画が詰まりにくくする
    /// </summary>
    private readonly Queue<ModelData> _visualAssetLoadQueue = new();
    private readonly object _visualAssetLoadQueueLock = new();
    private bool _isVisualAssetLoadQueueRunning;

    /// <summary>
    /// 見た目が揃ったモデルを集めて、後続のコライダー生成を順番に処理するキュー
    /// コライダー生成は視覚ロード後にまとめて行うことで、重い処理を分散させる
    /// </summary>
    private readonly Queue<ModelData> _colliderBuildQueue = new();
    private readonly object _colliderBuildQueueLock = new();
    private bool _isColliderBuildQueueRunning;

    /// <summary>
    /// Clear や再読み込み時に、既にキューに残っている非同期処理が旧データを更新しないように待機キューを停止する
    /// 旧モデルのロードが残ると、レジストリやツリーにゴミが残るため、ここで明示的に無効化する
    /// </summary>
    public void ClearPendingLoads()
    {
        lock (_visualAssetLoadQueueLock)
        {
            _visualAssetLoadQueue.Clear();
            _isVisualAssetLoadQueueRunning = false;
        }

        lock (_colliderBuildQueueLock)
        {
            _colliderBuildQueue.Clear();
            _isColliderBuildQueueRunning = false;
        }
    }

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

        UpdateModelStatus(modelData, ModelStatus.Initialized);

        Application.Model.Registry.Register(modelData);

        ModelNode node = EnsureNode(modelData);
        modelData.Node = node;

        Application.Model.Registry.ResolveHierarchy();
        bool hasGlb = !string.IsNullOrWhiteSpace(modelData.GlbPath);
        bool hasWrl = !string.IsNullOrWhiteSpace(modelData.WrlPath);
        if (!hasGlb && !hasWrl)
        {
            UpdateModelStatus(modelData, ModelStatus.Loaded);
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

        // 既存ノードが有効なら再利用し、破棄済みのノードや未生成のノードだけを作る
        // これにより CSV 再読み込み時にもノードが重複生成されにくくなる

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

        if (!Application.Model.Registry.IsRegistered(modelData.Id))
        {
            return;
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
            // 見た目のロード処理を順番に行う
            // 1件ずつ ProcessFrame を挟むことで、Godot の描画スレッドに負荷を抑える
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

                if (!IsActiveModel(nextModelData))
                {
                    continue;
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
            lock (_visualAssetLoadQueueLock)
            {
                _isVisualAssetLoadQueueRunning = false;
            }
        }
    }

    /// <summary>
    /// モデルの見た目に関わるアセットを読み込む
    /// </summary>
    private async Task LoadVisualAssetsAsync(ModelData modelData)
    {
        try
        {
            // クリア中や古いロードの残骸はスキップする
            // ここで先に弾かないと、レジストリから外れたモデルが後続処理で再利用される
            if (!IsActiveModel(modelData))
            {
                return;
            }

            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

            if (!IsActiveModel(modelData))
            {
                return;
            }

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
                UpdateModelStatus(modelData, ModelStatus.Loading);
                glbLoaded = await GlbMeshLoader.LoadModelAsync(modelNode, modelData.GlbPath);
                if (glbLoaded)
                {
                    hasLoadedVisual = true;
                }
            }

            if (!string.IsNullOrWhiteSpace(modelData.WrlPath))
            {
                UpdateModelStatus(modelData, ModelStatus.Loading);
                wrlLoaded = WrlLineParser.LoadLines(modelNode, modelData.WrlPath);
                if (wrlLoaded)
                {
                    hasLoadedVisual = true;
                }
            }

            if (hasLoadedVisual)
            {
                // メッシュロード完了後はまだコライダー生成前のため、Loading のまま維持する
                QueueColliderBuild(modelData);
                return;
            }

            UpdateModelStatus(modelData, glbLoaded && wrlLoaded ? ModelStatus.Loaded : ModelStatus.LoadFailed);
        }
        catch (Exception exception)
        {
            UpdateModelStatus(modelData, ModelStatus.LoadFailed);
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

        // メッシュロードが完了したモデルから順次コライダー生成を開始する。
        // 全件の見た目ロード完了を待つと、最初に終わったモデルが長く灰色のまま残るため、
        // ここで即時にキューへ積んで、別スレッドの処理が待っている状態にする。
        if (!Application.Model.Registry.IsRegistered(modelData.Id))
        {
            return;
        }

        lock (_colliderBuildQueueLock)
        {
            _colliderBuildQueue.Enqueue(modelData);
        }

        StartColliderBuildQueueIfNeeded();
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

                if (!IsActiveModel(nextModelData))
                {
                    continue;
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
    private async Task BuildColliderAsync(ModelData modelData)
    {
        if (modelData == null)
        {
            throw new ArgumentNullException(nameof(modelData));
        }

        if (!IsActiveModel(modelData))
        {
            return;
        }

        ModelNode modelNode = modelData.Node;
        if (modelNode == null || !IsInstanceValid(modelNode))
        {
            return;
        }

        ModelComponents components = modelNode.Components;
        if (components == null || (!components.HasMesh && !components.HasLine))
        {
            UpdateModelStatus(modelData, ModelStatus.Loaded);
            return;
        }

        // Godot の Node / Mesh / CollisionShape はメインスレッド専用。
        // ワーカースレッドから GetChildren() や Mesh.GetFaces() を呼ぶと thread-affinity で落ちるため、
        // ここでは ProcessFrame 後にメインスレッドでコライダーを生成する。
        UpdateModelStatus(modelData, ModelStatus.Loading);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        ModelColliderBuilder.AddCollider(modelNode);
        UpdateModelStatus(modelData, ModelStatus.Loaded);
    }

    private static bool IsActiveModel(ModelData modelData)
    {
        if (modelData == null)
        {
            return false;
        }

        if (modelData.Status == ModelStatus.Disposed)
        {
            return false;
        }

        return Application.Model.Registry.IsRegistered(modelData.Id)
            && modelData.Node != null
            && IsInstanceValid(modelData.Node);
    }

    private static void UpdateModelStatus(ModelData modelData, ModelStatus nextStatus)
    {
        if (modelData == null)
        {
            return;
        }

        if (modelData.Status == ModelStatus.Disposed && nextStatus != ModelStatus.Disposed)
        {
            return;
        }

        modelData.Status = nextStatus;
        Application.Model.Event.NotifyModelStatusChanged(modelData.Id, nextStatus);
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
