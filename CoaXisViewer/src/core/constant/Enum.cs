// TODO: リファクタリング確認後に削除
using Godot;
using System;

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
/// Viewportのレイヤーを表す列挙型、Raycast用のビットマスクとしても使用される
/// </summary>
public enum ViewportLayer
{
    None = 0b_0000_0000_0000_0000_0000,
    Default = 0b_0000_0000_0000_0000_0001,
    AxisNavigator = 0b_0000_0000_0000_0000_0010,
    Visible = 0b_0000_0000_0000_0000_0100,
    Invisible = 0b_0000_0000_0000_0000_1000,
}

/// <summary>
/// モデルの状態を表す列挙型
/// </summary>
public enum ModelStatus
{
    /// <summary> モデルがまだロードされていない状態、初期値 </summary>
    Unloaded,
    /// <summary> モデルが初期化された状態 </summary>
    Initialized,
    /// <summary> モデルがレジストリに登録された状態 </summary>
    Registered,
    /// <summary> モデルの読み込み処理中の状態 </summary>
    Loading,
    /// <summary> モデルのロードが完了した状態 </summary>
    Loaded,
    /// <summary> モデルのロードが失敗した状態 </summary>
    LoadFailed,
    /// <summary> モデルが破棄された状態 </summary>
    Disposed,
}

public enum ModelVisibility
{
    /// <summary> 上位モデルを継承、初期値 </summary>
    Inherit,
    /// <summary> モデルが表示 </summary>
    Visible,
    /// <summary> モデルが非表示 </summary>
    Invisible,
}