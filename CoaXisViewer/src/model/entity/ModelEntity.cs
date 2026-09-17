using System;
using Godot;
using System.Collections.Generic;

/// <summary>
/// モデルの実体（Entity）を表すクラス。ModelFactory で生成され、ModelNode（描画ノード）に対応する
/// </summary>
public class ModelEntity
{
    #region Fields

    /// <summary>
    /// 子モデル実体を保持するための辞書
    /// </summary>
    private readonly Dictionary<Guid, ModelEntity> _children = new();

    /// <summary>
    /// このモデル実体に直接紐づくルートプロパティを保持するための辞書
    /// </summary>
    private readonly Dictionary<Guid, ModelProperty> _properties = new();

    #endregion

    #region Properties

    /// <summary>
    /// モデル実体の一意識別子
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// 親モデル実体の識別子。Root 配下の場合は Guid.Empty
    /// </summary>
    public Guid ParentId { get; }

    /// <summary>
    /// モデル種別
    /// </summary>
    public string Type { get; }

    /// <summary>
    /// 表示名
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 座標変換後の配置位置
    /// </summary>
    public Vector3 Position { get; }

    /// <summary>
    /// 座標変換後の回転
    /// </summary>
    public Quaternion Rotation { get; }

    /// <summary>
    /// 表示状態
    /// </summary>
    public virtual ModelVisibility Visibility { get; internal set; }

    /// <summary>
    /// ツリー表示時の初期折り畳み状態
    /// </summary>
    public virtual bool IsCollapsed { get; internal set; }

    /// <summary>
    /// アイコン画像のパス
    /// </summary>
    public string IconPath { get; }

    /// <summary>
    /// 読み込み対象シーンのパス
    /// </summary>
    public string ScenePath { get; }

    /// <summary>
    /// 読み込んだシーンのAABB中心をモデル原点に合わせる位置補正を行うかどうか
    /// </summary>
    public bool AlignToAabbCenter { get; }

    /// <summary>
    /// ModelEntity の現在状態
    /// </summary>
    public ModelStatus Status { get; internal set; } = ModelStatus.Unloaded;

    /// <summary>
    /// この実体に対応する描画用ノード
    /// </summary>
    public ModelNode Node { get; internal set; } = null;

    /// <summary>
    /// 親実体を参照するためのプロパティ
    /// </summary>
    public ModelEntity Parent => ParentId != Guid.Empty ? Application.Model.Registry.GetEntity(ParentId) : null;

    /// <summary>
    /// 子実体の一覧を返す
    /// </summary>
    public IReadOnlyCollection<ModelEntity> Children => _children.Values;

    /// <summary>
    /// このモデル実体に直接紐づくルートプロパティの一覧を返す
    /// </summary>
    public IReadOnlyCollection<ModelProperty> Properties => _properties.Values;

    #endregion

    #region Constructors

    /// <summary>
    /// モデル実体を生成する
    /// </summary>
    public ModelEntity(
        Guid id,
        Guid parentId,
        string type,
        string name,
        Vector3 position,
        Quaternion rotation,
        ModelVisibility visibility,
        bool isCollapsed,
        string iconPath,
        string scenePath,
        bool alignToAabbCenter)
    {
        Id = id;
        ParentId = parentId;
        Type = type ?? string.Empty;
        Name = name ?? string.Empty;
        Position = position;
        Rotation = rotation;
        Visibility = visibility;
        IsCollapsed = isCollapsed;
        IconPath = iconPath ?? string.Empty;
        ScenePath = scenePath ?? string.Empty;
        AlignToAabbCenter = alignToAabbCenter;
    }

    /// <summary>
    /// 最小情報からモデル実体を生成する
    /// </summary>
    public ModelEntity(Guid id, Guid parentId, string name)
        : this(
            id,
            parentId,
            string.Empty,
            name,
            Vector3.Zero,
            Quaternion.Identity,
            ModelVisibility.Inherit,
            false,
            string.Empty,
            string.Empty,
            false)
    {
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 子モデル実体を登録する。同一 Id の重複登録は無視する。
    /// </summary>
    /// <param name="child">追加対象の子モデル実体</param>
    internal void AttachEntity(ModelEntity childEntity)
    {
        if (childEntity == null)
        {
            return;
        }

        if (_children.ContainsKey(childEntity.Id))
        {
            return;
        }

        _children.Add(childEntity.Id, childEntity);
    }

    /// <summary>
    /// 子モデル実体の登録を解除する
    /// </summary>
        /// <param name="childEntity">解除対象の子モデル実体</param>
    internal void DetachEntity(ModelEntity childEntity)
    {
        if (childEntity == null)
        {
            return;
        }

        _children.Remove(childEntity.Id);
    }

    /// <summary>
    /// プロパティをモデル実体に紐付ける
    /// </summary>
    /// <param name="property">追加対象のプロパティ</param>
    internal void AttachProperty(ModelProperty property)
    {
        if (property == null)
        {
            return;
        }

        if (_properties.ContainsKey(property.Id))
        {
            return;
        }

        _properties.Add(property.Id, property);
    }

    /// <summary>
    /// プロパティの紐付けを解除する
    /// </summary>
    /// <param name="property">解除対象のプロパティ</param>
    internal void DetachProperty(ModelProperty property)
    {
        if (property == null)
        {
            return;
        }

        _properties.Remove(property.Id);
    }

    /// <summary>
    /// 子モデル一覧およびプロパティ一覧をクリアする
    /// </summary>
    internal void Clear()
    {
        // TODO: 子モデルおよびプロパティの参照などを解除する処理が必要な場合はここに追加する
        _children.Clear();
        _properties.Clear();
    }

    #endregion
}