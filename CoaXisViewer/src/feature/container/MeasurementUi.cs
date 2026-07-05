using Godot;
using System;

/// <summary>
/// 測定ポイントの取得結果と派生値を表示するパネル
/// </summary>
public partial class MeasurementUi : PanelContainer
{
    #region Fields

    private bool _isInitialized = false;

    private PickHandlingMode _pickHandlingMode;
    private int _pickingPointIndex = 0; // 0: 未選択、1: ポイント1、2: ポイント2

    // 測定位置のラベルを表示するための PackedScene
    private PackedScene _annotationLabel = ResourceLoader.Load<PackedScene>("res://scenes/AnnotationLabel.tscn")!;
    private readonly Node3D[] _annotationInstances = new Node3D[2];
    private readonly ImmediateMesh _measurementLineMesh = new ImmediateMesh();

    // ピック結果の保持
    private PickResult[] _points = new PickResult[2] { new PickResult(), new PickResult() };

    // 関連ノードの参照
    private Node3D _measurementVisualRoot = null!;
    private MeshInstance3D _measurementLine = null!;

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

        // イベントの購読開始
        PickEventHub.Instance.PickHandlingModeNotified += OnPickHandlingModeNotified;
        PickEventHub.Instance.PickResultNotified += OnPickResultNotified;

