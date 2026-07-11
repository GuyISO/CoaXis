using Godot;

/// <summary>
/// ビューポート関連のイベント集約ハブ
/// </summary>
public partial class ViewportEventHub : EventHubBase<ViewportEventHub>
{
    #region --------------------------------------- Action ---------------------------------------

    [Signal] public delegate void AskStateRequestedEventHandler();
    /// <summary>
    /// ビューポート関連の状態の通知をリクエストする
    /// </summary>
    internal void AskState()
    {
        Emit(SignalName.AskStateRequested);
    }

    [Signal] public delegate void MovePositionToRequestedEventHandler(Vector3 position, bool useTween);
    /// <summary>
    /// 注視点の位置の設定をリクエストする
    /// </summary>
    /// <param name="position">設定する注視点の位置</param>
    /// <param name="useTween">移動にトゥイーンを使用するかどうかのフラグデフォルトは false </param>
    /// <remarks>SetPositionという名前にしたいところが、Node3Dの標準メソッドと区別するためにMovePositionToという名前にしている</remarks>
    internal void MovePositionTo(Vector3 position, bool useTween = false)
    {
        Emit(SignalName.MovePositionToRequested, position, useTween);
    }

    [Signal] public delegate void MoveRotationToRequestedEventHandler(Quaternion rotation, bool useTween);
    /// <summary>
    /// 注視点の回転の設定をリクエストする
    /// </summary>
    /// <param name="rotation">設定する注視点の回転</param>
    /// <param name="useTween">移動にトゥイーンを使用するかどうかのフラグ、デフォルトは false </param>
    /// <remarks>SetRotationという名前にしたいところが、Node3Dの標準メソッドと区別するためにMoveRotationToという名前にしている</remarks>
    internal void MoveRotationTo(Quaternion rotation, bool useTween = false)
    {
        Emit(SignalName.MoveRotationToRequested, rotation, useTween);
    }

    [Signal] public delegate void SetDistanceRequestedEventHandler(float distance, bool useTween);
    /// <summary>
    /// カメラの距離の設定をリクエストする
    /// </summary>
    /// <param name="distance">設定するカメラの距離、透視投影の場合のみ有効</param>
    /// <param name="useTween">移動にトゥイーンを使用するかどうかのフラグ、デフォルトは false </param>
    internal void SetDistance(float distance, bool useTween = false)
    {
        Emit(SignalName.SetDistanceRequested, distance, useTween);
    }

    [Signal] public delegate void SetSizeRequestedEventHandler(float size, bool useTween);
    /// <summary>
    /// カメラのサイズの設定をリクエストする
    /// </summary>
    /// <param name="size">設定するカメラのサイズ、平行投影の場合のみ有効</param>
    /// <param name="useTween">サイズ変更にトゥイーンを使用するかどうかのフラグ、デフォルトは false </param>
    internal void SetSizeTo(float size, bool useTween = false)
    {
        Emit(SignalName.SetSizeRequested, size, useTween);
    }

    [Signal] public delegate void SetFovRequestedEventHandler(float fov, bool useTween);
    /// <summary>
    /// FOV（視野角）の設定をリクエストする
    /// </summary>
    /// <param name="fov">設定する FOV の値</param>
    /// <param name="useTween">FOV変更にトゥイーンを使用するかどうかのフラグ、デフォルトは false </param>
    internal void SetFov(float fov, bool useTween = false)
    {
        Emit(SignalName.SetFovRequested, fov, useTween);
    }

    [Signal] public delegate void TranslateRequestedEventHandler(Vector3 translation, SpaceMode spaceMode, bool useTween);
    /// <summary>
    /// カメラの平行移動をリクエストする
    /// </summary>
    /// <param name="translation">カメラの平行移動量</param>
    /// <param name="spaceMode">平行移動の基準となる座標系</param>
    /// <param name="useTween">移動にトゥイーンを使用するかどうかのフラグ、デフォルトは false </param>
    internal void Translate(Vector3 translation, SpaceMode spaceMode = SpaceMode.World, bool useTween = false)
    {
        Emit(SignalName.TranslateRequested, translation, (int)spaceMode, useTween);
    }

