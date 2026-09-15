using Godot;

/// <summary>
/// ModelNode 配下の内部構造をまとめる抽象コンポーネントルート
/// </summary>
public partial class ModelComponents : Node3D
{
    #region Properties

    /// <summary>
    /// ロードしたシーン（メッシュ・コライダー等を含む）を保持する Node3D
    /// </summary>
    /// <remarks>
    /// scn/tscn にはメッシュ以外にコライダー等も含まれ得るため、種別を限定しない名前にしている
    /// </remarks>
    public Node3D Content { get; private set; }

    /// <summary>
    /// ロード済みシーンの内容が存在するかどうかを示す
    /// </summary>
    public bool HasContent => Content != null && Content.GetChildCount() > 0;

    /// <summary>
    /// エフェクトを保持する Node3D
    /// </summary>
    public Node3D Effect { get; private set; }

    /// <summary>
    /// エフェクトが存在するかどうかを示す
    /// </summary>
    public bool HasEffect => Effect != null && Effect.GetChildCount() > 0;

    /// <summary>
    /// AABB中心合わせのオフセットを吸収するノード。Content/Effect はこの子として生成される
    /// </summary>
    public Node3D Pivot { get; private set; }

    /// <summary>
    /// Initialize() が既に実行済みかどうかを示す（子ノードは Ensure* が個別に管理するため冪等化専用）
    /// </summary>
    private bool _isInitialized;

    /// <summary>
    /// PulseScale() で実行中の Tween、多重実行時に前回分を停止するために保持する
    /// </summary>
    private Tween _pulseTween;

    /// <summary>
    /// SpinAppeal() で実行中の Tween、多重実行時に前回分を停止するために保持する
    /// </summary>
    private Tween _spinTween;

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
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;
        Name = GetType().Name;

        // Content/Line/Collider/Effect はここでは作らず、ModelFactory 等が必要とした時点で
        // Ensure* メソッド経由で生成する（未使用アセットのノード生成を避けるため）
        InitializeDerivedComponents();
    }

    /// <summary>
    /// ロードしたシーン（メッシュ・コライダー等を含む）を保持するノードを取得する。未生成の場合はここで生成する
    /// </summary>
    /// <remarks>
    /// tscn は SceneBuilder が座標変換・原点補正済みで出力するため、ここでは座標変換を行わない。
    /// </remarks>
    /// <returns>ロードしたシーンの内容を保持する Node3D</returns>
    public Node3D EnsureContent()
    {
        return Content ??= CreateNode<Node3D>("Content");
    }

    /// <summary>
    /// エフェクト用ノードを取得する。未生成の場合はここで生成する
    /// </summary>
    /// <returns>エフェクトを保持する Node3D</returns>
    public Node3D EnsureEffect()
    {
        return Effect ??= CreateNode<Node3D>("Effect");
    }

    /// <summary>
    /// Pivot ノードを取得する。未生成の場合はここで生成する
    /// </summary>
    /// <returns>AABB中心合わせのオフセットを保持する Node3D</returns>
    public Node3D EnsurePivot()
    {
        // CreateNode()はPivot自身へAddChildする実装のため、Pivot生成だけは直接this配下に追加する
        if (Pivot == null)
        {
            Pivot = new Node3D { Name = "Pivot" };
            AddChild(Pivot);
        }

        return Pivot;
    }

    /// <summary>
    /// スケールを基準値と拡大値の間で往復させ、モデルを一時的に目立たせる
    /// </summary>
    /// <remarks>
    /// 選択時の MaterialOverride によるハイライトとは別に、視覚的な注意喚起として
    /// 拡大縮小を反復させる。多重呼び出し時は前回の Tween を止めてから作り直す。
    /// </remarks>
    /// <param name="repeatCount">拡大縮小を反復する回数</param>
    /// <param name="peakScale">反復時に到達する最大スケール倍率</param>
    /// <param name="stepDuration">1回の拡大または縮小にかける秒数</param>
    public void PulseScale(int repeatCount, float peakScale, float stepDuration)
    {
        // 前回のアニメーションが残っていると多重再生でスケールが乱れるため、必ず止めてから作り直す
        _pulseTween?.Kill();

        // AABB中心合わせ済みなら Pivot の原点がメッシュ中心と一致するため、そちらを拡縮対象にする
        Node3D pivot = (Node3D)Pivot ?? this;

        // Kill() は途中経過のスケールで止まるため、毎回必ず基準姿勢から開始させる
        Vector3 baseScale = Vector3.One;
        pivot.Scale = baseScale;

        Vector3 peak = Vector3.One * peakScale;

        _pulseTween = CreateTween();
        for (int i = 0; i < repeatCount; i++)
        {
            _pulseTween.TweenProperty(pivot, "scale", peak, stepDuration)
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.InOut);
            _pulseTween.TweenProperty(pivot, "scale", baseScale, stepDuration)
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.InOut);
        }
    }

    /// <summary>
    /// Y軸を軸に一回転させ、モデルをアピールする
    /// </summary>
    /// <remarks>
    /// 現在の回転値からの相対回転として積み上げるため、途中で ParentModel 側の回転が変わっても
    /// アピール分の回転量だけがそのまま加算される。
    /// </remarks>
    /// <param name="rotationCount">Y軸回転を繰り返す回数</param>
    /// <param name="duration">1回転あたりにかける秒数</param>
    public void SpinAppeal(int rotationCount, float duration)
    {
        // 前回のアニメーションが残っていると多重再生で回転量がずれるため、必ず止めてから作り直す
        _spinTween?.Kill();

        // AABB中心合わせ済みなら Pivot の原点がメッシュ中心と一致するため、そちらを回転対象にする
        Node3D pivot = (Node3D)Pivot ?? this;

        // Kill() は途中経過の回転で止まるため、Y軸だけ0へ戻してから開始する（X/Zは維持する）
        Vector3 rotation = pivot.Rotation;
        rotation.Y = 0f;
        pivot.Rotation = rotation;

        _spinTween = CreateTween();
        _spinTween.TweenProperty(pivot, "rotation:y", Mathf.Tau * rotationCount, duration)
            .AsRelative()
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.InOut);
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
    /// 子ノードを作成して Pivot 配下に追加する
    /// </summary>
    /// <remarks>
    /// Content/Effect は Pivot の子にすることで、AABB中心合わせのオフセットを一箇所に閉じ込める
    /// </remarks>
    protected T CreateNode<T>(string name) where T : Node, new()
    {
        var node = new T { Name = name };
        EnsurePivot().AddChild(node);
        return node;
    }

    #endregion
}