using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// カメラの位置と向きを制御し、イベントハブを通じて他コンポーネントと連携する
/// このクラスをアタッチした <see cref="Node3D"/> を注視点の基準ノードとして扱う
/// </summary>

public partial class CameraRig : Node3D
{
    #region Fields

    [ExportGroup("Settings")]
    [Export] private float _zoomBase = 1.005f; // ズーム倍率変更時の底、exponent1あたりの拡大倍率
    [Export] private float _minZoomValue = 0.01f; // ズームの最小値、これ以上近づけないようにするための制限値
    [Export] private float _fitPadding = 1.1f; // Fit All In 時に、対象が画面にぴったり収まるようにするための余白倍率
    [Export] private float _tweenDuration = 0.5f; // Tween を使用する場合のアニメーション時間（秒）

    private Camera3D _camera; // 操作対象のカメラノード

    private PickHandlingMode _currentPickHandlingMode; // 現在の選択操作モード
    private bool _isInitialized = false; // 初期化済みかどうかのフラグ、初回通知を受け取った時点で true にする

    #endregion

    #region Lifecycle

    public override void _Ready()
    {
        // 関連ノードのキャッシュ
        _camera = GetNode<Camera3D>("Camera3D");

        // イベント購読の登録
        PickEventHub.Instance.PickHandlingModeNotified += OnPickHandlingModeNotified;
        PickEventHub.Instance.PickResultNotified += OnPickResultNotified;
        ViewportEventHub.Instance.NotifyStateRequested += OnNotifyStateRequested;
        ViewportEventHub.Instance.MovePositionToRequested += OnMovePositionToRequested;
        ViewportEventHub.Instance.MoveRotationToRequested += OnMoveRotationToRequested;
        ViewportEventHub.Instance.SetSizeRequested += OnSetSizeRequested;
        ViewportEventHub.Instance.SetDistanceRequested += OnSetDistanceRequested;
        ViewportEventHub.Instance.SetFovRequested += OnSetFovRequested;
        ViewportEventHub.Instance.SetProjectionTypeRequested += OnSetProjectionTypeRequested;
        ViewportEventHub.Instance.TranslateRequested += OnTranslateRequested;
        ViewportEventHub.Instance.RotateRequested += OnRotateRequested;
        ViewportEventHub.Instance.ZoomRequested += OnZoomRequested;
        ViewportEventHub.Instance.ToggleProjectionTypeRequested += OnToggleProjectionTypeRequested;
        ViewportEventHub.Instance.FitRequested += OnFitRequested;
        ViewportEventHub.Instance.AlignNormalToRequested += OnAlignNormalToRequested;
    }

    public override void _ExitTree()
    {
        // イベント購読の解除
        PickEventHub.Instance.PickHandlingModeNotified -= OnPickHandlingModeNotified;
        PickEventHub.Instance.PickResultNotified -= OnPickResultNotified;
        ViewportEventHub.Instance.NotifyStateRequested -= OnNotifyStateRequested;
        ViewportEventHub.Instance.MovePositionToRequested -= OnMovePositionToRequested;
        ViewportEventHub.Instance.MoveRotationToRequested -= OnMoveRotationToRequested;
        ViewportEventHub.Instance.SetSizeRequested -= OnSetSizeRequested;
        ViewportEventHub.Instance.SetDistanceRequested -= OnSetDistanceRequested;
        ViewportEventHub.Instance.SetFovRequested -= OnSetFovRequested;
        ViewportEventHub.Instance.SetProjectionTypeRequested -= OnSetProjectionTypeRequested;
        ViewportEventHub.Instance.TranslateRequested -= OnTranslateRequested;
        ViewportEventHub.Instance.RotateRequested -= OnRotateRequested;
        ViewportEventHub.Instance.ZoomRequested -= OnZoomRequested;
        ViewportEventHub.Instance.ToggleProjectionTypeRequested -= OnToggleProjectionTypeRequested;
        ViewportEventHub.Instance.FitRequested -= OnFitRequested;
        ViewportEventHub.Instance.AlignNormalToRequested -= OnAlignNormalToRequested;
    }

