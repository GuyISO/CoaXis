using Godot;
using System;

/// <summary>
/// ログレベルを表す列挙型です。
/// </summary>
public enum LogLevel
{
    Debug,
    Info,
    Warn,
    Error
}

/// <summary>
/// 基準となる座標系を表す列挙型です。
/// </summary>
public enum SpaceMode
{
	World,
	FocalPoint,
	Camera
}

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