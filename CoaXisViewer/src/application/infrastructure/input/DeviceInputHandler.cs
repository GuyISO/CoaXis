using Godot;
using System;

/// <summary>
/// 繝ｦ繝ｼ繧ｶ繝ｼ縺ｮ繧ｭ繝ｼ繝懊・繝峨ｄ繧ｳ繝ｳ繝医Ο繝ｼ繝ｩ繝ｼ蜈･蜉帙ｒ蜃ｦ逅・☆繧・Autoload 繝弱・繝・/// </summary>
public partial class DeviceInputHandler : Node
{
    #region Fields

    private float _translateSpeed;
    private float _rotateSpeedDegrees;
    private float _rollSpeedDegrees;

    #endregion

    #region Lifecycle

    public override void _Ready()
    {
        Application.Setting.Event.SettingsNotified += ApplySettings;
        ApplySettings();
    }

    public override void _ExitTree()
    {
        Application.Setting.Event.SettingsNotified -= ApplySettings;

        base._ExitTree();
    }

    public override void _Process(double delta)
    {
        HandleSelectModeInput("switch_selection_mode_add", SelectionMode.Add);
        HandleSelectModeInput("switch_selection_mode_remove", SelectionMode.Remove);
        HandleSelectModeInput("switch_selection_mode_toggle", SelectionMode.Toggle);
        HandleButtonInput();
        HandleTranslationInput((float)delta);
        HandleRotationInput((float)delta);
    }

    #endregion

    #region Internal Helpers

    /// <summary>
    /// 驕ｸ謚槭Δ繝ｼ繝峨・蛻・ｊ譖ｿ縺医ｒ蜃ｦ逅・☆繧・    /// </summary>
    private void HandleSelectModeInput(string actionName, SelectionMode assignMode)
    { 
        if (Input.IsActionJustPressed(actionName))
        {
            Application.Selection.Event.SetMode(assignMode);
        }
        else if (Input.IsActionJustReleased(actionName) && Application.Selection.Service.Mode == assignMode)
        {
            Application.Selection.Event.SetMode(SelectionMode.Set);
        }
    }

    /// <summary>
    /// Undo/Redo 蜈･蜉帙↓蠢懊§縺ｦ繧ｳ繝槭Φ繝牙ｱ･豁ｴ繧呈桃菴懊☆繧・    /// </summary>
    private void HandleButtonInput()
    {
        if (Input.IsActionJustPressed("undo"))
        {
            Application.Log.Debug("DeviceInputHandler: Undo requested.");
            Application.Command.Event.Undo();
        }

        if (Input.IsActionJustPressed("redo"))
        {
            Application.Log.Debug("DeviceInputHandler: Redo requested.");
            Application.Command.Event.Redo();
        }
        
        if (Input.IsActionJustPressed("load"))
        {
            Application.Model.Event.LoadModel("res://assets/models/car.glb");
        }

        if (Input.IsActionJustPressed("escape"))
        {
            Application.Pick.Event.SetHandlingMode(PickHandlingMode.Selection);
            Application.Selection.Event.SetMode(SelectionMode.Set);
            Application.Selection.Event.Clear();
        }
    }

    /// <summary>
    /// 繝ｦ繝ｼ繧ｶ繝ｼ縺ｮ蜈･蜉帙↓蝓ｺ縺･縺・※繧ｫ繝｡繝ｩ縺ｮ蟷ｳ陦檎ｧｻ蜍輔ｒ繝ｪ繧ｯ繧ｨ繧ｹ繝医☆繧・    /// </summary>
    /// <param name="delta">蜑阪・繝輔Ξ繝ｼ繝縺九ｉ縺ｮ邨碁℃譎る俣・育ｧ抵ｼ・/param>
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
        Application.Viewport.Event.Translate(translation, SpaceMode.Camera);
    }

    /// <summary>
    /// 繝ｦ繝ｼ繧ｶ繝ｼ縺ｮ蜈･蜉帙↓蝓ｺ縺･縺・※繧ｫ繝｡繝ｩ縺ｮ蝗櫁ｻ｢繧偵Μ繧ｯ繧ｨ繧ｹ繝医☆繧・    /// </summary>
    /// <param name="delta">蜑阪・繝輔Ξ繝ｼ繝縺九ｉ縺ｮ邨碁℃譎る俣・育ｧ抵ｼ・/param>
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

        Application.Viewport.Event.Rotate(rotation, SpaceMode.Camera);
    }

    /// <summary>
    /// 謖・ｮ壹＆繧後◆繧｢繧ｯ繧ｷ繝ｧ繝ｳ縺ｫ蝓ｺ縺･縺・※霆ｸ縺ｮ蛟､繧貞叙蠕励☆繧・    /// </summary>
    /// <param name="negativeAction">雋縺ｮ譁ｹ蜷代・繧｢繧ｯ繧ｷ繝ｧ繝ｳ蜷・/param>
    /// <param name="positiveAction">豁｣縺ｮ譁ｹ蜷代・繧｢繧ｯ繧ｷ繝ｧ繝ｳ蜷・/param>
    /// <returns>霆ｸ縺ｮ蛟､・・1.0縺九ｉ1.0縺ｮ遽・峇・・/returns>
    private float GetAxis(string negativeAction, string positiveAction)
    {
        return Input.GetActionStrength(positiveAction) - Input.GetActionStrength(negativeAction);
    }

    /// <summary>
    /// 險ｭ螳壼､繧貞渚譏縺吶ｋ
    /// </summary>
    private void ApplySettings()
    {
        InputSettings s = Application.Setting.Service.Current.Input;
        _translateSpeed = s.TranslateSpeed;
        _rotateSpeedDegrees = s.RotateSpeedDeg;
        _rollSpeedDegrees = s.RollSpeedDeg;
    }

    #endregion
}
