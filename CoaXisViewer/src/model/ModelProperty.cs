// TODO: ちゃんと内容理解する

using System;
using System.Collections.Generic;
using Godot;

/// <summary>
/// モデルに付与される属性情報を表すクラス。
/// 木構造で管理され、ModelPropertyRegistryHub によって管理される。
/// </summary>
public class ModelProperty
{
    #region Fields

    /// <summary>
    /// 子プロパティを保持するための辞書
    /// </summary>
    private readonly Dictionary<Guid, ModelProperty> _children = new();

    /// <summary>
    /// 表示設定
    /// </summary>
    private ModelVisibility _visibility;

    /// <summary>
    /// パーティクルの色、null の場合はパーティクルシーン既定の色を使う
    /// </summary>
    private Color? _particleColor;

    /// <summary>
    /// このプロパティに対応する描画用パーティクル
    /// </summary>
    private ModelParticle _particleNode;

    #endregion

    #region Properties

    /// <summary>
    /// プロパティの一意識別子
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// 親プロパティまたは所属先モデル実体の識別子。ルート実体に直接ぶら下がる場合はその実体Id
    /// </summary>
    public Guid ParentId { get; }

    /// <summary>
    /// プロパティの種類識別（元の Parameter 名など）
    /// </summary>
    public string PropertyType { get; }

    /// <summary>
    /// 値の型（"int", "float", "string", "bool", "datetime", "Vector3", "Quaternion", "Color", "Transform" など）
    /// </summary>
    public string ValueType { get; }

    /// <summary>
    /// 文字列化された値
    /// </summary>
    public string Value { get; private set; }

    /// <summary>
    /// 表示設定、変更時は ModelPropertyStateHub 経由で通知する
    /// </summary>
    public ModelVisibility Visibility
    {
        get => _visibility;
        set
        {
            if (_visibility == value)
            {
                return;
            }

            _visibility = value;
            Application.Model.Property.State.NotifyVisibility(Id);
        }
    }

    /// <summary>
    /// 表示設定と親階層（親プロパティ、所属モデル）から導出した実効表示状態
    /// </summary>
    public bool IsVisible => ModelVisibilityResolver.ResolveIsVisible(this);

    /// <summary>
    /// ツリー表示に使うアイコンID
    /// </summary>
    public string IconId { get; }

    /// <summary>
    /// 3D Viewer 上のパーティクル表現の種類
    /// </summary>
    public ModelParticleType ParticleType { get; }

    /// <summary>
    /// パーティクルのテクスチャ画像パス、空の場合は既定の星画像
    /// </summary>
    public string ParticleTexturePath { get; }

    /// <summary>
    /// パーティクルの色、null の場合は種類ごとの既定色
    /// </summary>
    public Color? ParticleColor => _particleColor;

    /// <summary>
    /// このプロパティに対応する描画用パーティクル、未生成の場合は null
    /// </summary>
    public ModelParticle ParticleNode => _particleNode != null && GodotObject.IsInstanceValid(_particleNode) ? _particleNode : null;

    /// <summary>
    /// 親プロパティを取得する。親がモデル実体（ルートプロパティ）または未登録の場合は null を返す
    /// </summary>
    public ModelProperty ParentProperty => ParentId != Guid.Empty ? Application.Model.Property.Registry.Get(ParentId) : null;

    /// <summary>
    /// 親プロパティの参照（ParentProperty のエイリアス）
    /// </summary>
    public ModelProperty Parent => ParentProperty;

    /// <summary>
    /// 親がモデル実体である場合に、その親モデル実体を取得する。親がプロパティまたは未登録の場合は null を返す
    /// </summary>
    public ModelEntity ParentEntity => ParentId != Guid.Empty ? Application.Model.Entity.Registry.Get(ParentId) : null;

    /// <summary>
    /// このプロパティがモデル実体に直接紐づくルートプロパティかどうかを判定する
    /// </summary>
    public bool IsRootProperty => ParentEntity != null;

    /// <summary>
    /// 子プロパティの一覧を返す
    /// </summary>
    public IReadOnlyCollection<ModelProperty> Children => _children.Values;

    #endregion

    #region Lifecycle

