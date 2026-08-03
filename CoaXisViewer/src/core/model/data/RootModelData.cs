using System;

/// <summary>
/// モデルのルートデータで、アプリケーション全体で唯一のモデルデータ
/// </summary>
public class RootModelData : ModelData
{
    // 固定 GUID
    public static readonly Guid RootId = Guid.Parse(Constant.Model.RootModelId);

    public RootModelData()
        : base(RootId, Guid.Empty, "Root", "Root")
    {
        // Root は常にロード済み扱い
        Status = ModelStatus.Initialized;

        Application.Model.Registry.Register(this);

        // Root は常にロード済み扱い
        Status = ModelStatus.Loaded;

        Node = new RootModelNode(RootId);
    }
}
