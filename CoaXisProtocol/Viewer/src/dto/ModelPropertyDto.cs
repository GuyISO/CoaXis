using System;

namespace CoaXis.Protocol.Viewer;

/// <summary>
/// モデルの属性情報（プロパティ）を表す DTO クラス
/// </summary>
public class ModelPropertyDto : BaseDto
{
    /// <summary>
    /// プロパティの一意識別子
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// 親プロパティまたは所属先モデルの識別子。親が存在しない場合は null
    /// </summary>
    public Guid? ParentId { get; init; } = null;

    /// <summary>
    /// プロパティの種類識別（元の Parameter 名など）
    /// </summary>
    public string PropertyType { get; init; } = string.Empty;

    /// <summary>
    /// 値の型 ("int", "float", "string", "bool", "datetime", "Vector3", "Quaternion", "Color", "Transform")
    /// </summary>
    public string ValueType { get; init; } = string.Empty;

    /// <summary>
    /// 文字列化された値
    /// </summary>
    public string Value { get; init; } = string.Empty;
}
