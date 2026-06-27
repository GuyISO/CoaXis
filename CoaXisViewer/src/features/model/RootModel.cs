using Godot;
using System;

/// <summary>
/// RootModel はシーンルートに配置されるモデルで、モデルロード要求イベントを受け取り非同期でモデルをロードしてシーンへ追加する役割を持つ
/// </summary>
public partial class RootModel : AnyModel
{
    #region Lifecycle

    public override void _Ready()
    {
        // イベントハンドラの登録
        ModelEventHub.Instance.LoadModelRequested += OnLoadModelRequested;
    }

    public override void _ExitTree()
    {
        // イベントハンドラの登録解除
        ModelEventHub.Instance.LoadModelRequested -= OnLoadModelRequested;
    }

    #endregion

    #region Events

    /// <summary>
    /// モデルロード要求イベントのハンドラで ModelLoader を使用して非同期でモデルをロードし、ロード完了後にシーンへ追加する
    /// </summary>
    /// <param name="path">ロードするモデルのパス</param>
    private async void OnLoadModelRequested(string path)
    {
        AnyModel model = new AnyModel();
        model.Name = System.IO.Path.GetFileNameWithoutExtension(path);
        AddChild(model);

        bool loaded = await ModelLoader.LoadModelAsync(model, path);
        if (!loaded)
        {
            model.QueueFree();
            return;
        }

        // モデルの衝突形状を設定するために、1フレーム待つ
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        ModelColliderBuilder.AddCollider(model);

        ModelEventHub.RequestAddModel(model, this);

    }

    #endregion
}
