using Godot;
using System;
using System.Threading.Tasks;

/// <summary>
/// glTFモデルの非同期ロードを担当するヘルパー
/// </summary>
public static class ModelLoadUtility
{
    #region Public Methods

    /// <summary>
    /// 指定したパスのglTFモデルを非同期でロードし、指定したモデルに追加する
    /// </summary>
    /// <param name="model">メッシュを追加する親モデル</param>
    /// <param name="path">ロードするglTFモデルのパス</param>
    /// <returns>モデルロードに成功した場合はtrue、失敗した場合はfalseを返す</returns>
    public static async Task<bool> LoadModelAsync(AnyModel model, string path)
    {
        // 所要時間計測開始
        Application.Log.Service.Info($"Start loading model: {path}");
        var sw = System.Diagnostics.Stopwatch.StartNew();

        // 非同期でglTFモデルを読み込む
        var doc = new GltfDocument();
        var state = new GltfState();
        var error = await Task.Run(() => doc.AppendFromFile(path, state));

        if (error == Error.Ok)
        {
            var scene = (Node3D)doc.GenerateScene(state);
            model.Mesh.AddChild(scene);

            sw.Stop();
            Application.Log.Service.Info($"Finished loading model: {path} in {sw.ElapsedMilliseconds} ms");
            return true;
        }
        else
        {
            Application.Log.Service.Error($"Failed to load model: {path}, Error: {error}");
            return false;
        }
    }

    #endregion
}
