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

    /// <summary>アークボールの補助表示の現在回転を取得する。</summary>
    internal Quaternion ArcballHandleRotation { get; private set; } = Quaternion.Identity;

    /// <summary>矩形選択の開始位置を取得する。</summary>
    internal Vector2 PickRectStart { get; private set; }

    /// <summary>矩形選択の終了位置を取得する。</summary>
    internal Vector2 PickRectEnd { get; private set; }

    #endregion

    #region Lifecycle

    #endregion

    #region Events

    /// <summary>操作モードの変更通知。</summary>
    [Signal] public delegate void ModeNotifiedEventHandler();

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
        EmitSignal(SignalName.ModeNotified);
    }

    /// <summary>アークボール半径の変更通知。</summary>
    [Signal] public delegate void ArcballRadiusNotifiedEventHandler();

    /// <summary>アークボール半径を更新して通知する。</summary>
    /// <param name="radius">新しいアークボール半径</param>
    internal void SetArcballRadius(float radius)
    {
        ArcballRadius = radius;
        EmitSignal(SignalName.ArcballRadiusNotified);
    }

    /// <summary>アークボール補助表示の回転変更通知。</summary>
    [Signal] public delegate void ArcballHandleRotationNotifiedEventHandler();

    /// <summary>操作点からアークボール補助表示の初期回転を設定して通知する。</summary>
    /// <param name="position">球面上の操作点</param>
    internal void SetArcballHandle(Vector3 position)
    {
        ArcballHandleRotation = CalculateArcballHandleRotation(position);
        EmitSignal(SignalName.ArcballHandleRotationNotified);
    }

    /// <summary>カメラ回転に追従するアークボール補助表示の回転を更新して通知する。</summary>
    /// <param name="rotation">カメラへ適用した回転量</param>
    internal void RotateArcball(Quaternion rotation)
    {
        // 補助表示はカメラの回転と逆向きに追従させる。
        ArcballHandleRotation = rotation.Inverse() * ArcballHandleRotation;
        EmitSignal(SignalName.ArcballHandleRotationNotified);
    }

    /// <summary>矩形選択範囲の変更通知。</summary>
    [Signal] public delegate void PickRectNotifiedEventHandler();

    /// <summary>矩形選択範囲を更新して通知する。</summary>
    /// <param name="startPosition">範囲の開始位置</param>
    /// <param name="endPosition">範囲の終了位置</param>
    internal void SetPickRect(Vector2 startPosition, Vector2 endPosition)
    {
        PickRectStart = startPosition;
        PickRectEnd = endPosition;
        EmitSignal(SignalName.PickRectNotified);
    }

    #endregion

    #region Methods

    #endregion

    #region Helpers

    private static Quaternion CalculateArcballHandleRotation(Vector3 handlePosition)
    {
        if (handlePosition.LengthSquared() <= Mathf.Epsilon * Mathf.Epsilon)
        {
            return Quaternion.Identity;
        }

        Vector3 anchor = handlePosition.Normalized();

        // 画面投影で中心方向（-x, -y）を向く接線を作る。
        Vector3 desiredTowardCenter = new Vector3(-anchor.X, -anchor.Y, 0.0f);
        Vector3 tangentX = desiredTowardCenter - anchor * desiredTowardCenter.Dot(anchor);
        if (tangentX.LengthSquared() <= Mathf.Epsilon * Mathf.Epsilon)
        {
            Vector3 fallback = Vector3.Right - anchor * Vector3.Right.Dot(anchor);
            if (fallback.LengthSquared() <= Mathf.Epsilon * Mathf.Epsilon)
            {
                fallback = Vector3.Up - anchor * Vector3.Up.Dot(anchor);
            }

            tangentX = fallback;
        }

        tangentX = tangentX.Normalized();
        Vector3 zAxis = -anchor;
        Vector3 yAxis = zAxis.Cross(tangentX).Normalized();
        if (yAxis.LengthSquared() <= Mathf.Epsilon * Mathf.Epsilon)
        {
            yAxis = Vector3.Up;
        }

        Vector3 xAxis = yAxis.Cross(zAxis).Normalized();
        return new Basis(xAxis, yAxis, zAxis).Orthonormalized().GetRotationQuaternion();
    }

    #endregion
}
