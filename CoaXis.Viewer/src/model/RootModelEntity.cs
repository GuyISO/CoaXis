using System;
using CoaXis.Protocol.Viewer;
using Godot;

/// <summary>
/// シーン全体のルートとなる特別な ModelEntity
/// </summary>
public class RootModelEntity : ModelEntity
{
    #region Constants

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
        set { }
    }

    /// <summary>
    /// ルートモデルは常に展開状態固定
    /// </summary>
    public override bool IsCollapsed
    {
        get => false;
        set { }
    }
    
    #endregion

    #region Constructors

    /// <summary>
    /// RootModelEntity を生成する
    /// </summary>
    public RootModelEntity()
        : base(
            ViewerConstants.RootModelEntityId,
            Guid.Empty,
            string.Empty,
            RootName,
            Vector3.Zero,
            Quaternion.Identity,
            ModelVisibility.Visible,
            false,
            string.Empty,
            string.Empty)
    {
        // Root は常に存在するため、初期化直後からロード済みとして扱う、Registryの制約上、Initialized状態で追加する必要がある
        InitializeForRegistration();

        Application.Model.Entity.Registry.Register(this);

        MarkLoaded();

        Node = new RootModelNode(ViewerConstants.RootModelEntityId);
    }

    #endregion
}
