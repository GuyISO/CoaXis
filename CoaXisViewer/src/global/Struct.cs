using Godot;
using System;

/// <summary>
/// レイキャストのヒット情報を表す構造体です。
/// </summary>
public struct RaycastHitInfo
{
	/// <summary>
	/// レイがヒットしたかどうかを示します。
	/// </summary>
    public bool HasHit;
	/// <summary>
	/// レイがヒットした位置のワールド座標です。
	/// </summary>
    public Vector3 Position;
	/// <summary>
	/// レイがヒットした面の法線ベクトルです。
	/// </summary>
    public Vector3 Normal;
	/// <summary>
	/// レイがヒットしたコライダーノードへの参照です。
	/// </summary>
    public Node3D Collider;
    /// <summary>
    /// レイがヒットした距離です。
    /// </summary>
    public float Distance;
	 /// <summary>
	/// レイがヒットしたオブジェクトのRIDです。
	/// </summary>
	public Rid Rid;
}