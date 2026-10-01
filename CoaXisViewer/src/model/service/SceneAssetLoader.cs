// TODO: リファクタリング確認後に削除
using Godot;
using System;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// シーン読み込みの進行状態
/// </summary>
public enum SceneLoadResult
{
    /// <summary>バックグラウンドでの読み込みがまだ完了していない</summary>
    InProgress,
    /// <summary>読み込みとノード追加が完了した</summary>
    Loaded,
    /// <summary>読み込みに失敗した</summary>
    Failed,
}

/// <summary>
/// PackedScene(.tscn/.scn) の読み込みを担当するヘルパー
/// </summary>
public static class SceneAssetLoader
{
    /// <summary>
    /// Godotのスレッドロード要求と取得済みリソースをパス単位で一元管理する
    /// </summary>
    private static readonly Dictionary<string, SceneResourceLoadState> _loadStates = new();
    private static readonly Queue<string> _pendingLoadPaths = new();
    private static readonly object _loadStatesLock = new();
    private static string _activeLoadPath;

    #region Public Methods

    /// <summary>
    /// 完了済みまたは失敗済みのロード状態を破棄する（AssetHubのres://シーンキャッシュは維持する）
    /// </summary>
    public static void ClearCompletedCache()
    {
        lock (_loadStatesLock)
        {
            // Godot側にキャンセルAPIがないため、未開始要求だけ破棄し進行中要求の追跡は残す。
            _pendingLoadPaths.Clear();
            var completedPaths = new List<string>();
            foreach ((string path, SceneResourceLoadState state) in _loadStates)
            {
                if (state.Status == SceneResourceLoadStatus.Requested)
                {
                    if (!IsResourcePath(path))
                    {
                        state.PendingConsumers = 0;
                    }

                    continue;
                }

                completedPaths.Add(path);
            }

            foreach (string path in completedPaths)
            {
                _loadStates.Remove(path);
            }
        }
    }

    /// <summary>
    /// シーンリソースの読み込みをバックグラウンドスレッドへ投入する（Instantiate/AddChildは行わない）
    /// </summary>
    /// <param name="path">ロードするシーンのパス</param>
    /// <returns>投入済み（または投入に成功した）場合は true</returns>
    public static bool RequestLoad(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            Application.Log.Warn("SceneAssetLoader: empty scene path.");
            return false;
        }

        if (!TryResolveExistingFilePath(path, out string resolvedPath))
        {
            Application.Log.Warn($"SceneAssetLoader: file not found. path='{path}'");
            return false;
        }

