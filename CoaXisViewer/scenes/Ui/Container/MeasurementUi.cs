using Godot;
using System;

/// <summary>
/// 測定ポイントの取得結果と派生値を表示するパネル
/// </summary>
public partial class MeasurementUi : PanelContainer
{
    #region Fields

    // 関連ノードの参照
    private readonly Label[] _labelPositionXs = new Label[2];
    private readonly Label[] _labelPositionYs = new Label[2];
    private readonly Label[] _labelPositionZs = new Label[2];
    private readonly Label[] _labelNormalXs = new Label[2];
    private readonly Label[] _labelNormalYs = new Label[2];
    private readonly Label[] _labelNormalZs = new Label[2];
    private readonly Button[] _buttonPicks = new Button[2];
    private Label _labelDistance = null!;
    private Label _labelAngle = null!;
    private Label _labelDeltaX = null!;
    private Label _labelDeltaY = null!;
    private Label _labelDeltaZ = null!;

    #endregion

    #region Lifecycle

    public override void _Ready()
    {
        // シーン構造が変更される可能性があるため、名前探索で関連ノードを解決する
        Container[] container = new Container[2];
        for (int i = 0; i < 2; i++)
        {
            container[i] = (Container)FindChild($"VBoxContainerPickResult{i + 1}");
            _labelPositionXs[i] = (Label)container[i].FindChild($"LabelValuePositionX");
            _labelPositionYs[i] = (Label)container[i].FindChild($"LabelValuePositionY");
            _labelPositionZs[i] = (Label)container[i].FindChild($"LabelValuePositionZ");
            _labelNormalXs[i] = (Label)container[i].FindChild($"LabelValueNormalX");
            _labelNormalYs[i] = (Label)container[i].FindChild($"LabelValueNormalY");
            _labelNormalZs[i] = (Label)container[i].FindChild($"LabelValueNormalZ");
            _buttonPicks[i] = (Button)container[i].FindChild($"ButtonPick");
        }
        _labelDistance = (Label)FindChild("LabelValueDistance");
        _labelAngle = (Label)FindChild("LabelValueAngle");
        _labelDeltaX = (Label)FindChild("LabelValueDeltaX");
        _labelDeltaY = (Label)FindChild("LabelValueDeltaY");
        _labelDeltaZ = (Label)FindChild("LabelValueDeltaZ");

        // UIイベントの購読開始
        _buttonPicks[0].Pressed += OnButtonPick1Pressed;
        _buttonPicks[1].Pressed += OnButtonPick2Pressed;

        Application.Measurement.Event.MeasurementResultNotified += OnMeasurementUpdated;
        Application.Measurement.Event.AskMeasurementResult();
    }

    public override void _ExitTree()
    {
        // UIイベントの購読解除
        _buttonPicks[0].Pressed -= OnButtonPick1Pressed;
        _buttonPicks[1].Pressed -= OnButtonPick2Pressed;

        Application.Measurement.Event.MeasurementResultNotified -= OnMeasurementUpdated;
    }

    #endregion

    #region Events

    /// <summary>
    /// ピックボタン1のクリックイベントハンドラ、ポイント1の選択をリクエストする
    /// </summary>
    private void OnButtonPick1Pressed()
    {
        Application.Log.Service.Debug("MeasurementUi: pick point 1 requested.");
        Application.Measurement.Event.SetPickPoint(1);
    }

    /// <summary>
    /// ピックボタン2のクリックイベントハンドラ、ポイント2の選択をリクエストする
    /// </summary>
    private void OnButtonPick2Pressed()
    {
        Application.Log.Service.Debug("MeasurementUi: pick point 2 requested.");
        Application.Measurement.Event.SetPickPoint(2);
    }

    /// <summary>
    /// 測定結果の通知を受け取り、UIラベルを更新する
    /// </summary>
    /// <param name="result">測定結果</param>
    private void OnMeasurementUpdated(MeasurementResult result)
    {
        UpdatePointLabels(0, result.HasPoint1, result.Position1, result.Normal1);
        UpdatePointLabels(1, result.HasPoint2, result.Position2, result.Normal2);

        _labelDeltaX.Text = result.HasPoint1 && result.HasPoint2 ? result.Delta.X.ToString("F3") : "-";
        _labelDeltaY.Text = result.HasPoint1 && result.HasPoint2 ? result.Delta.Y.ToString("F3") : "-";
        _labelDeltaZ.Text = result.HasPoint1 && result.HasPoint2 ? result.Delta.Z.ToString("F3") : "-";
        _labelDistance.Text = result.HasPoint1 && result.HasPoint2 ? result.Distance.ToString("F3") : "-";
        _labelAngle.Text = (result.HasPoint1 && result.HasPoint2 && !float.IsNaN(result.Angle)) ? result.Angle.ToString("F1") : "-";
    }

    #endregion

    #region Internal Helpers

    private void UpdatePointLabels(int index, bool hasPoint, Vector3 position, Vector3 normal)
    {
        if (!hasPoint)
        {
            _labelPositionXs[index].Text = "-";
            _labelPositionYs[index].Text = "-";
            _labelPositionZs[index].Text = "-";
            _labelNormalXs[index].Text = "-";
            _labelNormalYs[index].Text = "-";
            _labelNormalZs[index].Text = "-";
            return;
        }

        _labelPositionXs[index].Text = position.X.ToString("F3");
        _labelPositionYs[index].Text = position.Y.ToString("F3");
        _labelPositionZs[index].Text = position.Z.ToString("F3");
        _labelNormalXs[index].Text = normal.X.ToString("F3");
        _labelNormalYs[index].Text = normal.Y.ToString("F3");
        _labelNormalZs[index].Text = normal.Z.ToString("F3");
    }

    #endregion
}