    #endregion

    #region Events

    /// <summary>
    /// 選択操作モードの通知を受け取るイベントハンドラ、現在の選択操作モードを更新する
    /// </summary>
    /// <param name="mode">通知された選択操作モード</param>
    private void OnPickHandlingModeNotified(PickHandlingMode mode)
    {
        // 初回通知を受け取った時点で初期化済みとする
        _isInitialized = true;
        _currentPickHandlingMode = mode;
    }

    /// <summary>
    /// ピック結果の通知を受け取るイベントハンドラ、選択操作モードに応じて選択状態を更新する
    /// </summary>
    /// <param name="pickResult">通知されたピック結果</param>
    private void OnPickResultNotified(PickResult pickResult)
    {
        if (_currentPickHandlingMode == PickHandlingMode.NormalToFace)
        {
            // 法線方向の整列モードの場合は、ピック結果の法線方向を取得してカメラを整列させる
            if (pickResult.HasHit)
            {
                MovePositionTo(pickResult.Position, true);
                AlignNormalTo(pickResult.Normal, true);
            }
        }
    }

    /// <summary>
    /// カメラの状態の通知がリクエストされたときに呼び出されるイベントハンドラ、現在のカメラ状態をイベントハブを通じて通知する
    /// </summary>
    private void OnNotifyStateRequested()
    {
        ViewportEventHub.NotifyPosition(Position);
        ViewportEventHub.NotifyRotation(Transform.Basis.GetRotationQuaternion());
        ViewportEventHub.NotifySize(_camera.Size);
        ViewportEventHub.NotifyDistance(_camera.Position.Z);
        ViewportEventHub.NotifyFov(_camera.Fov);
        ViewportEventHub.NotifyProjectionType(_camera.Projection);
    }

    /// <summary>
    /// カメラの位置移動がリクエストされたときに呼び出されるイベントハンドラ
    /// </summary>
    /// <param name="position">移動先の位置</param>
    /// <param name="useTween"><see langword="true"/> の場合は補間アニメーションを使用する</param>
    private void OnMovePositionToRequested(Vector3 position, bool useTween)
    {
        MovePositionTo(position, useTween);
    }

    /// <summary>
    /// カメラの回転移動がリクエストされたときに呼び出されるイベントハンドラ
    /// </summary>
    /// <param name="rotation">回転先の姿勢</param>
    /// <param name="useTween"><see langword="true"/> の場合は補間アニメーションを使用する</param>
    private void OnMoveRotationToRequested(Quaternion rotation, bool useTween)
    {
        MoveRotationTo(rotation, useTween);
    }

    /// <summary>
    /// カメラのサイズの設定がリクエストされたときに呼び出されるイベントハンドラ
    /// </summary>
    /// <param name="size">設定するサイズ</param>
    /// <param name="useTween"><see langword="true"/> の場合は補間アニメーションを使用する</param>
    private void OnSetSizeRequested(float size, bool useTween)
    {
        SetSize(size, useTween);
    }

    /// <summary>
    /// カメラの距離の設定がリクエストされたときに呼び出されるイベントハンドラ
    /// </summary>
    /// <param name="distance">設定する距離</param>
    /// <param name="useTween"><see langword="true"/> の場合は補間アニメーションを使用する</param>
    private void OnSetDistanceRequested(float distance, bool useTween)
    {
        SetDistance(distance, useTween);
    }

    /// <summary>
    /// カメラの視野角（FOV）の設定がリクエストされたときに呼び出されるイベントハンドラ
    /// </summary>
    /// <param name="fov">設定する視野角</param>
    /// <param name="useTween"><see langword="true"/> の場合は補間アニメーションを使用する</param>
    private void OnSetFovRequested(float fov, bool useTween)
    {
        SetFov(fov, useTween);
    }

    /// <summary>
    /// カメラの投影タイプの設定がリクエストされたときに呼び出されるイベントハンドラ
    /// </summary>
    /// <param name="type">設定する投影タイプ</param>
    private void OnSetProjectionTypeRequested(Camera3D.ProjectionType type)
    {
        SetProjectionType(type);
    }

