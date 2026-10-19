using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// ModelNode はモデル階層を管理するクラスで、ロードしたシーンを直下に保持する
/// </summary>
public partial class ModelNode : Node3D
{
    #region Properties

    /// <summary>
    /// このモデル実体の識別子
    /// </summary>
    public Guid EntityId { get; } = Guid.Empty;

    /// <summary>
    /// このモデルに対応する ModelEntity を取得する
    /// </summary>
    public ModelEntity Entity => EntityId == Guid.Empty ? null : Application.Model.Entity.Registry.Get(EntityId);

    /// <summary>
    /// ロードしたシーンのルート、未ロードまたは Node3D 以外の場合は null
    /// </summary>
    public Node3D SceneRoot { get; private set; }

    /// <summary>
    /// アニメーション用の Pivot、Emphasize/SpinAppeal 初回実行時にのみ生成する
    /// </summary>
    private Node3D _pivot;

    /// <summary>
    /// 実行中の Emphasize Tween、多重実行時に前回分を停止するために保持する
    /// </summary>
    private Tween _pulseTween;

    /// <summary>
    /// 実行中の SpinAppeal Tween、多重実行時に前回分を停止するために保持する
    /// </summary>
    private Tween _spinTween;

    /// <summary>
    /// このモデルの親モデルを取得する、親モデルが存在しない場合は null を返す
    /// </summary>
    /// <returns>親モデル、存在しない場合は null</returns>
    public virtual ModelNode ParentModel => GetParentOrNull<ModelNode>();

    /// <summary>
    /// このモデルの子モデルのリストを取得する、子モデルが存在しない場合は空のリストを返す
    /// </summary>
    /// <returns>子モデルのリスト、存在しない場合は空のリスト</returns>
    public List<ModelNode> ChildModels => GetChildren().OfType<ModelNode>().ToList();

    #endregion

    #region Lifecycle

    public ModelNode(Guid entityId)
    {
        // Registry に先に ModelEntity が存在していることを前提にノードを作る
        ModelEntity modelEntity = Application.Model.Entity.Registry.Get(entityId);
        if (modelEntity == null)
        {
            throw new ArgumentException($"ModelNode: ModelEntity not found for entityId='{entityId}'");
        }

        EntityId = entityId;
    }

    #endregion

    #region Methods

    /// <summary>
    /// ロードしたシーンのインスタンスをこのモデル直下へ追加する
    /// </summary>
    /// <param name="instance">インスタンス化済みのシーンルート</param>
    public void AttachScene(Node instance)
    {
        AddChild(instance);
        SceneRoot = instance as Node3D;
    }

    /// <summary>
    /// モデルをスケール反復アニメーションで一時的に目立たせる
    /// </summary>
    /// <remarks>
    /// ModelNode 自身ではなく子の Components をスケーリングする、
    /// ModelNode のローカル座標を基準にした配置や当たり判定に影響を与えないようにするため。
    /// </remarks>
    /// <param name="repeatCount">拡大縮小を反復する回数、デフォルトは3回</param>
    /// <param name="peakScale">反復時に到達する最大スケール倍率、デフォルトは1.15倍</param>
    /// <param name="stepDuration">1回の拡大または縮小にかける秒数、デフォルトは0.5秒</param>
    public void Emphasize(int repeatCount = 3, float peakScale = 1.15f, float stepDuration = 0.5f)
    {
        Node3D pivot = EnsurePivot();
        if (pivot == null)
        {
            return;
        }

        // Kill() は途中経過のスケールで止まるため、毎回基準姿勢から開始する
        _pulseTween?.Kill();
        pivot.Scale = Vector3.One;

        Vector3 peak = Vector3.One * peakScale;
        _pulseTween = CreateTween();
        for (int i = 0; i < repeatCount; i++)
        {
            _pulseTween.TweenProperty(pivot, "scale", peak, stepDuration)
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.InOut);
            _pulseTween.TweenProperty(pivot, "scale", Vector3.One, stepDuration)
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.InOut);
        }
    }

    /// <summary>
    /// モデルをY軸で一回転させてアピールする
    /// </summary>
    /// <remarks>
    /// ModelNode 自身ではなく子の Components を回転させる、Emphasize() と同様に
    /// ModelNode のローカル座標を基準にした配置や当たり判定に影響を与えないようにするため。
    /// </remarks>
    /// <param name="rotationCount">回転を繰り返す回数、デフォルトは3回</param>
    /// <param name="duration">1回転あたりにかける秒数、デフォルトは3.0秒</param>
    public void SpinAppeal(int rotationCount = 1, float duration = 3.0f)
    {
        Node3D pivot = EnsurePivot();
        if (pivot == null)
        {
            return;
        }

        // Kill() は途中経過の回転で止まるため、Y軸だけ0へ戻してから開始する（X/Zは維持する）
        _spinTween?.Kill();
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

    #region Helpers

    /// <summary>
    /// モデル配下のメッシュとコライダーへ表示レイヤーを適用する
    /// </summary>
    public void ApplyVisibilityLayer(bool isVisible)
    {
        uint layer = (uint)(isVisible ? ViewportLayer.VisibleEntity : ViewportLayer.InvisibleEntity);
        ApplyVisibilityLayerRecursive(this, layer);
    }

    private static void ApplyVisibilityLayerRecursive(Node node, uint layer)
    {
        if (node is GeometryInstance3D geometryInstance && node is not ModelParticle)
        {
            geometryInstance.Layers = layer;
        }

        if (node is CollisionObject3D collisionObject)
        {
            collisionObject.CollisionLayer = layer;
        }

        foreach (Node child in node.GetChildren())
        {
            if (child is ModelNode)
            {
                continue;
            }

            ApplyVisibilityLayerRecursive(child, layer);
        }
    }

    /// <summary>
    /// アニメーション用 Pivot を取得する、未生成ならここでシーンルートを Pivot 配下へ付け替えて生成する
    /// </summary>
    /// <remarks>
    /// 常時ノードを増やさないため遅延生成する。子 ModelNode は巻き込まず SceneRoot のみ付け替える。
    /// </remarks>
    /// <returns>Pivot、シーン未ロードの場合は null</returns>
    private Node3D EnsurePivot()
    {
        if (_pivot != null && IsInstanceValid(_pivot))
        {
            return _pivot;
        }

        if (SceneRoot == null || !IsInstanceValid(SceneRoot))
        {
            return null;
        }

        _pivot = new Node3D { Name = "Pivot" };
        AddChild(_pivot);
        SceneRoot.Reparent(_pivot, false);

        if (Entity != null && Entity.AlignToAabbCenter)
        {
            ModelMeshCenterAligner.Pivot(_pivot);
        }

        return _pivot;
    }

    #endregion
}