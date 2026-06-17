using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// レイキャストなどで空間上から取得した情報を表す構造体です。
/// </summary>
public struct PickResult
{
	/// <summary>
	/// 取得に成功したかどうかを示します。
	/// </summary>
    public bool HasHit;
	/// <summary>
	/// 取得したコライダーノードへの参照です。
	/// </summary>
    public Node3D Collider;
	 /// <summary>
	/// 取得したコライダーオブジェクトのRIDです。
	/// </summary>
	public Rid Rid;
	/// <summary>
	/// 取得したコライダーの親ノードへの参照です。本プロジェクトではコライダーはNode3Dの子ノードとして配置され、実態としてその親ノードを扱うことが多いため、親ノードも取得できるようにしています。
	/// </summary>
	public Node3D Node;
	/// <summary>
	/// 取得した位置のワールド座標です。レイキャストの場合のみ有効で、範囲選択などでは取得されません。
	/// </summary>
    public Vector3 Position;
	/// <summary>
	/// 取得した面の法線ベクトルです。レイキャストの場合のみ有効で、範囲選択などでは取得されません。
	/// </summary>
    public Vector3 Normal;
    /// <summary>
    /// 取得した位置までの距離です。レイキャストの場合のみ有効で、範囲選択などでは取得されません。
    /// </summary>
    public float Distance;
}