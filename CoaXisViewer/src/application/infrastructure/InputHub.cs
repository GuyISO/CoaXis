using Godot;

/// <summary>
/// インプットマップによるユーザー入力の取得とアプリケーション操作への変換を管理するハブ。
/// </summary>
public partial class InputHub : BaseHub
{
    #region Fields

    #endregion

    #region Properties

    #endregion

    #region Signals

    #endregion

    #region Lifecycle

    public override void _ExitTree()
    {
        base._ExitTree();
    }

    public override void _Process(double delta)
    {
        // 押下中に状態を切り替えるボタンの入力処理
        HandleSelectModeInput("switch_selection_mode_add", ModelEntitySelectionMode.Add);
        HandleSelectModeInput("switch_selection_mode_remove", ModelEntitySelectionMode.Remove);
        HandleSelectModeInput("switch_selection_mode_toggle", ModelEntitySelectionMode.Toggle);

        // ボタン入力処理
        HandleButtonInput();

        // 方向入力処理
        HandleTranslationInput((float)delta);
        HandleRotationInput((float)delta);
    }

    #endregion

    #region Events

    #endregion

    #region Methods

    #endregion

    #region Helpers

    /// <summary>
    /// 選択モードの切り替えを処理する。
    /// </summary>
    private void HandleSelectModeInput(string actionName, ModelEntitySelectionMode assignMode)
    { 
        if (Input.IsActionJustPressed(actionName))
        {
            Application.Model.Entity.Selection.SetMode(assignMode);
        }
        else if (Input.IsActionJustReleased(actionName) && Application.Model.Entity.Selection.Mode == assignMode)
        {
            Application.Model.Entity.Selection.SetMode(ModelEntitySelectionMode.Set);
        }
    }

    /// <summary>
    /// Undo/Redo 入力に応じてコマンド履歴を操作する。
    /// </summary>
    private void HandleButtonInput()
    {
        
        if (Input.IsActionJustPressed("clear"))
        {
            Application.Model.Entity.Load.Clear();
        }
        
        if (Input.IsActionJustPressed("undo"))
        {
            Application.Log.Debug("DeviceInputHandler: Undo requested.");
            Application.Command.Undo();
        }

        if (Input.IsActionJustPressed("redo"))
        {
            Application.Log.Debug("DeviceInputHandler: Redo requested.");
            Application.Command.Redo();
        }
        
        if (Input.IsActionJustPressed("escape"))
        {
            Application.Model.Entity.Pick.SetHandlingMode(PickHandlingMode.Selection);
            Application.Model.Entity.Selection.SetMode(ModelEntitySelectionMode.Set);
            Application.Model.Entity.Selection.Clear();
        }
    }

    /// <summary>
    /// ユーザーの入力に基づき、カメラを平行移動する。
    /// </summary>
    /// <param name="delta">前フレームからの経過時間（秒）</param>
    private void HandleTranslationInput(float delta)
    {
        InputSettings settings = Application.Setting.Current.Input;
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

        Vector3 translation = translationDirection * (settings.TranslateSpeed * delta);
        Application.Viewport.Camera.Translate(translation, SpaceMode.Camera);
    }

    /// <summary>
    /// ユーザーの入力に基づき、カメラを回転する。
    /// </summary>
    /// <param name="delta">前フレームからの経過時間（秒）</param>
    private void HandleRotationInput(float delta)
    {
        InputSettings settings = Application.Setting.Current.Input;
        float yawInput = GetAxis("rotate_camera_right", "rotate_camera_left");
        float pitchInput = GetAxis("rotate_camera_down", "rotate_camera_up");
        float rollInput = GetAxis("rotate_camera_clockwise", "rotate_camera_counterclockwise");

        if (Mathf.IsZeroApprox(yawInput) && Mathf.IsZeroApprox(pitchInput) && Mathf.IsZeroApprox(rollInput))
        {
            return;
        }

        float yawAngle = Mathf.DegToRad(yawInput * settings.RotateSpeedDeg * delta);
        float pitchAngle = Mathf.DegToRad(pitchInput * settings.RotateSpeedDeg * delta);
        float rollAngle = Mathf.DegToRad(rollInput * settings.RollSpeedDeg * delta);
        Quaternion yaw = new Quaternion(Vector3.Up, yawAngle);
        Quaternion pitch = new Quaternion(Vector3.Right, pitchAngle);
        Quaternion roll = new Quaternion(Vector3.Forward, rollAngle);
        Quaternion rotation = yaw * pitch * roll;

        Application.Viewport.Camera.Rotate(rotation, SpaceMode.Camera);
    }

    /// <summary>
    /// 指定されたアクションに基づき、軸の値を取得する。
    /// </summary>
    /// <param name="negativeAction">負の方向のアクション名</param>
    /// <param name="positiveAction">正の方向のアクション名</param>
    /// <returns>軸の値（-1.0 から 1.0）</returns>
    private float GetAxis(string negativeAction, string positiveAction)
    {
        return Input.GetActionStrength(positiveAction) - Input.GetActionStrength(negativeAction);
    }

    #endregion
}
