using Godot;
using System;

/// <summary>
/// カメラの入力制御モードです。
/// </summary>
public enum CameraControlMode
{
	/// <summary>操作待機状態。</summary>
	None,
	/// <summary>注視点の平行移動操作。</summary>
	Pan,
	/// <summary>注視点中心のオービット回転操作。</summary>
	Orbit,
	/// <summary>視線方向を軸としたロール回転操作。</summary>
	Roll,
	/// <summary>ズーム操作。</summary>
	Zoom
}





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
}