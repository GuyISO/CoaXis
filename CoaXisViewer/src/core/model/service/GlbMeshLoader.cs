using Godot;
using System;
using System.IO;
using System.Threading.Tasks;

/// <summary>
/// glTFモデルの非同期ロードを担当するヘルパー
/// </summary>
public static class GlbMeshLoader
{
    #region Public Methods

    /// <summary>
    /// 指定したパスのglTFモデルを非同期でロードし、指定したモデルに追加する
    /// </summary>
    /// <param name="modelNode">メッシュを追加する親モデル</param>
    /// <param name="path">ロードするglTFモデルのパス</param>
    /// <returns>モデルロードに成功した場合はtrue、失敗した場合はfalseを返す</returns>
    public static async Task<bool> LoadModelAsync(ModelNode modelNode, string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            Application.Log.Warn("GlbMeshLoader: empty glb path.");
            return false;
        }

        ModelComponents components = modelNode.Components;
        if (components == null)
        {
            Application.Log.Error($"Failed to load model: {path}, components are not initialized.");
            return false;
        }

        if (!TryResolveExistingFilePath(path, out string resolvedPath))
        {
            Application.Log.Warn($"GlbMeshLoader: file not found. path='{path}'");
            return false;
        }

        // 非同期でglTFモデルを読み込む
        var doc = new GltfDocument();
        var state = new GltfState();
        var error = await Task.Run(() => doc.AppendFromFile(resolvedPath, state));

        if (error == Error.Ok)
        {
            var scene = (Node3D)doc.GenerateScene(state);
            components.Mesh.AddChild(scene);

            Application.Log.Info($"Finished loading model: {path}");
            return true;
        }
        else
        {
            Application.Log.Error($"Failed to load model: {path}, Error: {error}");
            return false;
        }
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
            string globalizedPath = ProjectSettings.GlobalizePath(path);
            if (File.Exists(globalizedPath))
            {
                resolvedPath = globalizedPath;
                return true;
            }

            return false;
        }

        if (Path.IsPathRooted(path))
        {
            return File.Exists(path);
        }

        string relativePath = ProjectSettings.GlobalizePath(path);
        if (File.Exists(relativePath))
        {
            resolvedPath = relativePath;
            return true;
        }

        return false;
    }

    #endregion
}