    /// <summary>
    /// プロパティデータを生成する
    /// </summary>
    /// <param name="id">プロパティの一意識別子</param>
    /// <param name="parentId">親プロパティまたは所属先モデル実体の識別子</param>
    /// <param name="propertyType">プロパティの種類識別</param>
    /// <param name="valueType">値の型名</param>
    /// <param name="value">文字列化された値</param>
    /// <param name="visibility">表示設定</param>
    /// <param name="iconId">ツリー表示に使うアイコンID。空の場合はアイコンなし</param>
    /// <param name="particleType">パーティクル表現の種類</param>
    /// <param name="particleColor">パーティクルの色、null の場合は既定色</param>
    /// <param name="particleTexturePath">パーティクルのテクスチャ画像パス、空の場合は既定</param>
    public ModelProperty(
        Guid id,
        Guid parentId,
        string propertyType,
        string valueType,
        string value,
        ModelVisibility visibility = ModelVisibility.Inherit,
        string iconId = "",
        ModelParticleType particleType = ModelParticleType.None,
        Color? particleColor = null,
        string particleTexturePath = null)
    {
        ParticleTexturePath = particleTexturePath ?? string.Empty;
        Id = id;
        ParentId = parentId;
        PropertyType = propertyType ?? string.Empty;
        ValueType = valueType ?? string.Empty;
        Value = value ?? string.Empty;
        _visibility = visibility;
        IconId = iconId ?? string.Empty;
        ParticleType = particleType;
        _particleColor = particleColor;
    }

    #endregion

    #region Methods

    /// <summary>
    /// このプロパティが所属する ModelEntity を再帰的に探索して取得する。
    /// </summary>
    /// <returns>所属する ModelEntity。見つからない場合は null</returns>
    public ModelEntity GetEntity()
    {
        return Application.Model.Property.Registry.GetOwningEntity(Id);
    }
    
    /// <summary>
    /// 表示設定を次の状態（Inherit → Visible → Invisible）へ切り替える。
    /// </summary>
    public void ToggleVisibility()
    {
        if (!Application.Model.Property.Registry.IsRegistered(Id))
        {
            Application.Log.Warn($"ModelProperty: toggle target is not registered. propertyId='{Id}'");
            return;
        }

        Application.Command.Execute(new SetModelPropertyVisibilityCommand(
            Id,
            ModelVisibilityResolver.GetNextVisibility(Visibility)));
    }

    /// <summary>
    /// 値を更新する
    /// </summary>
    /// <param name="newValue">新しい文字列化された値</param>
    public void SetValue(string newValue)
    {
        // 描画更新や外部IPCからの指示時にプロパティ値を書き換える
        Value = newValue ?? string.Empty;
    }

    /// <summary>
    /// パーティクルの色を更新し、対応するパーティクルへ反映する
    /// </summary>
    /// <param name="color">新しい色、null の場合は既定色へ戻す</param>
    public void SetParticleColor(Color? color)
    {
        if (_particleColor == color)
        {
            return;
        }

        _particleColor = color;
        ParticleNode?.Refresh();
    }

    /// <summary>
    /// 描画用パーティクルを関連付ける
    /// </summary>
    /// <param name="particle">関連付けるパーティクル</param>
    public void AttachParticle(ModelParticle particle)
    {
        _particleNode = particle;
    }

    /// <summary>
    /// 描画用パーティクルをシーンから外して破棄する
    /// </summary>
    public void DetachParticle()
    {
        ModelParticle particle = ParticleNode;
        _particleNode = null;
        if (particle == null)
        {
            return;
        }

        particle.GetParent()?.RemoveChild(particle);
        particle.QueueFree();
    }

    /// <summary>
    /// 子プロパティを登録する。同一 Id の重複追加は無視する。
    /// </summary>
    /// <param name="child">追加対象の子プロパティ</param>
    public void Attach(ModelProperty child)
    {
        if (child == null)
        {
            return;
        }

        if (_children.ContainsKey(child.Id))
        {
            return;
        }

        _children.Add(child.Id, child);
    }

    /// <summary>
    /// 子プロパティの登録を解除する
    /// </summary>
    /// <param name="child">解除対象の子プロパティ</param>
    public void Detach(ModelProperty child)
    {
        if (child == null)
        {
            return;
        }

        _children.Remove(child.Id);
    }

    /// <summary>
    /// 子プロパティ一覧をクリアする
    /// </summary>
    public void Clear()
    {
        _children.Clear();
    }

    #endregion
}
