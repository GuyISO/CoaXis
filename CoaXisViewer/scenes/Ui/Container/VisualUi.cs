using Godot;
using System;
using System.Linq;

/// <summary>
/// モデルの表示状態をコントロールするパネル
/// </summary>
public partial class VisualUi : PanelContainer
{
    #region Fields

    private bool _isUpdating = false;

    // 関連ノードのキャッシュ
    private Label _labelValueTransparency = null!;
    private HSlider _sliderTransparency = null!;
    private Button _buttonInherit = null!;
    private Button _buttonVisible = null!;
    private Button _buttonInvisible = null!;

    #endregion

    #region Lifecycle

    public override void _Ready()
    {
        EnsureChildNodes();
        SubscribeUiEvents();
        SubscribeApplicationEvents();
        SyncInitialState();
    }

    public override void _ExitTree()
    {
        UnsubscribeUiEvents();
        UnsubscribeApplicationEvents();

        base._ExitTree();
    }

    #endregion

    #region Events

    /// <summary>
    /// UIイベントの購読を開始する
    /// </summary>
    private void SubscribeUiEvents()
    {
        _sliderTransparency.ValueChanged += OnSliderTransparencyValueChanged;
        _buttonInherit.Pressed += OnButtonInheritPressed;
        _buttonVisible.Pressed += OnButtonVisiblePressed;
        _buttonInvisible.Pressed += OnButtonInvisiblePressed;
    }

    /// <summary>
    /// UIイベントの購読を解除する
    /// </summary>
    private void UnsubscribeUiEvents()
    {
        _sliderTransparency.ValueChanged -= OnSliderTransparencyValueChanged;
        _buttonInherit.Pressed -= OnButtonInheritPressed;
        _buttonVisible.Pressed -= OnButtonVisiblePressed;
        _buttonInvisible.Pressed -= OnButtonInvisiblePressed;
    }

    /// <summary>
    /// Applicationイベントの購読を開始する
    /// </summary>
    private void SubscribeApplicationEvents()
    {
        Application.Model.Event.TransparencyNotified += OnTransparencyNotified;
    }

    /// <summary>
    /// Applicationイベントの購読を解除する
    /// </summary>
    private void UnsubscribeApplicationEvents()
    {
        Application.Model.Event.TransparencyNotified -= OnTransparencyNotified;
    }

    /// <summary>
    /// Transparency スライダー値変更時のイベントハンドラ
    /// </summary>
    /// <param name="value">スライダーの値 (0.0 - 1.0)</param>
    private void OnSliderTransparencyValueChanged(double value)
    {
        if (_isUpdating)
        {
            return;
        }

        float transparencyValue = (float)value;
        Application.Model.Service.SetTransparency(transparencyValue);
    }

    /// <summary>
    /// Inherit ボタン押下時のイベントハンドラ
    /// </summary>
    private void OnButtonInheritPressed()
    {
        ApplyVisibilityToSelection(ModelVisibility.Inherit);
    }

    /// <summary>
    /// Visible ボタン押下時のイベントハンドラ
    /// </summary>
    private void OnButtonVisiblePressed()
    {
        ApplyVisibilityToSelection(ModelVisibility.Visible);
    }

    /// <summary>
    /// Invisible ボタン押下時のイベントハンドラ
    /// </summary>
    private void OnButtonInvisiblePressed()
    {
        ApplyVisibilityToSelection(ModelVisibility.Invisible);
    }

    /// <summary>
    /// Transparency が通知されたときに呼び出されるイベントハンドラ
    /// </summary>
    /// <param name="transparency">通知された透明度値 (0.0 - 1.0)</param>
    private void OnTransparencyNotified(float transparency)
    {
        _isUpdating = true;
        _sliderTransparency.Value = transparency;
        UpdateTransparencyLabel(transparency);
        _isUpdating = false;
    }

    #endregion

    #region Internal Helpers

    /// <summary>
    /// 子ノードを解決し、フィールドに保持する
    /// </summary>
    private void EnsureChildNodes()
    {
        _labelValueTransparency = (Label)FindChild("LabelValueTransparency");
        _sliderTransparency = (HSlider)FindChild("HSliderTransparency");
        _buttonInherit = (Button)FindChild("ButtonInherit");
        _buttonVisible = (Button)FindChild("ButtonVisible");
        _buttonInvisible = (Button)FindChild("ButtonInvisible");
    }

    /// <summary>
    /// 初期状態を同期する
    /// </summary>
    private void SyncInitialState()
    {
        _isUpdating = true;
        _sliderTransparency.Value = Application.Model.Service.Transparency;
        UpdateTransparencyLabel(Application.Model.Service.Transparency);
        _isUpdating = false;
    }

    /// <summary>
    /// Transparency ラベルを更新する
    /// </summary>
    /// <param name="value">Transparency 値 (0.0 - 1.0)</param>
    private void UpdateTransparencyLabel(float value)
    {
        int percentage = (int)(value * 100);
        _labelValueTransparency.Text = percentage.ToString();
    }

    /// <summary>
    /// 選択されているすべてのモデルに指定された表示状態を適用する
    /// </summary>
    /// <param name="visibility">適用する表示状態</param>
    private void ApplyVisibilityToSelection(ModelVisibility visibility)
    {
        var selectedModelIds = Application.Selection.Service.ModelIds.ToArray();
        if (selectedModelIds.Length == 0)
        {
            Application.Log.Warn("VisualUi: No models are selected.");
            return;
        }

        var command = new SetModelVisibilityCommand(selectedModelIds, visibility);
        Application.Command.Event.Execute(command);
    }

    #endregion
}