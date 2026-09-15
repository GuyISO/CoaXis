using System;
using Godot;

/// <summary>
/// シーン全体のルートとなる特別な ModelEntity
/// </summary>
public class RootModelEntity : ModelEntity
{
    #region Constants

    /// <summary>
    /// ルート ModelEntity の固定識別子
    /// </summary>
        public static readonly Guid RootEntityId = Guid.Parse(Constant.Model.RootEntityId);

    /// <summary>
    /// ルート ModelEntity の固定名
    /// </summary>
    public const string RootName = "Root";

    #endregion

    #region Properties

    /// <summary>
    /// ルートモデルは常に可視状態固定
    /// </summary>
    public override ModelVisibility Visibility
    {
        get => ModelVisibility.Visible;
        internal set { }
    }

    /// <summary>
    /// ルートモデルは常に展開状態固定
    /// </summary>
    public override bool IsCollapsed
    {
        get => false;
        internal set { }
    }
    
    #endregion

    #region Constructors

    /// <summary>
    /// RootModelEntity を生成する
    /// </summary>
    public RootModelEntity()
        : base(
            RootEntityId,
            Guid.Empty,
            string.Empty,
            RootName,
            Vector3.Zero,
            Quaternion.Identity,
            ModelVisibility.Visible,
            false,
            string.Empty,
            string.Empty,
            false)
    {
        // Root は常に存在するため、初期化直後からロード済みとして扱う、Registryの制約上、Initialized状態で追加する必要がある
        Status = ModelStatus.Initialized;

        Application.Model.Registry.RegisterEntity(this);

        // 登録後に最終状態へ更新する
        Status = ModelStatus.Loaded;

        Node = new RootModelNode(RootEntityId);
    }

    #endregion
}
