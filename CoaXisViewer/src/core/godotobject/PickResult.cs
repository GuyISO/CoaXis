using Godot;

/// <summary>
/// レイキャストや範囲選択で空間上から取得した情報を表すオブジェクト
/// </summary>
public partial class PickResult : RefCounted
{
    /// <summary>
    /// 取得に成功したかどうかを示す
    /// </summary>
    public bool HasHit { get; }
    /// <summary>
    /// 取得したコライダーノードへの参照
    /// </summary>
    public Node3D Collider { get; }
    /// <summary>
    /// 取得したコライダーオブジェクトのRID
    /// </summary>
    public Rid Rid { get; }
    /// <summary>
    /// 取得したコライダーの親ノードへの参照で、本プロジェクトではコライダーを AnyModel の子ノードとして配置するためその参照を取得しやすくしている
    /// </summary>
    public AnyModel Model { get; }
    /// <summary>
    /// 取得した位置のワールド座標で、レイキャスト時のみ有効で範囲選択では取得されない
    /// </summary>
    public Vector3 Position { get; }
    /// <summary>
    /// 取得した面の法線ベクトルで、レイキャスト時のみ有効で範囲選択では取得されない
    /// </summary>
    public Vector3 Normal { get; }
    /// <summary>
    /// 取得した位置までの距離で、レイキャスト時のみ有効で範囲選択では取得されない
    /// </summary>
    public float Distance { get; }

    /// <summary>
    /// ヒットなし状態を表す PickResult を初期化する
    /// </summary>
    public PickResult()
        : this(false, null, default, null, Vector3.Zero, Vector3.Zero, 0f)
    {
    }

    /// <summary>
    /// PickUtility からのみ使用する完全初期化コンストラクタ
    /// </summary>
    /// <param name="hasHit">ヒットしたかどうか</param>
    /// <param name="collider">ヒットしたコライダー</param>
    /// <param name="rid">ヒットしたRID</param>
    /// <param name="model">ヒットしたモデル</param>
    /// <param name="position">ヒット位置</param>
    /// <param name="normal">ヒット法線</param>
    /// <param name="distance">ヒット距離</param>
    internal PickResult(bool hasHit, Node3D collider, Rid rid, AnyModel model, Vector3 position, Vector3 normal, float distance)
    {
        HasHit = hasHit;
        Collider = collider;
        Rid = rid;
        Model = model;
        Position = position;
        Normal = normal;
        Distance = distance;
    }
}