    [Signal] public delegate void RotateRequestedEventHandler(Quaternion rotation, SpaceMode spaceMode, bool useTween);
    /// <summary>
    /// カメラの回転をリクエストする
    /// </summary>
    /// <param name="rotation">設定するカメラの回転</param>
    /// <param name="spaceMode">回転の基準となる座標系</param>
    /// <param name="useTween">回転にトゥイーンを使用するかどうかのフラグ、デフォルトは false </param>
    internal void Rotate(Quaternion rotation, SpaceMode spaceMode = SpaceMode.World, bool useTween = false)
    {
        Emit(SignalName.RotateRequested, rotation, (int)spaceMode, useTween);
    }

    [Signal] public delegate void ZoomRequestedEventHandler(float exponent, bool useTween);
    /// <summary>
    /// カメラのズームをリクエストする
    /// </summary>
    /// <param name="exponent">ズームの指数値で、1 より大きい値はズームインかつ 1 より小さい値はズームアウトを意味する</param>
    /// <param name="useTween">ズームにトゥイーンを使用するかどうかのフラグ、デフォルトは false </param>
    internal void Zoom(float exponent, bool useTween = false)
    {
        Emit(SignalName.ZoomRequested, exponent, useTween);
    }

    [Signal] public delegate void SetProjectionTypeRequestedEventHandler(Camera3D.ProjectionType type);
    /// <summary>
    /// 投影タイプの設定をリクエストする
    /// </summary>
    /// <param name="type">設定する投影タイプ</param>
    internal void SetProjectionType(Camera3D.ProjectionType type)
    {
        Emit(SignalName.SetProjectionTypeRequested, (int)type);
    }

    [Signal] public delegate void ToggleProjectionTypeRequestedEventHandler();
    /// <summary>
    /// 投影タイプの切り替えをリクエストする
    /// </summary>
    internal void ToggleProjectionType()
    {
        Emit(SignalName.ToggleProjectionTypeRequested);
    }

    [Signal] public delegate void FitRequestedEventHandler(AnyModel[] targetModels, bool useTween);
    /// <summary>
    /// カメラを指定モデル群全体にフィットさせる操作をリクエストする
    /// </summary>
    /// <param name="targetModels">フィットさせたいターゲットモデル群</param>
    /// <param name="useTween">フィット操作にトゥイーンを使用するかどうかのフラグ、デフォルトは false </param>
    internal void Fit(AnyModel[] targetModels, bool useTween = false)
    {
        Emit(SignalName.FitRequested, targetModels, useTween);
    }

    [Signal] public delegate void AlignNormalToRequestedEventHandler(Vector3 normal, bool useTween);
    /// <summary>
    /// カメラの向きを指定した法線に合わせる操作をリクエストする
    /// </summary>
    /// <param name="normal">カメラの向きを合わせたい法線ベクトル</param>
    /// <param name="useTween">回転にトゥイーンを使用するかどうかのフラグ、デフォルトは false </param>
    internal void AlignNormalTo(Vector3 normal, bool useTween = false)
    {
        Emit(SignalName.AlignNormalToRequested, normal, useTween);
    }

    [Signal] public delegate void DecidePickRectRequestedEventHandler(Vector2 startPosition, Vector2 endPosition);
    /// <summary>
    /// 矩形選択の確定を通知するシグナル
    /// </summary>
    /// <param name="startPosition">矩形選択の開始位置</param>
    /// <param name="endPosition">矩形選択の終了位置</param>
    internal void DecidePickRect(Vector2 startPosition, Vector2 endPosition)
    {
        Emit(SignalName.DecidePickRectRequested, startPosition, endPosition);
    }

    #endregion

    #region --------------------------------------- Notification ---------------------------------------

    [Signal] public delegate void InteractionModeNotifiedEventHandler(ViewportInteractionMode mode);
    /// <summary>
    /// 操作モードを通知するシグナル
    /// </summary>
    /// <param name="mode">操作モード</param>
    internal void NotifyInteractionMode(ViewportInteractionMode mode)
    {
        Emit(SignalName.InteractionModeNotified, (int)mode);
    }

