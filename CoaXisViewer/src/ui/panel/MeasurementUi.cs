using Godot;
using System;

/// <summary>
/// 測定ポイントの取得結果と派生値を表示するパネル
/// </summary>
public partial class MeasurementUi : PanelContainer
{
    #region Fields

    private bool _isInitialized = false;

    // 関連ノードの参照
    private readonly Label[] _labelPositionXs = new Label[2];
    private readonly Label[] _labelPositionYs = new Label[2];
    private readonly Label[] _labelPositionZs = new Label[2];
    private readonly Label[] _labelNormalXs = new Label[2];
    private readonly Label[] _labelNormalYs = new Label[2];
    private readonly Label[] _labelNormalZs = new Label[2];
    private readonly Button[] _buttonPicks = new Button[2];
    private readonly Button[] _buttonClears = new Button[2];
    private Label _labelDistance = null!;
    private Label _labelAngle = null!;
    private Label _labelDeltaX = null!;
    private Label _labelDeltaY = null!;
    private Label _labelDeltaZ = null!;

    #endregion

    #region Lifecycle

    public override void _Ready()
    {
        EnsureChildNodes();
        SubscribeUiEvents();
        SubscribeApplicationEvents();
    }

    public override void _Process(double delta)
    {
        if (!_isInitialized)
        {
            Application.Measurement.Event.AskResult();
        }
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
    /// 関連ノードの参照を解決する。シーン構造が変更される可能性があるため、名前探索で関連ノードを解決する
    /// </summary>
    private void EnsureChildNodes()
    {
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
            _buttonClears[i] = (Button)container[i].FindChild($"ButtonClear");
        }
        _labelDistance = (Label)FindChild("LabelValueDistance");
        _labelAngle = (Label)FindChild("LabelValueAngle");
        _labelDeltaX = (Label)FindChild("LabelValueDeltaX");
        _labelDeltaY = (Label)FindChild("LabelValueDeltaY");
        _labelDeltaZ = (Label)FindChild("LabelValueDeltaZ");
    }

    /// <summary>
    /// Applicationイベントの購読を開始する
    /// </summary>
    private void SubscribeApplicationEvents()
    {
        Application.Measurement.Event.ResultNotified += OnResultNotified;
        Application.Measurement.Event.PointNotified += OnPointNotified;
    }

    /// <summary>
    /// Applicationイベントの購読を解除する
    /// </summary>
    private void UnsubscribeApplicationEvents()
    {
        Application.Measurement.Event.ResultNotified -= OnResultNotified;
        Application.Measurement.Event.PointNotified -= OnPointNotified;
    }

    /// <summary>
    /// UIイベントの購読を開始する
    /// </summary>
    private void SubscribeUiEvents()
    {
        _buttonPicks[0].Pressed += OnButtonPick1Pressed;
        _buttonPicks[1].Pressed += OnButtonPick2Pressed;
        _buttonClears[0].Pressed += OnButtonClear1Pressed;
        _buttonClears[1].Pressed += OnButtonClear2Pressed;
    }

    /// <summary>
    /// UIイベントの購読を解除する
    /// </summary>
    private void UnsubscribeUiEvents()
    {
        _buttonPicks[0].Pressed -= OnButtonPick1Pressed;
        _buttonPicks[1].Pressed -= OnButtonPick2Pressed;
        _buttonClears[0].Pressed -= OnButtonClear1Pressed;
        _buttonClears[1].Pressed -= OnButtonClear2Pressed;
    }

    /// <summary>
    /// ピックボタン1のクリックイベントハンドラ、ポイント1の選択をリクエストする
    /// </summary>
    private void OnButtonPick1Pressed()
    {
        Application.Log.Debug("MeasurementUi: pick point 1 requested.");
        Application.Measurement.Event.SetPoint(1);
    }

    /// <summary>
    /// ピックボタン2のクリックイベントハンドラ、ポイント2の選択をリクエストする
    /// </summary>
    private void OnButtonPick2Pressed()
    {
        Application.Log.Debug("MeasurementUi: pick point 2 requested.");
        Application.Measurement.Event.SetPoint(2);
    }