        SetupMeasurementVisuals();
    }

    public override void _ExitTree()
    {
        // UIイベントの購読解除
        _buttonPicks[0].Pressed -= OnButtonPick1Pressed;
        _buttonPicks[1].Pressed -= OnButtonPick2Pressed;

        // イベントの購読解除
        PickEventHub.Instance.PickHandlingModeNotified -= OnPickHandlingModeNotified;
        PickEventHub.Instance.PickResultNotified -= OnPickResultNotified;

        // 実行時生成した注釈ラベルを破棄
        ClearAnnotationLabels();

        if (_measurementVisualRoot != null && GodotObject.IsInstanceValid(_measurementVisualRoot))
        {
            _measurementVisualRoot.QueueFree();
            _measurementVisualRoot = null;
            _measurementLine = null;
        }
    }

    public override void _Process(double delta)
    {
        if (!_isInitialized)
        {
            PickEventHub.RequestNotifyPickHandlingMode();
        }
    }

    #endregion

    #region Events

    /// <summary>
    /// ピックボタン1のクリックイベントハンドラ、ポイント1の選択をリクエストする
    /// </summary>
    private void OnButtonPick1Pressed()
    {
        LogHub.Debug("MeasurementUi: pick point 1 requested.");
        PickEventHub.NotifyPickHandlingMode(PickHandlingMode.Measurement);
        _pickingPointIndex = 1;
    }

    /// <summary>
    /// ピックボタン2のクリックイベントハンドラ、ポイント2の選択をリクエストする
    /// </summary>
    private void OnButtonPick2Pressed()
    {
        LogHub.Debug("MeasurementUi: pick point 2 requested.");
        PickEventHub.NotifyPickHandlingMode(PickHandlingMode.Measurement);
        _pickingPointIndex = 2;
    }

    /// <summary>
    /// 選択操作モードの通知を受け取るイベントハンドラ、測定モードの状態を更新する
    /// </summary>
    /// <param name="mode">通知された選択操作モード</param>
    private void OnPickHandlingModeNotified(PickHandlingMode mode)
    {
        _isInitialized = true;
        _pickHandlingMode = mode;

        if (mode != PickHandlingMode.Measurement)
        {
            // 測定モード以外の通知が来た場合はピックインデックスをリセットする
            _pickingPointIndex = 0;
        }
    }

    /// <summary>
    /// PickEventHub からの PickResultNotified シグナルを受け取るイベントハンドラ、選択結果を保持してUIを更新する
    /// </summary>
    private void OnPickResultNotified(PickResult pickResult)
    {
        // 測定モードではない場合は無視する
        if (_pickHandlingMode != PickHandlingMode.Measurement)
        {
            return;
        }

        // 測定対象のポイントが明示されていない場合は無視する
        if (_pickingPointIndex == 0)
        {
            return;
        }

        // ピック結果が空なら処理できない
        if (pickResult == null || pickResult.Model == null)
        {
            return;
        }

        int index = _pickingPointIndex - 1;
        _points[index] = pickResult;
        LogHub.Debug($"MeasurementUi: point {_pickingPointIndex} picked. Position: {_points[index].Position}, Normal: {_points[index].Normal}, Distance: {_points[index].Distance}");

        UpdateAnnotationLabel(index, pickResult);
        RefreshMeasurementLabels();
    }

    #endregion

    #region Internal Helpers

    /// <summary>
    /// 測定ポイントのラベルと測定線を更新する
    /// </summary>
    private void RefreshMeasurementLabels()
    {
        for (int i = 0; i < _points.Length; i++)
        {
            UpdatePointLabels(i, _points[i]);
        }

        UpdateMeasurementLine();

        if (_points[0].HasHit && _points[1].HasHit)
        {
            Vector3 point1 = CoordinateSystemUtility.GodotToCatia(_points[0].Position) * 1000.0f; // Convert meters to millimeters
            Vector3 point2 = CoordinateSystemUtility.GodotToCatia(_points[1].Position) * 1000.0f; // Convert meters to millimeters
            Vector3 delta = point2 - point1;

            _labelDeltaX.Text = delta.X.ToString("F3");
            _labelDeltaY.Text = delta.Y.ToString("F3");
            _labelDeltaZ.Text = delta.Z.ToString("F3");
            _labelDistance.Text = point1.DistanceTo(point2).ToString("F3");
            float angle = ComputeNormalAngleDegrees(_points[0].Normal, _points[1].Normal);
            _labelAngle.Text = float.IsNaN(angle) ? "-" : angle.ToString("F1");
            return;
        }

        _labelDeltaX.Text = "-";
        _labelDeltaY.Text = "-";
        _labelDeltaZ.Text = "-";
        _labelDistance.Text = "-";
        _labelAngle.Text = "-";
    }

    private void UpdatePointLabels(int index, PickResult pickResult)
    {
        if (!pickResult.HasHit)
        {
            _labelPositionXs[index].Text = "-";
            _labelPositionYs[index].Text = "-";
            _labelPositionZs[index].Text = "-";
            _labelNormalXs[index].Text = "-";
            _labelNormalYs[index].Text = "-";
            _labelNormalZs[index].Text = "-";
            return;
        }

        Vector3 position = CoordinateSystemUtility.GodotToCatia(pickResult.Position) * 1000.0f; // Convert meters to millimeters
        Vector3 normal = CoordinateSystemUtility.GodotToCatia(pickResult.Normal);

        _labelPositionXs[index].Text = position.X.ToString("F3");
        _labelPositionYs[index].Text = position.Y.ToString("F3");
        _labelPositionZs[index].Text = position.Z.ToString("F3");
        _labelNormalXs[index].Text = normal.X.ToString("F3");
        _labelNormalYs[index].Text = normal.Y.ToString("F3");
        _labelNormalZs[index].Text = normal.Z.ToString("F3");
    }

    /// <summary>
    /// 2つの法線ベクトルのなす角度を計算する
    /// </summary>
    /// <param name="normal1">1つ目の法線ベクトル</param>
    /// <param name="normal2">2つ目の法線ベクトル</param>
    /// <returns>2つの法線ベクトルのなす角度（度単位）</returns>
    private float ComputeNormalAngleDegrees(Vector3 normal1, Vector3 normal2)
    {
        if (normal1.LengthSquared() <= Mathf.Epsilon || normal2.LengthSquared() <= Mathf.Epsilon)
        {
            return float.NaN;
        }

        float dot = Mathf.Clamp(normal1.Normalized().Dot(normal2.Normalized()), -1.0f, 1.0f);
        return Mathf.RadToDeg(Mathf.Acos(dot));
    }

    /// <summary>
    /// 指定したインデックスの測定ポイントに注釈ラベルを配置する
    /// </summary>
    /// <param name="index">測定ポイントのインデックス</param>
    /// <param name="pickResult">ピック結果</param>
    private void UpdateAnnotationLabel(int index, PickResult pickResult)
    {
        RemoveAnnotationLabel(index);

        if (!pickResult.HasHit)
        {
            return;
        }

        if (_annotationLabel == null)
        {
            LogHub.Warn("MeasurementUi: annotation label scene is not loaded.");
            return;
        }

        if (pickResult.Collider == null || !GodotObject.IsInstanceValid(pickResult.Collider))
        {
            LogHub.Warn("MeasurementUi: collider is invalid, skip annotation label placement.");
            return;
        }

        Node3D annotation = _annotationLabel.Instantiate<Node3D>();
        annotation.Name = $"MeasurementPoint{index + 1}";
        pickResult.Collider.AddChild(annotation);

        annotation.GlobalPosition = pickResult.Position;
        if (pickResult.Normal.LengthSquared() > Mathf.Epsilon)
        {
            // AnnotationLabel は +Z を前方として作成されているため useModelFront=true で法線方向へ向ける
            annotation.LookAt(annotation.GlobalPosition + pickResult.Normal.Normalized(), Vector3.Up, true);
        }

        SetAnnotationText(annotation, $"P{index + 1}");
        _annotationInstances[index] = annotation;
    }

    /// <summary>
    /// 指定した注釈ラベルにテキストを設定する
    /// </summary>
    /// <param name="annotation">注釈ラベルのノード</param>
    /// <param name="text">設定するテキスト</param>
    private static void SetAnnotationText(Node3D annotation, string text)
    {
        if (annotation.FindChild("Label3DLeft", true, false) is Label3D left)
        {
            left.Text = text;
        }

        if (annotation.FindChild("Label3DRight", true, false) is Label3D right)
        {
            right.Text = text;
        }
    }

    /// <summary>
    /// 指定したインデックスの注釈ラベルを破棄する
    /// </summary>
    /// <param name="index">破棄する注釈ラベルのインデックス</param>
    private void RemoveAnnotationLabel(int index)
    {
        Node3D annotation = _annotationInstances[index];
        if (annotation != null && GodotObject.IsInstanceValid(annotation))
        {
            annotation.QueueFree();
        }

        _annotationInstances[index] = null;
    }

    /// <summary>
    /// すべての注釈ラベルを破棄する
    /// </summary>
    private void ClearAnnotationLabels()
    {
        for (int i = 0; i < _annotationInstances.Length; i++)
        {
            RemoveAnnotationLabel(i);
        }
    }

    /// <summary>
    /// 測定ビジュアルをセットアップする
    /// </summary>
    private void SetupMeasurementVisuals()
    {
        Node sceneRoot = GetTree()?.CurrentScene;
        if (sceneRoot == null)
        {
            LogHub.Warn("MeasurementUi: current scene is null, skip measurement visual setup.");
            return;
        }

        _measurementVisualRoot = new Node3D
        {
            Name = "MeasurementVisualRoot"
        };
        sceneRoot.AddChild(_measurementVisualRoot);

        _measurementLine = new MeshInstance3D
        {
            Name = "MeasurementLine",
            Mesh = _measurementLineMesh,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
        };

        _measurementLine.MaterialOverride = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            AlbedoColor = new Color(0.98f, 0.82f, 0.12f, 1.0f)
        };

        _measurementVisualRoot.AddChild(_measurementLine);
        _measurementLine.Visible = false;
    }

    /// <summary>
    /// 測定線を更新する
    /// </summary>
    private void UpdateMeasurementLine()
    {
        if (_measurementLine == null || !GodotObject.IsInstanceValid(_measurementLine))
        {
            return;
        }

        _measurementLineMesh.ClearSurfaces();

        if (!(_points[0].HasHit && _points[1].HasHit))
        {
            _measurementLine.Visible = false;
            return;
        }

        _measurementLineMesh.SurfaceBegin(Mesh.PrimitiveType.Lines);
        _measurementLineMesh.SurfaceAddVertex(_points[0].Position);
        _measurementLineMesh.SurfaceAddVertex(_points[1].Position);
        _measurementLineMesh.SurfaceEnd();

        _measurementLine.Visible = true;
    }

    #endregion
}
