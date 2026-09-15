using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// ModelNode はモデル階層を管理するクラスで、内部構造は ModelComponents に委譲する
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
    public ModelEntity Entity => EntityId == Guid.Empty ? null : Application.Model.Registry.GetEntity(EntityId);

    /// <summary>
    /// このモデルの内部構造を保持するコンポーネントルート
    /// </summary>
    /// <returns>内部構造を保持する ModelComponents</returns>
    public ModelComponents Components { get; private set; }

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
        ModelEntity modelEntity = Application.Model.Registry.GetEntity(entityId);
        if (modelEntity == null)
        {
            throw new ArgumentException($"ModelNode: ModelEntity not found for entityId='{entityId}'");
        }

        EntityId = entityId;
    }

    /// <summary>
    /// Godot のツリーに入った後で内部コンポーネントを初期化する
    /// </summary>
    public override void _Ready()
    {
        Components = CreateComponents();
        Components.Initialize();
        AddChild(Components);
    }

    #endregion

    #region Public Methods

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
        Components?.PulseScale(repeatCount, peakScale, stepDuration);
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
        Components?.SpinAppeal(rotationCount, duration);
    }

    #endregion

    #region Internal Helpers

    /// <summary>
    /// モデル配下のメッシュとコライダーへ表示レイヤーを適用する
    /// </summary>
    internal void ApplyVisibilityLayer(bool isVisible)
    {
        uint layer = (uint)(isVisible ? ViewportLayer.Visible : ViewportLayer.Invisible);
        ApplyVisibilityLayerRecursive(this, layer);
    }

    private static void ApplyVisibilityLayerRecursive(Node node, uint layer)
    {
        if (node is GeometryInstance3D geometryInstance)
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
    /// このモデルに追加する内部構造を生成する
    /// </summary>
    /// <returns>内部構造のルートコンポーネント</returns>
    protected virtual ModelComponents CreateComponents()
    {
        return new ModelComponents();
    }

    #endregion
}