    /// <summary>
    /// クリアボタン1のクリックイベントハンドラ、ポイント1のクリアをリクエストする
    /// </summary>
    private void OnButtonClear1Pressed()
    {
        Application.Log.Debug("MeasurementUi: clear point 1 requested.");
        Application.Measurement.Event.ClearPoint(1);
    }

    /// <summary>
    /// クリアボタン2のクリックイベントハンドラ、ポイント2のクリアをリクエストする
    /// </summary>
    private void OnButtonClear2Pressed()
    {
        Application.Log.Debug("MeasurementUi: clear point 2 requested.");
        Application.Measurement.Event.ClearPoint(2);
    }

    /// <summary>
    /// 測定結果の通知を受け取り、UIラベルを更新する
    /// </summary>
    /// <param name="result">測定結果</param>
    private void OnResultNotified(MeasurementResult result)
    {
        // 初回実行時に初期化済フラグを立てる
        _isInitialized = true;

        Vector3 position1ForDisplay = Vector3.Zero;
        Vector3 position2ForDisplay = Vector3.Zero;
        Vector3 normal1ForDisplay = Vector3.Zero;
        Vector3 normal2ForDisplay = Vector3.Zero;

        if (result.HasPoint1)
        {
            position1ForDisplay = CoordinateSystemUtility.GodotToCatia(result.Position1);
            normal1ForDisplay = CoordinateSystemUtility.GodotDirectionToCatia(result.Normal1);
        }

        if (result.HasPoint2)
        {
            position2ForDisplay = CoordinateSystemUtility.GodotToCatia(result.Position2);
            normal2ForDisplay = CoordinateSystemUtility.GodotDirectionToCatia(result.Normal2);
        }

        bool hasBothPoints = result.HasPoint1 && result.HasPoint2;
        Vector3 deltaForDisplay = hasBothPoints
            ? CoordinateSystemUtility.GodotToCatia(result.Delta)
            : Vector3.Zero;
        float distanceForDisplay = hasBothPoints
            ? CoordinateSystemUtility.GodotDistanceToCatia(result.Distance)
            : float.NaN;

        UpdatePointLabels(0, result.HasPoint1, position1ForDisplay, normal1ForDisplay);
        UpdatePointLabels(1, result.HasPoint2, position2ForDisplay, normal2ForDisplay);

        _labelDeltaX.Text = hasBothPoints ? deltaForDisplay.X.ToString("F3") : "-";
        _labelDeltaY.Text = hasBothPoints ? deltaForDisplay.Y.ToString("F3") : "-";
        _labelDeltaZ.Text = hasBothPoints ? deltaForDisplay.Z.ToString("F3") : "-";
        _labelDistance.Text = hasBothPoints ? distanceForDisplay.ToString("F3") : "-";
        _labelAngle.Text = (hasBothPoints && !float.IsNaN(result.Angle)) ? result.Angle.ToString("F1") : "-";
    }

    /// <summary>
    /// 測定ポイントの通知を受け取り、UIラベルの有効/無効状態を更新する
    /// </summary>
    /// <param name="pointIndex">測定ポイントのインデックス (0: 未選択、1: ポイント1、2: ポイント2)</param>
    private void OnPointNotified(int pointIndex)
    {
        switch (pointIndex)
        {
            case 0:
                _buttonPicks[0].ButtonPressed = false;
                _buttonPicks[1].ButtonPressed = false;
                break;
            case 1:
                _buttonPicks[0].ButtonPressed = true;
                _buttonPicks[1].ButtonPressed = false;
                break;
            case 2:
                _buttonPicks[0].ButtonPressed = false;
                _buttonPicks[1].ButtonPressed = true;
                break;
            default:
                Application.Log.Warn($"MeasurementUi: invalid pick point index {pointIndex}.");
                break;
        }
    }

    #endregion

    #region Internal Helpers

    /// <summary>
    /// 測定結果を受け取り、UIラベルを更新する
    /// </summary>
    /// <param name="index">ポイントのインデックス（0または1）</param>
    /// <param name="hasPoint">ポイントが有効かどうか</param>
    /// <param name="position">ポイントの位置</param>
    /// <param name="normal">ポイントの法線</param>
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