        lock (_loadStatesLock)
        {
            bool isResourcePath = IsResourcePath(resolvedPath);
            if (_loadStates.TryGetValue(resolvedPath, out SceneResourceLoadState existingState))
            {
                if (!isResourcePath)
                {
                    existingState.PendingConsumers++;
                }

                return true;
            }

            if (isResourcePath && Application.Asset.Scene.TryGetByPath(resolvedPath) != null)
            {
                _loadStates.Add(resolvedPath, new SceneResourceLoadState
                {
                    Status = SceneResourceLoadStatus.Loaded,
                });
                return true;
            }

            SceneResourceLoadState state = new()
            {
                Status = SceneResourceLoadStatus.Queued,
                PendingConsumers = isResourcePath ? 0 : 1,
            };
            _loadStates.Add(resolvedPath, state);
            _pendingLoadPaths.Enqueue(resolvedPath);

            StartNextLoadLocked();
            return state.Status != SceneResourceLoadStatus.Failed;
        }
    }

    /// <summary>
    /// バックグラウンド読み込みの完了を確認し、完了していればインスタンス化して対象モデルへ追加する
    /// </summary>
    /// <param name="modelNode">シーンを追加する親モデル</param>
    /// <param name="path">ロードするシーンのパス（<see cref="RequestLoad"/> と同一のもの）</param>
    /// <returns>読み込みの進行状態</returns>
    public static SceneLoadResult TryFinishLoad(ModelNode modelNode, string path)
    {
        if (modelNode == null)
        {
            throw new ArgumentNullException(nameof(modelNode));
        }

        if (!TryResolveExistingFilePath(path, out string resolvedPath))
        {
            ReleaseLoad(path);
            return SceneLoadResult.Failed;
        }

        PackedScene packedScene;
        lock (_loadStatesLock)
        {
            if (!_loadStates.TryGetValue(resolvedPath, out SceneResourceLoadState state))
            {
                return SceneLoadResult.Failed;
            }

            // キャンセル後もGodot側の進行中要求は残るため、別パスの確認時にも完了を回収して次へ進める。
            AdvanceActiveLoadLocked();

            if (state.Status == SceneResourceLoadStatus.Failed)
            {
                ReleaseUncachedConsumerLocked(resolvedPath, state);
                return SceneLoadResult.Failed;
            }

            if (state.Status == SceneResourceLoadStatus.Queued
                || state.Status == SceneResourceLoadStatus.Requested)
            {
                return SceneLoadResult.InProgress;
            }

            packedScene = IsResourcePath(resolvedPath)
                ? Application.Asset.Scene.TryGetByPath(resolvedPath)
                : state.PackedScene;
            if (packedScene == null)
            {
                state.Status = SceneResourceLoadStatus.Failed;
                ReleaseUncachedConsumerLocked(resolvedPath, state);
                return SceneLoadResult.Failed;
            }
        }

        ModelComponents components = modelNode.Components;
        if (!IsValidAndNotQueuedForDeletion(modelNode)
            || components == null
            || !IsValidAndNotQueuedForDeletion(components))
        {
            Application.Log.Warn($"SceneAssetLoader: model node is no longer available. path='{path}'");
            ReleaseUncachedConsumer(resolvedPath);
            return SceneLoadResult.Failed;
        }

        Node3D contentContainer = components.EnsureContent();
        if (!IsValidAndNotQueuedForDeletion(contentContainer))
        {
            Application.Log.Warn($"SceneAssetLoader: content container is no longer available. path='{path}'");
            ReleaseUncachedConsumer(resolvedPath);
            return SceneLoadResult.Failed;
        }

        try
        {
            Node instance = packedScene.Instantiate();
            contentContainer.AddChild(instance);

            Application.Log.Info($"SceneAssetLoader: loaded scene '{path}'.");
            return SceneLoadResult.Loaded;
        }
        finally
        {
            ReleaseUncachedConsumer(resolvedPath);
        }
    }

    /// <summary>
    /// 外部シーンを使うEntityがロード前に破棄された場合、その一時利用枠を解放する。
    /// </summary>
    /// <param name="path">シーンのパス</param>
    public static void ReleaseLoad(string path)
    {
        TryResolveExistingFilePath(path, out string resolvedPath);
        ReleaseUncachedConsumer(resolvedPath);
    }

    #endregion

    #region Internal Helpers

    private static void AdvanceActiveLoadLocked()
    {
        if (_activeLoadPath == null
            || !_loadStates.TryGetValue(_activeLoadPath, out SceneResourceLoadState state)
            || state.Status != SceneResourceLoadStatus.Requested)
        {
            return;
        }

        ResourceLoader.ThreadLoadStatus status = ResourceLoader.LoadThreadedGetStatus(_activeLoadPath);
        if (status == ResourceLoader.ThreadLoadStatus.InProgress)
        {
            return;
        }

        string completedPath = _activeLoadPath;
        if (status != ResourceLoader.ThreadLoadStatus.Loaded)
        {
            Application.Log.Error($"SceneAssetLoader: threaded load failed. path='{completedPath}', status='{status}'");
            state.Status = SceneResourceLoadStatus.Failed;
            RemoveUnclaimedExternalStateLocked(completedPath, state);
            CompleteActiveLoadLocked(completedPath);
            return;
        }

        state.PackedScene = ResourceLoader.LoadThreadedGet(completedPath) as PackedScene;
        if (state.PackedScene == null)
        {
            Application.Log.Error($"SceneAssetLoader: failed to load scene. path='{completedPath}'");
            state.Status = SceneResourceLoadStatus.Failed;
            RemoveUnclaimedExternalStateLocked(completedPath, state);
            CompleteActiveLoadLocked(completedPath);
            return;
        }

        if (IsResourcePath(completedPath))
        {
            Application.Asset.Scene.Cache(completedPath, state.PackedScene);
            state.PackedScene = null;
        }

        state.Status = SceneResourceLoadStatus.Loaded;
        RemoveUnclaimedExternalStateLocked(completedPath, state);
        CompleteActiveLoadLocked(completedPath);
    }

    private static void StartNextLoadLocked()
    {
        if (_activeLoadPath != null)
        {
            return;
        }

        while (_pendingLoadPaths.Count > 0)
        {
            string nextPath = _pendingLoadPaths.Dequeue();
            if (!_loadStates.TryGetValue(nextPath, out SceneResourceLoadState state)
                || state.Status != SceneResourceLoadStatus.Queued)
            {
                continue;
            }

            // Godotのスレッドロードを複数scnで並行させるとクラッシュするため、要求は常に1件ずつ開始する。
            ResourceLoader.CacheMode cacheMode = IsResourcePath(nextPath)
                ? ResourceLoader.CacheMode.Reuse
                : ResourceLoader.CacheMode.Ignore;
            Error error = ResourceLoader.LoadThreadedRequest(nextPath, "", false, cacheMode);
            if (error != Error.Ok)
            {
                Application.Log.Error($"SceneAssetLoader: failed to request threaded load. path='{nextPath}', error='{error}'");
                state.Status = SceneResourceLoadStatus.Failed;
                continue;
            }

            state.Status = SceneResourceLoadStatus.Requested;
            _activeLoadPath = nextPath;
            return;
        }
    }

    private static void CompleteActiveLoadLocked(string path)
    {
        if (_activeLoadPath == path)
        {
            _activeLoadPath = null;
        }

        StartNextLoadLocked();
    }

    private static void ReleaseUncachedConsumer(string path)
    {
        lock (_loadStatesLock)
        {
            if (_loadStates.TryGetValue(path, out SceneResourceLoadState state))
            {
                ReleaseUncachedConsumerLocked(path, state);
            }
        }
    }

    private static void ReleaseUncachedConsumerLocked(string path, SceneResourceLoadState state)
    {
        if (IsResourcePath(path) || state.PendingConsumers <= 0)
        {
            return;
        }

        state.PendingConsumers--;
        RemoveUnclaimedExternalStateLocked(path, state);
    }

    private static void RemoveUnclaimedExternalStateLocked(string path, SceneResourceLoadState state)
    {
        if (!IsResourcePath(path)
            && state.PendingConsumers == 0
            && state.Status != SceneResourceLoadStatus.Requested)
        {
            state.PackedScene = null;
            _loadStates.Remove(path);
        }
    }

    private static bool IsResourcePath(string path)
    {
        return !string.IsNullOrWhiteSpace(path)
            && path.StartsWith("res://", StringComparison.Ordinal);
    }

    private static bool IsValidAndNotQueuedForDeletion(Node node)
    {
        return node != null && GodotObject.IsInstanceValid(node) && !node.IsQueuedForDeletion();
    }

    private enum SceneResourceLoadStatus
    {
        Queued,
        Requested,
        Loaded,
        Failed,
    }

    private sealed class SceneResourceLoadState
    {
        public SceneResourceLoadStatus Status { get; set; } = SceneResourceLoadStatus.Requested;

        public PackedScene PackedScene { get; set; }

        public int PendingConsumers { get; set; }
    }

    private static bool TryResolveExistingFilePath(string path, out string resolvedPath)
    {
        resolvedPath = path;

        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        if (path.StartsWith("res://", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("user://", StringComparison.OrdinalIgnoreCase))
        {
            return ResourceLoader.Exists(path);
        }

        if (Path.IsPathRooted(path))
        {
            return File.Exists(path);
        }

        string relativePath = ProjectSettings.GlobalizePath(path);
        return File.Exists(relativePath);
    }

    #endregion
}
