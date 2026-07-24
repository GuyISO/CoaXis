using Godot;

/// <summary>
/// AnyModel 配下の内部構造をまとめる抽象コンポーネントルート
/// </summary>
public partial class AnyComponents : Node3D
{
    #region Properties

    /// <summary>
    /// メッシュを保持する Node3D
    /// </summary>
    public Node3D Mesh { get; private set; }

    /// <summary>
    /// メッシュが存在するかどうかを示す
    /// </summary>
    public bool HasMesh => Mesh != null && Mesh.GetChildCount() > 0;

    /// <summary>
    /// 衝突形状を保持する StaticBody3D
    /// </summary>
    public StaticBody3D Collider { get; private set; }

    /// <summary>
    /// 衝突形状が存在するかどうかを示す
    /// </summary>
    public bool HasCollider => Collider != null && Collider.GetChildCount() > 0;

    /// <summary>
    /// エフェクトを保持する Node3D
    /// </summary>
    public Node3D Effect { get; private set; }

    /// <summary>
    /// エフェクトが存在するかどうかを示す
    /// </summary>
    public bool HasEffect => Effect != null && Effect.GetChildCount() > 0;

    #endregion

    #region Lifecycle

    public override void _Ready()
    {
        Initialize();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 内部構造を初期化する
    /// </summary>
    public void Initialize()
    {
        if (Mesh != null)
        {
            return;
        }

        Name = GetType().Name;

        Mesh = CreateNode<Node3D>("Mesh");
        Collider = CreateNode<StaticBody3D>("Collider");
        Effect = CreateNode<Node3D>("Effect");

        InitializeDerivedComponents();
    }

    #endregion

    #region Internal Helpers

    /// <summary>
    /// 派生クラス固有の内部構造を初期化する
    /// </summary>
    protected virtual void InitializeDerivedComponents()
    {
    }

    /// <summary>
    /// 子ノードを作成して追加する
    /// </summary>
    protected T CreateNode<T>(string name) where T : Node, new()
    {
        var node = new T { Name = name };
        AddChild(node);
        return node;
    }

    #endregion
}