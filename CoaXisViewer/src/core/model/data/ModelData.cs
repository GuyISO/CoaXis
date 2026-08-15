using System;
using Godot;
using System.Collections.Generic;

/// <summary>
/// モデルのデータを表すクラス、ModelFactoryで生成され、ModelNodeに対応する
/// </summary>
public class ModelData
{
    #region Fields

    /// <summary>
    /// 子モデルをNameで管理するための保持先
    /// </summary>
    private readonly Dictionary<string, ModelData> _children = new Dictionary<string, ModelData>();

    #endregion

    #region Properties

    /// <summary>
    /// モデルの一意識別子
    /// </summary>
    public Guid Id { get; }
    /// <summary>
    /// 親モデルの識別子。Root 配下の場合は Guid.Empty
    /// </summary>
    public Guid ParentId { get; }
    /// <summary>
    /// CSV などから受け取ったモデル種別
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
    /// アイコン画像のパス
    /// </summary>
    public string IconPath { get; }
    /// <summary>
    /// GLB モデルのパス
    /// </summary>
    public string GlbPath { get; }
    /// <summary>
    /// WRL モデルのパス
    /// </summary>
    public string WrlPath { get; }

    /// <summary>
    /// ModelData の現在状態
    /// </summary>
    public ModelStatus Status { get; internal set; } = ModelStatus.Unloaded;
    /// <summary>
    /// このデータに対応する実体ノード
    /// </summary>
    public ModelNode Node { get; internal set; } = null;

    /// <summary>
    /// 親データを参照するためのプロパティ
    /// </summary>
    public ModelData Parent => ParentId != Guid.Empty ? Application.Model.Registry.GetModelData(ParentId) : null;
    /// <summary>
    /// 子データの一覧を返す
    /// </summary>
    public IReadOnlyCollection<ModelData> Children => _children.Values;

    #endregion

    #region Constructors

    /// <summary>
    /// モデルデータを生成する
    /// </summary>
    public ModelData(
        Guid id,
        Guid parentId,
        string type,
        string name,
        Vector3 position,
        Quaternion rotation,
        ModelVisibility visibility,
        string iconPath,
        string glbPath,
        string wrlPath)
    {
        Id = id;
        ParentId = parentId;
        Type = type ?? string.Empty;
        Name = name ?? string.Empty;
        Position = position;
        Rotation = rotation;
        Visibility = visibility;
        IconPath = iconPath ?? string.Empty;
        GlbPath = glbPath ?? string.Empty;
        WrlPath = wrlPath ?? string.Empty;
    }

    /// <summary>
    /// 最小情報からモデルデータを生成する
    /// </summary>
    public ModelData(Guid id, Guid parentId, string name)
        : this(
            id,
            parentId,
            string.Empty,
            name,
            Vector3.Zero,
            Quaternion.Identity,
            ModelVisibility.Inherit,
            string.Empty,
            string.Empty,
            string.Empty)
    {
    }
    #endregion

    #region Public Methods

    /// <summary>
    /// 子モデルを登録する。重複名は無視する。
    /// </summary>
    /// <param name="child">追加対象の子モデル</param>
    internal void Attach(ModelData child)
    {
        if (child == null)
        {
            return;
        }

        if (_children.ContainsKey(child.Name))
        {
            return;
        }

        _children.Add(child.Name, child);
    }

    /// <summary>
    /// 子モデルの登録を解除する
    /// </summary>
    /// <param name="child">解除対象の子モデル</param>
    internal void Detach(ModelData child)
    {
        if (child == null)
        {
            return;
        }

        _children.Remove(child.Name);
    }

    /// <summary>
    /// 子モデル一覧をクリアする
    /// </summary>
    internal void Clear()
    {
        _children.Clear();
    }

    #endregion
}