    /// <summary>
    /// カメラの平行移動がリクエストされたときに呼び出されるイベントハンドラ
    /// </summary>
    /// <param name="translation">移動量</param>
    /// <param name="spaceMode">移動の基準となる座標系</param>
    /// <param name="useTween"><see langword="true"/> の場合は補間アニメーションを使用する</param>
    private void OnTranslateRequested(Vector3 translation, SpaceMode spaceMode, bool useTween)
    {
        Translate(translation, spaceMode, useTween);
    }

    /// <summary>
    /// カメラの回転がリクエストされたときに呼び出されるイベントハンドラ
    /// </summary>
    /// <param name="rotation">回転先の姿勢</param>
    /// <param name="spaceMode">回転の基準となる座標系</param>
    /// <param name="useTween"><see langword="true"/> の場合は補間アニメーションを使用する</param>
    private void OnRotateRequested(Quaternion rotation, SpaceMode spaceMode, bool useTween)
    {
        Rotate(rotation, spaceMode, useTween);
    }

    /// <summary>
    /// カメラのズームがリクエストされたときに呼び出されるイベントハンドラ
    /// </summary>
    /// <param name="exponent">ズームの指数値</param>
    /// <param name="useTween"><see langword="true"/> の場合は補間アニメーションを使用する</param>
    private void OnZoomRequested(float exponent, bool useTween)
    {
        Zoom(exponent, useTween);
    }

    /// <summary>
    /// カメラの投影タイプの切り替えがリクエストされたときに呼び出されるイベントハンドラ
    /// </summary>
    private void OnToggleProjectionTypeRequested()
    {
        ToggleProjectionType();
    }

    /// <summary>
    /// カメラのフィット操作がリクエストされたときに呼び出されるイベントハンドラ
    /// </summary>
    /// <param name="targetNodes">フィット対象のノード群</param>
    /// <param name="useTween"><see langword="true"/> の場合は補間アニメーションを使用する</param>
    private void OnFitRequested(Node3D[] targetNodes, bool useTween)
    {
        Fit(targetNodes, useTween);
    }

    /// <summary>
    /// カメラの法線方向の整列がリクエストされたときに呼び出されるイベントハンドラ
    /// </summary>
    /// <param name="normal">整列先の法線方向を表すベクトル</param>
    /// <param name="useTween"><see langword="true"/> の場合は補間アニメーションを使用する</param>
    private void OnAlignNormalToRequested(Vector3 normal, bool useTween)
    {
        AlignNormalTo(normal, useTween);
    }

    #endregion

    #region Internal Helpers

    /// <summary>
    /// 注視点の位置を更新する
    /// </summary>
    /// <param name="position">移動先位置</param>
    /// <param name="useTween"><see langword="true"/> の場合は補間アニメーションを使用</param>
    private void MovePositionTo(Vector3 position, bool useTween = false)
    {
        if (useTween)
        {
            TweenPosition(position);
        }
        else
        {
            Transform = new Transform3D(Transform.Basis, position);
            ViewportEventHub.NotifyPosition(Position);
        }
    }

    /// <summary>
    /// 注視点の回転を更新する
    /// </summary>
    /// <param name="rotation">回転先姿勢</param>
    /// <param name="useTween"><see langword="true"/> の場合は補間アニメーションを使用</param>
    private void MoveRotationTo(Quaternion rotation, bool useTween = false)
    {
        if (useTween)
        {
            TweenRotation(rotation);
        }
        else
        {
            Transform = new Transform3D(new Basis(rotation), Transform.Origin);
            ViewportEventHub.NotifyRotation(Transform.Basis.GetRotationQuaternion());
        }
    }

    /// <summary>
    /// カメラの距離を設定する
    /// </summary>
    /// <param name="distance">設定する距離</param>
    /// <param name="useTween"><see langword="true"/> の場合は補間アニメーションを使用する</param>
    private void SetDistance(float distance, bool useTween = false)
    {
        if (useTween)
        {
            TweenDistance(distance);
        }
        else
        {
            _camera.Position = new Vector3(0, 0, distance);
            ViewportEventHub.NotifyDistance(_camera.Position.Z);
        }
    }

