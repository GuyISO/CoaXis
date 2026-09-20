using Godot;
using System;

public partial class EmbededModelPropertyTree : ModelPropertyTree
{
    #region Lifecycle

    public override void _Ready()
    {
        base._Ready();

        // Serviceが参照するために自身を設定する
        Application.Model.Service.SetEmbededModelPropertyTree(this);

        SubscribeEvents();
    }

    public override void _ExitTree()
    {
        UnsubscribeEvents();

        base._ExitTree();
    }

    #endregion

    #region Events

    /// <summary>
    /// Applicationイベントの購読を開始する
    /// </summary>
    private void SubscribeEvents()
    {
        Application.Pick.Event.ResultNotified += OnPickResultNotified;
    }

    /// <summary>
    /// Applicationイベントの購読を解除する
    /// </summary>
    private void UnsubscribeEvents()
    {
        Application.Pick.Event.ResultNotified -= OnPickResultNotified;
    }

    /// <summary>
    /// ピック結果の通知を受け取り、EntityIdで表示を更新する
    /// </summary>
    /// <param name="pickResult">通知されたピック結果（複数一括のResultsNotifiedは対象外）</param>
    private void OnPickResultNotified(PickResult pickResult)
    {
        // 実体がない場合、ShowはEntity未取得のままツリーをクリアするだけになる
        Show(pickResult != null ? pickResult.EntityId : Guid.Empty);
    }

    #endregion
}
