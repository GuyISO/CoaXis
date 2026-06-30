using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// レイキャストや範囲選択で空間上から取得した情報を表す構造体
/// </summary>
public struct PickResult
{
    /// <summary>
    /// 取得に成功したかどうかを示す
    /// </summary>
    public bool HasHit;
    /// <summary>
    /// 取得したコライダーノードへの参照
    /// </summary>
    public Node3D Collider;
    /// <summary>
    /// 取得したコライダーオブジェクトのRID
    /// </summary>
    public Rid Rid;
    /// <summary>
    /// 取得したコライダーの親ノードへの参照で、本プロジェクトではコライダーを AnyModel の子ノードとして配置するためその参照を取得しやすくしている
    /// </summary>
    public AnyModel Model;
    /// <summary>
    /// 取得した位置のワールド座標で、レイキャスト時のみ有効で範囲選択では取得されない
    /// </summary>
    public Vector3 Position;
    /// <summary>
    /// 取得した面の法線ベクトルで、レイキャスト時のみ有効で範囲選択では取得されない
    /// </summary>
    public Vector3 Normal;
    /// <summary>
    /// 取得した位置までの距離で、レイキャスト時のみ有効で範囲選択では取得されない
    /// </summary>
    public float Distance;
}