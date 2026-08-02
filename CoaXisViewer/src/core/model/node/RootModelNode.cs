using Godot;
using System;

/// <summary>
/// モデルのルートノードで、シーンルートに配置されるモデル
/// </summary>
public partial class RootModelNode : ModelNode
{
    #region Properties

    /// <summary>
    /// RootModel は常に親モデルを持たない
    /// </summary>
    public override ModelNode ParentModel => null;

    #endregion

    #region Lifecycle

    public RootModelNode(Guid modelId)
        : base(modelId)
    {
    }

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

        // ModelNode modelNode = new ModelNode();
        // modelNode.Name = System.IO.Path.GetFileNameWithoutExtension(path);
        // AddChild(modelNode);

        // bool loaded = await ModelLoadUtility.LoadModelAsync(modelNode, path);
        // if (!loaded)
        // {
        //     modelNode.QueueFree();
        //     return;
        // }

        // // モデルの衝突形状を設定するために、1フレーム待つ
        // await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        // ModelColliderBuilder.AddCollider(modelNode);

        // Application.Model.Event.AddModel(modelNode, this);

        var list = ModelCsvLoader.Load("res://sample//modeldata.csv");

        GD.Print($"Loaded models: {list.Count}");

        foreach (var m in list)
        {
            GD.Print($"{m.Id} parent={m.ParentId}");
        }

    }

    #endregion

    #region Internal Helpers

    protected override ModelComponents CreateComponents()
    {
        return new RootComponents();
    }

    #endregion
}
