using Godot;
using System;

/// <summary>
/// ログレベルを表す列挙型
/// </summary>
public enum LogLevel
{
    /// <summary>開発中のデバッグ用</summary>
    Debug,
    /// <summary>ユーザーに通知する必要がある情報</summary>
    Info,
    /// <summary>警告を表す</summary>
    Warn,
    /// <summary>エラーを表す</summary>
    Error,
}

/// <summary>
/// 座標系の基準を表す列挙型
/// </summary>
public enum SpaceMode
{
    /// <summary>ワールド座標系</summary>
    World,
    /// <summary>注視点基準の座標系</summary>
    FocalPoint,
    /// <summary>カメラ基準の座標系</summary>
    Camera,
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
/// モデルの選択モードを表す列挙型
/// </summary>
public enum SelectionMode
{
    /// <summary>対象のみを選択するデフォルトの選択モード</summary>
    Set,
    /// <summary>追加選択モード</summary>
    Add,
    /// <summary>削除選択モード</summary>
    Remove,
    /// <summary>トグル選択モード</summary>
    Toggle,
}

/// <summary>
/// Viewportの操作モードを表す列挙型
/// </summary>
public enum ViewportInteractionMode
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
    PickRect,
}

/// <summary>
/// 選択されたモデルの操作モードを表す列挙型
/// </summary>
public enum PickHandlingMode
{
    /// <summary>選択操作モード</summary>
    Selection,
    /// <summary>測定操作モード</summary>
    Measurement,
    /// <summary>面に垂直操作モード</summary>
    NormalToFace,
}