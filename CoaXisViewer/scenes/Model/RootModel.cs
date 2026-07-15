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
        SubscribeApplicationEvents();
    }

    public override void _ExitTree()
    {
        UnsubscribeApplicationEvents();

        base._ExitTree();
    }

    #endregion

    #region Internal Helpers

    private void SubscribeApplicationEvents()
    {
        Application.Model.Event.AskRootModelRequested += OnAskRootModelRequested;
        Application.Model.Event.LoadModelRequested += OnLoadModelRequested;
    }

    private void UnsubscribeApplicationEvents()
    {
        Application.Model.Event.AskRootModelRequested -= OnAskRootModelRequested;
        Application.Model.Event.LoadModelRequested -= OnLoadModelRequested;
    }

    #endregion

    #region Events

    /// <summary>
    /// ルートモデルの通知要求イベントのハンドラで、ModelEvent に対して自身を通知する
    /// </summary>
    private void OnAskRootModelRequested()
    {
        Application.Model.Event.NotifyRootModel(this);
    }

    /// <summary>
    /// モデルロード要求イベントのハンドラで ModelLoadUtility を使用して非同期でモデルをロードし、ロード完了後にシーンへ追加する
    /// </summary>
    /// <param name="path">ロードするモデルのパス</param>
    private async void OnLoadModelRequested(string path)
    {
        AnyModel model = new AnyModel();
        model.Name = System.IO.Path.GetFileNameWithoutExtension(path);
        AddChild(model);

        bool loaded = await ModelLoadUtility.LoadModelAsync(model, path);
        if (!loaded)
        {
            model.QueueFree();
            return;
        }

        // モデルの衝突形状を設定するために、1フレーム待つ
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        ModelColliderBuilder.AddCollider(model);

        Application.Model.Event.AddModel(model, this);

    }

    #endregion
}
