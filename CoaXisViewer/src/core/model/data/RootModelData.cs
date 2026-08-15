using System;
using Godot;

/// <summary>
/// モデルのルートデータで、アプリケーション全体で唯一のモデルデータ
/// </summary>
public class RootModelData : ModelData
{
    /// <summary>
    /// ルートモデルは常に表示されるため、表示設定を Visible に固定する
    /// </summary>
    public override ModelVisibility Visibility
    {
        get => ModelVisibility.Visible;
        internal set { }
    }

    /// <summary>
    /// ルートモデルに固定で割り当てる ID
    /// </summary>
    public static readonly Guid RootId = Guid.Parse(Constant.Model.RootModelId);

    /// <summary>
    /// ルートモデルを生成する
    /// </summary>
    public RootModelData()
        : base(
            RootId,
            Guid.Empty,
            "Root",
            "Root",
            Vector3.Zero,
            Quaternion.Identity,
            ModelVisibility.Visible,
            string.Empty,
            string.Empty,
            string.Empty)
    {
        // Root は常に存在するため、初期化直後からロード済みとして扱う、Registryの制約上、Initialized状態で追加する必要がある
        Status = ModelStatus.Initialized;

        Application.Model.Registry.Register(this);

        // 登録後に最終状態へ更新する
        Status = ModelStatus.Loaded;

        Node = new RootModelNode(RootId);
    }
}
