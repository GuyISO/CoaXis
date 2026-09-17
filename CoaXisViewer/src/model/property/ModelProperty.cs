using System;
using System.Collections.Generic;

/// <summary>
/// モデルに付与される属性情報を表すクラス。
/// 木構造で管理され、ModelRegistry によって一元管理される。
/// </summary>
public class ModelProperty
{
    #region Fields

    /// <summary>
    /// 子プロパティを保持するための辞書
    /// </summary>
    private readonly Dictionary<Guid, ModelProperty> _children = new();

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
    /// 親プロパティを取得する。親がモデル実体（ルートプロパティ）または未登録の場合は null を返す
    /// </summary>
    public ModelProperty ParentProperty => ParentId != Guid.Empty ? Application.Model.Registry.GetProperty(ParentId) : null;

    /// <summary>
    /// 親プロパティの参照（ParentProperty のエイリアス）
    /// </summary>
    public ModelProperty Parent => ParentProperty;

    /// <summary>
    /// 親がモデル実体である場合に、その親モデル実体を取得する。親がプロパティまたは未登録の場合は null を返す
    /// </summary>
    public ModelEntity ParentEntity => ParentId != Guid.Empty ? Application.Model.Registry.GetEntity(ParentId) : null;

    /// <summary>
    /// このプロパティがモデル実体に直接紐づくルートプロパティかどうかを判定する
    /// </summary>
    public bool IsRootProperty => ParentEntity != null;

    /// <summary>
    /// 子プロパティの一覧を返す
    /// </summary>
    public IReadOnlyCollection<ModelProperty> Children => _children.Values;

    #endregion

    #region Constructors

    /// <summary>
    /// プロパティデータを生成する
    /// </summary>
    /// <param name="id">プロパティの一意識別子</param>
    /// <param name="parentId">親プロパティまたは所属先モデル実体の識別子</param>
    /// <param name="propertyType">プロパティの種類識別</param>
    /// <param name="valueType">値の型名</param>
    /// <param name="value">文字列化された値</param>
    public ModelProperty(
        Guid id,
        Guid parentId,
        string propertyType,
        string valueType,
        string value)
    {
        Id = id;
        ParentId = parentId;
        PropertyType = propertyType ?? string.Empty;
        ValueType = valueType ?? string.Empty;
        Value = value ?? string.Empty;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// このプロパティが所属する ModelEntity を再帰的に探索して取得する。
    /// </summary>
    /// <returns>所属する ModelEntity。見つからない場合は null</returns>
    public ModelEntity GetEntity()
    {
        return Application.Model.Registry.GetOwningEntity(Id);
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
    /// 子プロパティを登録する。同一 Id の重複追加は無視する。
    /// </summary>
    /// <param name="child">追加対象の子プロパティ</param>
    internal void Attach(ModelProperty child)
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
    internal void Detach(ModelProperty child)
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
    internal void Clear()
    {
        _children.Clear();
    }

    #endregion
}