    /// <summary>
    /// カメラのサイズを設定する
    /// </summary>
    /// <param name="size">設定するサイズ</param>
    /// <param name="useTween"><see langword="true"/> の場合は補間アニメーションを使用する</param>
    private void SetSize(float size, bool useTween = false)
    {
        if (useTween)
        {
            TweenSize(size);
        }
        else
        {
            _camera.Size = size;
            ViewportEventHub.NotifySize(_camera.Size);
        }
    }

    /// <summary>
    /// カメラの視野角（FOV）を設定する
    /// </summary>
    /// <param name="fov">設定する視野角（度）</param>
    /// <param name="useTween"><see langword="true"/> の場合は補間アニメーションを使用</param>
    private void SetFov(float fov, bool useTween = false)
    {
        if (useTween)
        {
            TweenFov(fov);
        }
        else
        {
            _camera.Fov = fov;
            ViewportEventHub.NotifyFov(fov);
        }
    }

    /// <summary>
    /// カメラの投影方式を設定する
    /// </summary>
    /// <param name="projectionType">切り替え先の投影方式</param>
    private void SetProjectionType(Camera3D.ProjectionType projectionType)
    {
        if (_camera.Projection == projectionType)
        {
            return;
        }

        if (projectionType == Camera3D.ProjectionType.Perspective)
        {
            float distance = GetPerspectiveDistanceFromOrthographicSize();
            SetDistance(distance, false);
        }
        else
        {
            float size = GetOrthographicSizeFromPerspectiveDistance();
            SetSize(size, false);

            // 投影物がカメラの視界遠近範囲から出ないようにNearとFarの中間あたりに注視点を置く
            float farZ = (_camera.Near + _camera.Far) / 2.0f;
            SetDistance(farZ, false);
        }

        _camera.Projection = projectionType;
        ViewportEventHub.NotifyProjectionType(projectionType);
    }

    /// <summary>
    /// 指定した基準で移動する
    /// </summary>
    /// <param name="translation">移動量</param>
    /// <param name="spaceMode">移動の基準となる座標系</param>
    /// <param name="useTween"><see langword="true"/> の場合は補間アニメーションを使用する</param>
    private void Translate(Vector3 translation, SpaceMode spaceMode = SpaceMode.World, bool useTween = false)
    {
        Vector3 newPosition;
        switch (spaceMode)
        {
            case SpaceMode.World:
                // ワールド基準の平行移動はそのまま移動量を加算すれば実現できる
                newPosition = Transform.Origin + translation;
                MovePositionTo(newPosition, useTween);
                break;
            case SpaceMode.FocalPoint:
            case SpaceMode.Camera:
                // 注視点基準の平行移動は、カメラの向きに応じて移動量を回転させる必要がある
                Vector3 rotatedTranslation = Transform.Basis * translation;
                Transform = Transform.Translated(rotatedTranslation);
                MovePositionTo(Transform.Origin, useTween);
                break;
        }
    }

