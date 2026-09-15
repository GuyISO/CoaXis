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
    private static readonly object _loadStatesLock = new();

    #region Public Methods

    /// <summary>
    /// 完了済みまたは失敗済みのキャッシュだけを破棄する（モデルレジストリのクリアに合わせて呼び出す想定）
    /// </summary>
    public static void ClearCompletedCache()
    {
        lock (_loadStatesLock)
        {
            // Godot側にキャンセルAPIがないため、進行中要求の追跡は残して二重要求を防ぐ
            var completedPaths = new List<string>();
            foreach ((string path, SceneResourceLoadState state) in _loadStates)
            {
                if (state.Status != SceneResourceLoadStatus.Requested)
                {
                    completedPaths.Add(path);
                }
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
            // 進行中・完了済みのいずれも同一パスの再投入は不要
            if (_loadStates.ContainsKey(resolvedPath))
            {
                return true;
            }

            Error error = ResourceLoader.LoadThreadedRequest(resolvedPath);
            if (error != Error.Ok)
            {
                Application.Log.Error($"SceneAssetLoader: failed to request threaded load. path='{path}', error='{error}'");
                return false;
            }

            _loadStates.Add(resolvedPath, new SceneResourceLoadState());
        }

        return true;
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
            return SceneLoadResult.Failed;
        }

        PackedScene packedScene;
        lock (_loadStatesLock)
        {
            if (!_loadStates.TryGetValue(resolvedPath, out SceneResourceLoadState state))
            {
                return SceneLoadResult.Failed;
            }

            if (state.Status == SceneResourceLoadStatus.Failed)
            {
                return SceneLoadResult.Failed;
            }

            if (state.Status == SceneResourceLoadStatus.Requested)
            {
                ResourceLoader.ThreadLoadStatus status = ResourceLoader.LoadThreadedGetStatus(resolvedPath);
                if (status == ResourceLoader.ThreadLoadStatus.InProgress)
                {
                    return SceneLoadResult.InProgress;
                }

                if (status != ResourceLoader.ThreadLoadStatus.Loaded)
                {
                    Application.Log.Error($"SceneAssetLoader: threaded load failed. path='{path}', status='{status}'");
                    state.Status = SceneResourceLoadStatus.Failed;
                    return SceneLoadResult.Failed;
                }

                packedScene = ResourceLoader.LoadThreadedGet(resolvedPath) as PackedScene;
                if (packedScene == null)
                {
                    Application.Log.Error($"SceneAssetLoader: failed to load scene. path='{path}'");
                    state.Status = SceneResourceLoadStatus.Failed;
                    return SceneLoadResult.Failed;
                }

                state.PackedScene = packedScene;
                state.Status = SceneResourceLoadStatus.Loaded;
            }

            packedScene = state.PackedScene;
        }

        ModelComponents components = modelNode.Components;
        if (!IsValidAndNotQueuedForDeletion(modelNode)
            || components == null
            || !IsValidAndNotQueuedForDeletion(components))
        {
            Application.Log.Warn($"SceneAssetLoader: model node is no longer available. path='{path}'");
            return SceneLoadResult.Failed;
        }

        Node3D contentContainer = components.EnsureContent();
        if (!IsValidAndNotQueuedForDeletion(contentContainer))
        {
            Application.Log.Warn($"SceneAssetLoader: content container is no longer available. path='{path}'");
            return SceneLoadResult.Failed;
        }

        Node instance = packedScene.Instantiate();
        contentContainer.AddChild(instance);

        Application.Log.Info($"SceneAssetLoader: loaded scene '{path}'.");
        return SceneLoadResult.Loaded;
    }

    #endregion

    #region Internal Helpers

    private static bool IsValidAndNotQueuedForDeletion(Node node)
    {
        return node != null && GodotObject.IsInstanceValid(node) && !node.IsQueuedForDeletion();
    }

    private enum SceneResourceLoadStatus
    {
        Requested,
        Loaded,
        Failed,
    }

    private sealed class SceneResourceLoadState
    {
        public SceneResourceLoadStatus Status { get; set; } = SceneResourceLoadStatus.Requested;

        public PackedScene PackedScene { get; set; }
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