    [Signal] public delegate void PositionNotifiedEventHandler(Vector3 position);
    /// <summary>
    /// 注視点の位置を通知するシグナル
    /// </summary>
    /// <param name="position">注視点の位置</param>
    internal void NotifyPosition(Vector3 position)
    {
        Emit(SignalName.PositionNotified, position);
    }

    [Signal] public delegate void RotationNotifiedEventHandler(Quaternion rotation);
    /// <summary>
    /// 注視点の回転を通知するシグナル
    /// </summary>
    /// <param name="rotation">注視点の回転</param>
    internal void NotifyRotation(Quaternion rotation)
    {
        Emit(SignalName.RotationNotified, rotation);
    }

    [Signal] public delegate void DistanceNotifiedEventHandler(float distance);
    /// <summary>
    /// カメラの距離を通知するシグナル
    /// </summary>
    /// <param name="distance">カメラの距離、透視投影の場合のみ有効</param>
    internal void NotifyDistance(float distance)
    {
        Emit(SignalName.DistanceNotified, distance);
    }

    [Signal] public delegate void SizeNotifiedEventHandler(float size);
    /// <summary>
    /// カメラのズームレベルを通知するシグナル
    /// </summary>
    /// <param name="size">カメラのサイズ（ズームレベル）、平行投影の場合のみ有効</param>
    internal void NotifySize(float size)
    {
        Emit(SignalName.SizeNotified, size);
    }

    [Signal] public delegate void FovNotifiedEventHandler(float fov);
    /// <summary>
    /// カメラの FOV（視野角）を通知するシグナル
    /// </summary>
    /// <param name="fov">FOV の値</param>
    internal void NotifyFov(float fov)
    {
        Emit(SignalName.FovNotified, fov);
    }

    [Signal] public delegate void ProjectionTypeNotifiedEventHandler(Camera3D.ProjectionType type);
    /// <summary>
    /// カメラの投影タイプを通知するシグナル
    /// </summary>
    /// <param name="type">投影タイプ</param>
    internal void NotifyProjectionType(Camera3D.ProjectionType type)
    {
        Emit(SignalName.ProjectionTypeNotified, (int)type);
    }

    [Signal] public delegate void ArcballRadiusNotifiedEventHandler(float radius);
    /// <summary>
    /// アークボールの半径を通知するシグナル
    /// </summary>
    /// <param name="radius">アークボールの半径</param>
    internal void NotifyArcballRadius(float radius)
    {
        Emit(SignalName.ArcballRadiusNotified, radius);
    }

    [Signal] public delegate void ArcballHandleNotifiedEventHandler(Vector3 position);
    /// <summary>
    /// アークボール操作の操作点を通知するシグナル
    /// </summary>
    /// <param name="position">アークボールの操作点の位置</param>
    internal void NotifyArcballHandle(Vector3 position)
    {
        Emit(SignalName.ArcballHandleNotified, position);
    }

    [Signal] public delegate void PickRectNotifiedEventHandler(Vector2 startPosition, Vector2 endPosition);
    /// <summary>
    /// 矩形選択の範囲を通知するシグナル
    /// </summary>
    /// <param name="startPosition">矩形選択の開始位置</param>
    /// <param name="endPosition">矩形選択の終了位置</param>
    internal void NotifyPickRect(Vector2 startPosition, Vector2 endPosition)
    {
        Emit(SignalName.PickRectNotified, startPosition, endPosition);
    }

    [Signal] public delegate void PickResultNotifiedEventHandler(PickResult pickResult);
    /// <summary>
    /// 測定用の点選択結果を通知するシグナル
    /// </summary>
    /// <param name="pickResult">選択結果の情報を含むオブジェクト</param>
    internal void NotifyPickResult(PickResult pickResult)
    {
        Emit(SignalName.PickResultNotified, pickResult);
    }

    #endregion
}