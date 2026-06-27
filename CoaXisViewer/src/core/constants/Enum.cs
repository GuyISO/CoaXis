using Godot;
using System;

/// <summary>
/// ログレベルを表す列挙型
/// </summary>
public enum LogLevel
{
    Debug,
    Info,
    Warn,
    Error
}

/// <summary>
/// 座標系の基準を表す列挙型
/// </summary>
public enum SpaceMode
{
    World,
    FocalPoint,
    Camera
}

/// <summary>
/// HierarchyTree の列を表す列挙型
/// </summary>
public enum HierarchyTreeColumn
{
    Name,
    VisibleButton,
}

/// <summary>
/// Viewportのレイヤーを表す列挙型、Raycast用のビットマスクとしても使用される
/// </summary>
public enum ViewportLayer
{
    None = 0b_0000_0000_0000_0000_0000,
    Default = 0b_0000_0000_0000_0000_0001,
    AxisNavigator = 0b_0000_0000_0000_0000_0010,
}

/// <summary>
/// Viewportの操作モードを表す列挙型
/// </summary>
public enum ViewportInputMode
{
    /// <summary>操作待機状態</summary>
    None,
    /// <summary>カメラの注視点の平行移動操作</summary>
    CameraPan,
    /// <summary>カメラの注視点中心のオービット回転操作</summary>
    CameraOrbit,
    /// <summary>カメラの視線方向を軸としたロール回転操作</summary>
    CameraRoll,
    /// <summary>カメラのズーム操作</summary>
    CameraZoom,
    /// <summary>選択矩形操作</summary>
    SelectionRect
}