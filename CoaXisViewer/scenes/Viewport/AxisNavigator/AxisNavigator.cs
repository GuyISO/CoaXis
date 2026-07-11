using Godot;
using System.Globalization;

/// <summary>
/// 画面隅の軸ナビゲータを制御し、クリックでカメラ向きを変更する
/// </summary>
/// <remarks>
/// AxisNavigator関連のオブジェクトは表示も当たり判定もすべてマスクレイヤー2に配置して使用する
/// </remarks>
public partial class AxisNavigator : Control
{
    #region Fields

    // 関連ノードのキャッシュ
    private SubViewportContainer _subViewportContainer = null!;
    private SubViewport _subViewport = null!;
    private Node3D _focalPoint = null!;
    private Camera3D _camera = null!;

    private bool _isInitialized = false; // メインビューポートのカメラの初期状態を取得してUIに反映するためのフラグ

    #endregion

    #region Lifecycle

    public override void _Ready()
    {
        // 関連ノードのキャッシュ
        _subViewportContainer = GetNodeOrNull<SubViewportContainer>("SubViewportContainer");
        _subViewport = _subViewportContainer?.GetNodeOrNull<SubViewport>("SubViewport");
        _focalPoint = _subViewport?.GetNodeOrNull<Node3D>("FocalPoint");
        _camera = _focalPoint?.GetNodeOrNull<Camera3D>("Camera3D");

        // イベント購読の登録
        Application.Viewport.RotationNotified += OnRotationNotified;
    }

    public override void _ExitTree()
    {
        // イベント購読の解除
        Application.Viewport.RotationNotified -= OnRotationNotified;
    }

    public override void _Process(double delta)
    {
        if (!_isInitialized)
        {
            // カメラの初期回転を取得して軸ナビゲータに反映する
            Application.Viewport.RequestNotifyState();
        }
    }

    /// <summary>
    /// 軸ナビゲータのクリックを検知し、対応する向きへカメラを移動する
    /// </summary>
    /// <param name="@event">未処理入力イベント</param>
    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
        {
            var localMouse = mb.Position - _subViewportContainer.GlobalPosition;
            if (!new Rect2(Vector2.Zero, _subViewportContainer.Size).HasPoint(localMouse))
            {
                return;
            }

            PickResult pickResult = PickUtility.PickByRay(_camera, localMouse, 1u << 1); // mask 2 only
            if (pickResult.HasHit)
            {
                ViewLookAt(pickResult.Collider);
                _subViewport.SetInputAsHandled(); // 入力イベントを消費する
            }
        }
    }

    #endregion

    #region Events

    private void OnRotationNotified(Quaternion rotation)
    {
        _isInitialized = true;
        _focalPoint.Quaternion = rotation;
    }

    #endregion

    #region Internal Helpers

    /// <summary>
    /// 指定されたノードの名前を回転角度（度）として解釈しその向きにカメラを移動させ
    /// ノード名は "x, y, z" 形式の回転角度を表す想定で、クォータニオンに変換してカメラ回転要求イベントを発行する
    /// </summary>
    /// <param name="node">回転角度を表す名称を持つノード</param>
    private void ViewLookAt(Node3D node)
    {
        if (TryParseRotationDegreesFromName(node.Name, out Vector3 rotationDegrees))
        {
            // オイラー角をdegからradに変換し、クォータニオンに変換
            var quaternion = Quaternion.FromEuler(rotationDegrees * (Mathf.Pi / 180f));
            // _cameraController.MoveFocalPoint(null, quaternion, true);

            // カメラ回転要求イベントを発行
            Application.Viewport.RequestMoveRotationTo(quaternion, true);
        }
    }

    /// <summary>
    /// ノード名を "x, y, z" 形式の回転角度（度）として解釈し、クォータニオンに変換する
    /// </summary>
    /// <param name="name">ノード名</param>
    /// <param name="rotationDegrees">変換された回転角度（度）</param>
    /// <returns>変換に成功した場合は true、失敗した場合は false を返す</returns>
    private static bool TryParseRotationDegreesFromName(string name, out Vector3 rotationDegrees)
    {
        // ノード名が "x, y, z" 形式の回転角度を表す想定で、Quaternion に変換する
        rotationDegrees = Vector3.Zero;
        var parts = name.Split(',');
        if (parts.Length != 3)
        {
            return false;
        }

        if (!float.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var x))
        {
            return false;
        }

        if (!float.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var y))
        {
            return false;
        }

        if (!float.TryParse(parts[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var z))
        {
            return false;
        }

        rotationDegrees = new Vector3(x, y, z);
        return true;
    }

    #endregion
}
