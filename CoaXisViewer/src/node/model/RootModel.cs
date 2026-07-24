using Godot;
using System;

/// <summary>
/// モデルのルートノードで、シーンルートに配置されるモデル
/// </summary>
public partial class RootModel : AnyModel
{
    #region Properties

    /// <summary>
    /// RootModel は常に親モデルを持たない
    /// </summary>
    public override AnyModel ParentModel => null;

    #endregion

    #region Lifecycle

    public override void _Ready()
    {
        base._Ready();

        SubscribeApplicationEvents();
    }

    public override void _ExitTree()
    {
        UnsubscribeApplicationEvents();

        base._ExitTree();
    }

    #endregion

    #region Events

    /// <summary>
    /// Applicationイベントの購読を開始する
    /// </summary>
    private void SubscribeApplicationEvents()
    {
        Application.Model.Event.LoadModelRequested += OnLoadModelRequested;
    }

    /// <summary>
    /// Applicationイベントの購読を解除する
    /// </summary>
    private void UnsubscribeApplicationEvents()
    {
        Application.Model.Event.LoadModelRequested -= OnLoadModelRequested;
    }

    /// <summary>
    /// モデルロード要求イベントのハンドラで ModelLoadUtility を使用して非同期でモデルをロードし、ロード完了後にシーンへ追加する
    /// </summary>
    /// <param name="path">ロードするモデルのパス</param>
    private async void OnLoadModelRequested(string path)
    {
        // TODO: 暫定的にこの直下に入れているのでドメインに合わせて洗練させる

        AnyModel model = new SurfaceModel();
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

    #region Internal Helpers

    protected override AnyComponents CreateComponents()
    {
        return new RootComponents();
    }

    #endregion
}