    /// <summary>
    /// 指定した基準で回転する
    /// </summary>
    /// <param name="rotation">加算する回転</param>
    /// <param name="spaceMode">回転の基準となる座標系</param>
    /// <param name="useTween"><see langword="true"/> の場合は補間アニメーションを使用</param>
    private void Rotate(Quaternion rotation, SpaceMode spaceMode, bool useTween = false)
    {
        Quaternion nowRotation = Transform.Basis.GetRotationQuaternion();
        Quaternion newRotation;
        switch (spaceMode)
        {
            case SpaceMode.World:
                // ワールド基準の回転は回転を先に掛けることで実現できる
                newRotation = rotation * nowRotation;
                MoveRotationTo(newRotation, useTween);
                break;
            case SpaceMode.FocalPoint:
                // 注視点基準の回転は回転を後に掛けることで実現できる
                newRotation = nowRotation * rotation;
                MoveRotationTo(newRotation, useTween);
                break;
            case SpaceMode.Camera:
                // カメラ基準で回転しているかのように見せるため、FocalPointを回しながら移動させる
                newRotation = nowRotation * rotation;
                // 透視投影では現在のカメラ座標を GlobalPosition で求められるが、平行投影では Z 距離を極端に大きくしているため使えないので Size を距離換算してカメラ位置を計算する
                Vector3 distance = new Vector3(0, 0, _camera.Projection == Camera3D.ProjectionType.Perspective ? _camera.Position.Z : GetPerspectiveDistanceFromOrthographicSize());
                Vector3 nowCameraPossition = Position + Transform.Basis.GetRotationQuaternion() * distance;
                Vector3 rotatedDistance = newRotation * distance;
                // 回転後のカメラ位置は回転前のカメラ位置から回転前の距離ベクトルを回転後の距離ベクトルに置き換えた分だけ移動した位置になる
                Vector3 newPosition = nowCameraPossition - rotatedDistance;
                MovePositionTo(newPosition, useTween);
                MoveRotationTo(newRotation, useTween);
                break;
        }
    }

    /// <summary>
    /// ズーム操作を実行する、等角投影ではサイズを変更し透視投影ではカメラの Z 距離を変更してズームを表現する
    /// </summary>
    /// <param name="exponent">ズームの指数値</param>
    /// <param name="useTween"><see langword="true"/> の場合は補間アニメーションを使用する</param>
    private void Zoom(float exponent, bool useTween = false)
    {
        float scale = Mathf.Pow(_zoomBase, exponent);
        // 投影方式ごとにズーム表現が異なるため、変更先を分ける
        if (_camera.Projection == Camera3D.ProjectionType.Orthogonal)
        {
            // 等角投影の場合はサイズを変更してズームを表現する
            float newSize = _camera.Size * scale;
            // サイズが小さくなりすぎて見えなくなるのを防止するため、最小値を設定する
            float fixedSize = Mathf.Max(newSize, _minZoomValue);
            SetSize(fixedSize, useTween);
        }
        else
        {
            // 透視投影の場合はカメラと焦点のZ距離を変更してズームを表現する
            float distance = _camera.Position.Z;
            float newDistance = Mathf.Max(distance * scale, _minZoomValue);
            // 距離が近すぎて見えなくなるのを防止するため、最小値を設定する
            float fixedDistance = Mathf.Max(newDistance, _minZoomValue);
            SetDistance(fixedDistance, useTween);
        }
    }

    /// <summary>
    /// 現在の投影方式を Perspective/Orthogonal でトグルする
    /// </summary>
    private void ToggleProjectionType()
    {
        // 現在の投影方式をトグルして、内部で必要な補正を行う
        Camera3D.ProjectionType nextProjection = _camera.Projection == Camera3D.ProjectionType.Perspective
            ? Camera3D.ProjectionType.Orthogonal
            : Camera3D.ProjectionType.Perspective;
        SetProjectionType(nextProjection);
    }

    /// <summary>
    /// 位置の補間アニメーションを実行する
    /// </summary>
    /// <param name="position">補間先の位置</param>
    private void TweenPosition(Vector3 position)
    {
        Tween tween = BuildTween();
        Vector3 startPos = Position;
        tween.TweenMethod(Callable.From<float>(t =>
        {
            Position = startPos.Lerp(position, t);
            ViewportEventHub.NotifyPosition(Position);
        }), 0f, 1f, _tweenDuration);
    }

    /// <summary>
    /// 回転の補間アニメーションを実行する
    /// </summary>
    /// <param name="rotation">補間先の回転</param>
    private void TweenRotation(Quaternion rotation)
    {
        Tween tween = BuildTween();
        Quaternion startRot = Transform.Basis.GetRotationQuaternion();
        tween.TweenMethod(Callable.From<float>(t =>
        {
            Transform = new Transform3D(
                new Basis(startRot.Slerp(rotation, t)),
                Transform.Origin
            );
            ViewportEventHub.NotifyRotation(Transform.Basis.GetRotationQuaternion());
        }), 0f, 1f, _tweenDuration);
    }

