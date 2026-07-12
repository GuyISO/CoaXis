using Godot;
using System;

/// <summary>
/// ユーザーのキーボードやコントローラー入力を処理する Autoload ノード
/// </summary>
public partial class DeviceInputHandler : Node
{
    #region Fields

    // TODO: 将来的にユーザーが設定可能な値にする
    [ExportGroup("Settings")]
    [Export] private float _translateSpeed = 8.0f;
    [Export] private float _rotateSpeedDegrees = 90.0f;
    [Export] private float _rollSpeedDegrees = 120.0f;

    private bool _isMultiSelectMode = false;

    #endregion

    #region Lifecycle

    public override void _Process(double delta)
    {
        // マルチセレクトモードの状態を更新する
        bool wasMultiSelectMode = _isMultiSelectMode;
        _isMultiSelectMode = Input.IsActionPressed("select_multiple");
        if (wasMultiSelectMode != _isMultiSelectMode)
        {
            Application.Model.SetMultiSelectionMode(_isMultiSelectMode);
        }

        if (Input.IsActionJustPressed("load"))
        {
            Application.Model.LoadModel("res://assets/models/car.glb");
        }

        if (Input.IsActionJustPressed("escape"))
        {
            Application.Pick.NotifyPickHandlingMode(PickHandlingMode.Selection);
            Application.Model.ClearSelection();
        }

        HandleUndoRedoInput();

        // ユーザーの入力に基づいてカメラの平行移動と回転をリクエストする
        float dt = (float)delta;
        HandleTranslationInput(dt);
        HandleRotationInput(dt);
    }

    #endregion

    #region Internal Helpers

    /// <summary>
    /// Undo/Redo 入力に応じてコマンド履歴を操作する
    /// </summary>
    private void HandleUndoRedoInput()
    {
        bool undoPressed = Input.IsActionJustPressed("undo");
        bool redoPressed = Input.IsActionJustPressed("redo");
        if (!undoPressed && !redoPressed)
        {
            return;
        }

        if (undoPressed)
        {
            Application.Log.Debug("DeviceInputHandler: Undo requested.");
            UndoService.Undo();
        }

        if (redoPressed)
        {
            Application.Log.Debug("DeviceInputHandler: Redo requested.");
            UndoService.Redo();
        }
    }

    /// <summary>
    /// ユーザーの入力に基づいてカメラの平行移動をリクエストする
    /// </summary>
    /// <param name="delta">前のフレームからの経過時間（秒）</param>
    private void HandleTranslationInput(float delta)
    {
        float x = GetAxis("translate_camera_left", "translate_camera_right");
        float y = GetAxis("translate_camera_down", "translate_camera_up");
        float z = GetAxis("translate_camera_forward", "translate_camera_backward");

        Vector3 translationDirection = new Vector3(x, y, z);
        if (translationDirection.LengthSquared() <= Mathf.Epsilon)
        {
            return;
        }

        if (translationDirection.LengthSquared() > 1.0f)
        {
            translationDirection = translationDirection.Normalized();
        }

        Vector3 translation = translationDirection * (_translateSpeed * delta);
        Application.Viewport.Translate(translation, SpaceMode.Camera);
    }

    /// <summary>
    /// ユーザーの入力に基づいてカメラの回転をリクエストする
    /// </summary>
    /// <param name="delta">前のフレームからの経過時間（秒）</param>
    private void HandleRotationInput(float delta)
    {
        float yawInput = GetAxis("rotate_camera_right", "rotate_camera_left");
        float pitchInput = GetAxis("rotate_camera_down", "rotate_camera_up");
        float rollInput = GetAxis("rotate_camera_clockwise", "rotate_camera_counterclockwise");

        if (Mathf.IsZeroApprox(yawInput) && Mathf.IsZeroApprox(pitchInput) && Mathf.IsZeroApprox(rollInput))
        {
            return;
        }

        float yawAngle = Mathf.DegToRad(yawInput * _rotateSpeedDegrees * delta);
        float pitchAngle = Mathf.DegToRad(pitchInput * _rotateSpeedDegrees * delta);
        float rollAngle = Mathf.DegToRad(rollInput * _rollSpeedDegrees * delta);
        Quaternion yaw = new Quaternion(Vector3.Up, yawAngle);
        Quaternion pitch = new Quaternion(Vector3.Right, pitchAngle);
        Quaternion roll = new Quaternion(Vector3.Forward, rollAngle);
        Quaternion rotation = yaw * pitch * roll;

        Application.Viewport.Rotate(rotation, SpaceMode.Camera);
    }

    /// <summary>
    /// 指定されたアクションに基づいて軸の値を取得する
    /// </summary>
    /// <param name="negativeAction">負の方向のアクション名</param>
    /// <param name="positiveAction">正の方向のアクション名</param>
    /// <returns>軸の値（-1.0から1.0の範囲）</returns>
    private float GetAxis(string negativeAction, string positiveAction)
    {
        return Input.GetActionStrength(positiveAction) - Input.GetActionStrength(negativeAction);
    }

    #endregion
}
