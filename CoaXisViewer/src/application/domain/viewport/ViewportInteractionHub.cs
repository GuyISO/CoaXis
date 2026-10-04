// TODO: リファクタリング確認後に削除

using Godot;

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
/// Viewportの操作モードと操作補助表示状態を保持し、変更を通知する。
/// </summary>
public partial class ViewportInteractionHub : BaseHub
{
    #region Fields

    #endregion

    #region Properties

    /// <summary>現在のビューポート操作モードを取得する。</summary>
    internal ViewportInteractionMode Mode { get; private set; } = ViewportInteractionMode.None;

    /// <summary>アークボールの操作半径を取得する。</summary>
    internal float ArcballRadius { get; private set; }

    /// <summary>アークボールの現在の操作点を取得する。</summary>
    internal Vector3 ArcballHandle { get; private set; } = new(0, 0, 1);

    /// <summary>矩形選択の開始位置を取得する。</summary>
    internal Vector2 PickRectStart { get; private set; }

    /// <summary>矩形選択の終了位置を取得する。</summary>
    internal Vector2 PickRectEnd { get; private set; }

    #endregion

    #region Lifecycle

    #endregion

    #region Actions

    #endregion

    #region Notifications

    /// <summary>操作モードの変更通知。</summary>
    /// <param name="mode">現在の操作モード</param>
    [Signal] public delegate void ModeNotifiedEventHandler(ViewportInteractionMode mode);

    /// <summary>
    /// 操作モードを更新し、変更された場合に通知する。
    /// </summary>
    /// <param name="mode">新しい操作モード</param>
    internal void SetMode(ViewportInteractionMode mode)
    {
        if (Mode == mode)
        {
            return;
        }

        Mode = mode;
        EmitSignal(SignalName.ModeNotified, (int)mode);
    }

    /// <summary>アークボール半径の変更通知。</summary>
    /// <param name="radius">現在のアークボール半径</param>
    [Signal] public delegate void ArcballRadiusNotifiedEventHandler(float radius);

    /// <summary>アークボール半径を更新して通知する。</summary>
    /// <param name="radius">新しいアークボール半径</param>
    internal void SetArcballRadius(float radius)
    {
        ArcballRadius = radius;
        EmitSignal(SignalName.ArcballRadiusNotified, radius);
    }

    /// <summary>アークボール操作点の変更通知。</summary>
    /// <param name="position">現在の操作点</param>
    [Signal] public delegate void ArcballHandleNotifiedEventHandler(Vector3 position);

    /// <summary>アークボール操作点を更新して通知する。</summary>
    /// <param name="position">新しい操作点</param>
    internal void SetArcballHandle(Vector3 position)
    {
        ArcballHandle = position;
        EmitSignal(SignalName.ArcballHandleNotified, position);
    }

    /// <summary>矩形選択範囲の変更通知。</summary>
    /// <param name="startPosition">範囲の開始位置</param>
    /// <param name="endPosition">範囲の終了位置</param>
    [Signal] public delegate void PickRectNotifiedEventHandler(Vector2 startPosition, Vector2 endPosition);

    /// <summary>矩形選択範囲を更新して通知する。</summary>
    /// <param name="startPosition">範囲の開始位置</param>
    /// <param name="endPosition">範囲の終了位置</param>
    internal void SetPickRect(Vector2 startPosition, Vector2 endPosition)
    {
        PickRectStart = startPosition;
        PickRectEnd = endPosition;
        EmitSignal(SignalName.PickRectNotified, startPosition, endPosition);
    }

    #endregion
}