    /// <summary>
    /// 距離の補間アニメーションを実行する
    /// </summary>
    /// <param name="distance">補間先の距離</param>
    private void TweenDistance(float distance)
    {
        Tween tween = BuildTween();
        float startDistance = _camera.Position.Z;
        tween.TweenMethod(Callable.From<float>(distance =>
        {
            _camera.Position = new Vector3(0, 0, distance);
            ViewportEventHub.NotifyDistance(distance);
        }), startDistance, distance, _tweenDuration);
    }

    /// <summary>
    /// サイズの補間アニメーションを実行する
    /// </summary>
    /// <param name="size">補間先のサイズ</param>
    private void TweenSize(float size)
    {
        Tween tween = BuildTween();
        float startSize = _camera.Size;
        tween.TweenMethod(Callable.From<float>(size =>
        {
            _camera.Size = size;
            ViewportEventHub.NotifySize(size);
        }), startSize, size, _tweenDuration);
    }

    /// <summary>
    /// 視野角（FOV）の補間アニメーションを実行する
    /// </summary>
    /// <param name="fov">補間先の視野角</param>
    private void TweenFov(float fov)
    {
        Tween tween = BuildTween();
        float startFov = _camera.Fov;
        tween.TweenMethod(Callable.From<float>(fov =>
        {
            _camera.Fov = fov;
            ViewportEventHub.NotifyFov(fov);
        }), startFov, fov, _tweenDuration);
    }

    /// <summary>
    /// 指定ノード群配下を画角内に収めるようカメラを調整する
    /// </summary>
    /// <param name="targetRoots">フィット対象のルートノード群</param>
    /// <param name="useTween"><see langword="true"/> の場合は補間アニメーションを使用</param>
    /// <returns>フィット対象の AABB を取得できた場合は <see langword="true"/></returns>
    private bool Fit(IEnumerable<Node3D> targetRoots, bool useTween = false)
    {
        if (!CameraFitUtility.TryGetWorldAabb(targetRoots, out Aabb worldAabb))
        {
            return false;
        }

        Vector3 center = worldAabb.Position + worldAabb.Size * 0.5f;
        MovePositionTo(center, useTween);

        Basis inverseBasis = Transform.Basis.Inverse();
        Rect2 viewportRect = GetViewport().GetVisibleRect();
        float aspect = Mathf.Max(viewportRect.Size.X / Mathf.Max(viewportRect.Size.Y, 1.0f), 0.01f);

        float maxAbsX = 0.0f;
        float maxAbsY = 0.0f;
        float maxZ = float.NegativeInfinity;
        float requiredDistance = 0.0f;

        if (_camera.Projection == Camera3D.ProjectionType.Perspective)
        {
            float halfVerticalFov = Mathf.DegToRad(_camera.Fov) * 0.5f;
            float tanHalfY = Mathf.Max(Mathf.Tan(halfVerticalFov), 1e-5f);
            float tanHalfX = Mathf.Max(tanHalfY * aspect, 1e-5f);

            foreach (Vector3 corner in CameraFitUtility.GetAabbCorners(worldAabb))
            {
                Vector3 local = inverseBasis * (corner - center);
                requiredDistance = Mathf.Max(requiredDistance, local.Z + Mathf.Abs(local.X) / tanHalfX);
                requiredDistance = Mathf.Max(requiredDistance, local.Z + Mathf.Abs(local.Y) / tanHalfY);
                maxZ = Mathf.Max(maxZ, local.Z);
            }

            requiredDistance = Mathf.Max(requiredDistance, maxZ + _camera.Near * 1.5f);
            requiredDistance = Mathf.Max(requiredDistance * _fitPadding, _minZoomValue);

            SetDistance(requiredDistance, useTween);
        }
        else
        {
            foreach (Vector3 corner in CameraFitUtility.GetAabbCorners(worldAabb))
            {
                Vector3 local = inverseBasis * (corner - center);
                maxAbsX = Mathf.Max(maxAbsX, Mathf.Abs(local.X));
                maxAbsY = Mathf.Max(maxAbsY, Mathf.Abs(local.Y));
            }

            float requiredHeight = 2.0f * Mathf.Max(maxAbsY, maxAbsX / aspect);
            float targetSize = Mathf.Max(requiredHeight * _fitPadding, _minZoomValue);

            SetSize(targetSize, useTween);
        }

        return true;
    }

