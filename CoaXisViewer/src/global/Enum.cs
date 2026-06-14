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
/// Viewportの操作モードを表す列挙型です。
/// </summary>
public enum ViewportInputMode
{
	/// <summary>操作待機状態。</summary>
	None,
	/// <summary>カメラの注視点の平行移動操作。</summary>
	CameraPan,
	/// <summary>カメラの注視点中心のオービット回転操作。</summary>
	CameraOrbit,
	/// <summary>カメラの視線方向を軸としたロール回転操作。</summary>
	CameraRoll,
	/// <summary>カメラのズーム操作。</summary>
	CameraZoom,
	/// <summary>選択矩形操作。</summary>
	SelectRect
}