    /// <summary>
    /// カメラの法線方向を指定したベクトルに整列させるよう回転を調整する
    /// </summary>
    /// <param name="normal">整列先の法線方向を表すベクトル</param>
    /// <param name="useTween"><see langword="true"/> の場合は補間アニメーションを使用</param>
    private void AlignNormalTo(Vector3 normal, bool useTween = false)
    {
        if (normal.LengthSquared() < Mathf.Epsilon)
        {
            return;
        }

        // 注視点からカメラへの方向（ローカル +Z）を法線方向へ合わせる
        Vector3 targetBack = normal.Normalized();
        Vector3 currentUp = Transform.Basis.Y.Normalized();

        // 現在の画面上向きを、法線に直交する平面へ射影してロール方向を引き継ぐ
        Vector3 projectedUp = currentUp - targetBack * currentUp.Dot(targetBack);
        if (projectedUp.LengthSquared() < Mathf.Epsilon)
        {
            Vector3 currentRight = Transform.Basis.X.Normalized();
            Vector3 projectedRight = currentRight - targetBack * currentRight.Dot(targetBack);
            if (projectedRight.LengthSquared() < Mathf.Epsilon)
            {
                projectedRight = Mathf.Abs(targetBack.Dot(Vector3.Up)) < 0.999f ? Vector3.Up.Cross(targetBack) : Vector3.Right.Cross(targetBack);
            }

            Vector3 rightFromProjection = projectedRight.Normalized();
            projectedUp = targetBack.Cross(rightFromProjection);
        }

        Vector3 up = projectedUp.Normalized();
        Vector3 right = up.Cross(targetBack).Normalized();
        up = targetBack.Cross(right).Normalized();

        Basis targetBasis = new Basis(right, up, targetBack);
        Quaternion rotation = targetBasis.GetRotationQuaternion();
        MoveRotationTo(rotation, useTween);
    }

    /// <summary>
    /// Tween を構築するための共通処理、Tween の設定はこれで作成すると統一される
    /// </summary>
    /// <returns>構築された Tween オブジェクト</returns>
    private Tween BuildTween()
    {
        return CreateTween()
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
    }

    /// <summary>
    /// Orthogonal のサイズを Perspective のカメラ距離へ変換する
    /// </summary>
    /// <returns>Perspective のカメラ距離</returns>
    private float GetPerspectiveDistanceFromOrthographicSize()
    {
        float sizeAtZ1 = CalculateSizeAtZ1();
        return _camera.Size / sizeAtZ1;
    }

    /// <summary>
    /// Perspective のカメラ距離を Orthogonal のサイズへ変換する
    /// </summary>
    /// <returns>Orthogonal のサイズ</returns>
    private float GetOrthographicSizeFromPerspectiveDistance()
    {
        // 等角投影の場合はZ距離を固定して、サイズでズームを表現する方式にする
        float sizeAtZ1 = CalculateSizeAtZ1();
        return Mathf.Abs(_camera.Position.Z) * sizeAtZ1;
    }

    /// <summary>
    /// カメラのFOVから、距離Z=1のときに見える縦サイズを計算する
    /// </summary>
    /// <returns>距離Z=1のときに見える縦サイズ</returns>
    private float CalculateSizeAtZ1()
    {
        // FOVは度数法で与えられるため、ラジアンに変換
        float fovRadians = Mathf.DegToRad(_camera.Fov);

        // FOVの半分の角度のタンジェントを使用して計算
        return Mathf.Tan(fovRadians / 2.0f) * 2.0f; // Z=1のときのサイズを計算
    }

    #endregion
}
