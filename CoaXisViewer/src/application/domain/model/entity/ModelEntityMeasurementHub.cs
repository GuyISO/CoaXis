using Godot;

/// <summary>
/// 測定状態、測定結果、測定用ビジュアルを管理するハブ。
/// </summary>
public partial class ModelEntityMeasurementHub : BaseHub
{
    #region Fields

    /// <summary>計測点に表示するラベルのインスタンス。</summary>
    private readonly PointerLabel[] _pointerLabelInstances = new PointerLabel[2];
    /// <summary>計測線の描画メッシュ。</summary>
    private readonly ImmediateMesh _lineMesh = new ImmediateMesh();

    /// <summary>計測の始点・終点のピック結果。</summary>
    private readonly PickResult[] _points = new PickResult[2] { new PickResult(), new PickResult() };

    /// <summary>計測表示用ノードのルート。</summary>
    private Node3D _visualRoot = null!;
    /// <summary>計測線を描画するメッシュインスタンス。</summary>
    private MeshInstance3D _line = null!;
    /// <summary>計測線のマテリアル。</summary>
    private StandardMaterial3D _lineMaterial = null!;

    #endregion

    #region Properties

    /// <summary>現在測定対象としているポイントのインデックスを取得する。0は未選択、1と2は各測定ポイントを示す。</summary>
    public int CurrentPointIndex { get; private set; } = 0;
    /// <summary>現在の計測結果を取得する。</summary>
    public MeasurementResult CurrentResult => GetCurrentResult();

    #endregion

    #region Signals

    /// <summary>測定対象ポイントの変更通知。</summary>
    [Signal] public delegate void PointNotifiedEventHandler();

    /// <summary>測定結果の変更通知。</summary>
    [Signal] public delegate void ResultNotifiedEventHandler();

    #endregion

    #region Lifecycle

    public override void _Ready()
    {
        SubscribeEvents();
        EnsureMeasurementVisuals();
        ApplySettings();
    }

    public override void _ExitTree()
    {
        UnsubscribeEvents();
        ClearPointerLabels();

        if (_visualRoot != null && GodotObject.IsInstanceValid(_visualRoot))
        {
            _visualRoot.QueueFree();
            _visualRoot = null;
            _line = null;
        }

        base._ExitTree();
    }

    #endregion

    #region Events

    /// <summary>
    /// Applicationイベントの購読を開始する。
    /// </summary>
    private void SubscribeEvents()
    {
        Application.Model.Entity.Pick.HandlingModeNotified += OnModelEntityPickHandlingModeNotified;
        Application.Model.Entity.Pick.ResultNotified += OnModelEntityPickResultNotified;
        Application.Setting.SettingsNotified += ApplySettings;
    }

    /// <summary>
    /// Applicationイベントの購読を解除する。
    /// </summary>
    private void UnsubscribeEvents()
    {
        Application.Model.Entity.Pick.HandlingModeNotified -= OnModelEntityPickHandlingModeNotified;
        Application.Model.Entity.Pick.ResultNotified -= OnModelEntityPickResultNotified;
        Application.Setting.SettingsNotified -= ApplySettings;
    }

    /// <summary>
    /// ピック操作モードの変更通知を受け取ったときに呼び出されるイベントハンドラ。
    /// </summary>
    private void OnModelEntityPickHandlingModeNotified()
    {
        if (Application.Model.Entity.Pick.HandlingMode != PickHandlingMode.Measurement)
        {
            CurrentPointIndex = 0;
            EmitSignal(SignalName.PointNotified);
        }
    }

    /// <summary>
    /// ピック結果が通知されたときに呼び出されるイベントハンドラ。
    /// </summary>
    /// <param name="pickResult">通知されたピック結果</param>
    private void OnModelEntityPickResultNotified(PickResult pickResult)
    {
        if (Application.Model.Entity.Pick.HandlingMode != PickHandlingMode.Measurement)
        {
            return;
        }

        if (CurrentPointIndex == 0)
        {
            return;
        }

        if (!pickResult.HasHit)
        {
            return;
        }

        int index = CurrentPointIndex - 1;
        _points[index] = pickResult;
        Application.Log.Debug($"MeasurementService: point {CurrentPointIndex} picked. Position: {_points[index].Position}, Normal: {_points[index].Normal}, Distance: {_points[index].Distance}");

        EnsureMeasurementVisuals();
        UpdatePointerLabel(index, pickResult);
        UpdateMeasurementLine();
        EmitSignal(SignalName.ResultNotified);
    }

    #endregion

    #region Methods

    /// <summary>
    /// 測定ポイントのピックを開始する。
    /// </summary>
    /// <param name="pointIndex">設定するポイントのインデックス（1または2）</param>
    public void SetPoint(int pointIndex)
    {
        if (pointIndex is < 1 or > 2)
        {
            Application.Log.Warn($"MeasurementService: invalid point index {pointIndex}.");
            return;
        }

        CurrentPointIndex = pointIndex;

        Application.Model.Entity.Pick.SetHandlingMode(PickHandlingMode.Measurement);
        EmitSignal(SignalName.PointNotified);
    }

    /// <summary>
    /// 測定ポイントをクリアする。
    /// </summary>
    /// <param name="pointIndex">クリアするポイントのインデックス（1または2）</param>
    public void ClearPoint(int pointIndex)
    {
        if (pointIndex is < 1 or > 2)
        {
            Application.Log.Warn($"MeasurementService: invalid point index {pointIndex}.");
            return;
        }

        int index = pointIndex - 1;
        _points[index] = new PickResult();
        RemovePointerLabel(index);
        UpdateMeasurementLine();
        EmitSignal(SignalName.ResultNotified);
    }

    /// <summary>
    /// 現在の測定結果を取得する。
    /// </summary>
    /// <returns>現在の測定結果</returns>
    public MeasurementResult GetCurrentResult()
    {
        return ComputeMeasurementResult();
    }

    #endregion

    #region Helpers

    /// <summary>
    /// 設定値を反映する。
    /// </summary>
    private void ApplySettings()
    {
        if (_lineMaterial == null || !GodotObject.IsInstanceValid(_lineMaterial))
        {
            return;
        }

        _lineMaterial.AlbedoColor = Color.FromHtml(Application.Setting.Current.Color.MeasurementLineColor);
    }

    /// <summary>
    /// 現在の測定結果を計算する。
    /// </summary>
    /// <returns>現在の測定結果</returns>
    private MeasurementResult ComputeMeasurementResult()
    {
        Vector3 position1 = Vector3.Zero;
        Vector3 position2 = Vector3.Zero;
        Vector3 normal1 = Vector3.Zero;
        Vector3 normal2 = Vector3.Zero;
        Vector3 delta = Vector3.Zero;
        float distance = float.NaN;
        float angle = float.NaN;

        if (_points[0].HasHit)
        {
            position1 = _points[0].Position;
            normal1 = _points[0].Normal;
        }

        if (_points[1].HasHit)
        {
            position2 = _points[1].Position;
            normal2 = _points[1].Normal;
        }

        if (_points[0].HasHit && _points[1].HasHit)
        {
            delta = position2 - position1;
            distance = position1.DistanceTo(position2);
            angle = ComputeNormalAngleDegrees(_points[0].Normal, _points[1].Normal);
        }

        return new MeasurementResult(
            _points[0].HasHit,
            _points[1].HasHit,
            position1,
            position2,
            normal1,
            normal2,
            distance,
            angle,
            delta);
    }

    /// <summary>
    /// 2つの法線ベクトルのなす角を度単位で計算する。
    /// </summary>
    /// <param name="normal1">法線ベクトル1</param>
    /// <param name="normal2">法線ベクトル2</param>
    /// <returns>2つの法線ベクトルのなす角（度単位）</returns>
    private static float ComputeNormalAngleDegrees(Vector3 normal1, Vector3 normal2)
    {
        if (normal1.LengthSquared() <= Mathf.Epsilon || normal2.LengthSquared() <= Mathf.Epsilon)
        {
            return float.NaN;
        }

        float dot = Mathf.Clamp(normal1.Normalized().Dot(normal2.Normalized()), -1.0f, 1.0f);
        return Mathf.RadToDeg(Mathf.Acos(dot));
    }

    /// <summary>
    /// 指定したインデックスのポイントラベルを更新する。
    /// </summary>
    /// <param name="index">更新するポイントラベルのインデックス（0または1）</param>
    /// <param name="pickResult">ポイントラベルの位置と法線を決定するピック結果</param> 
    private void UpdatePointerLabel(int index, PickResult pickResult)
    {
        RemovePointerLabel(index);

        if (!pickResult.HasHit)
        {
            return;
        }

        if (pickResult.Collider == null || !GodotObject.IsInstanceValid(pickResult.Collider))
        {
            Application.Log.Warn("MeasurementService: collider is invalid, skip pointer label placement.");
            return;
        }

        PointerLabel pointerLabel = Application.Asset.Scene.GetPointerLabel();
        AddChild(pointerLabel);

        pointerLabel.Name = $"MeasurementPoint{index + 1}";
        pointerLabel.GlobalPosition = pickResult.Position;
        pointerLabel.SetOrientationFromNormal(pickResult.Normal);
        pointerLabel.SetText($"Point{index + 1}");

        _pointerLabelInstances[index] = pointerLabel;
    }

    /// <summary>
    /// 指定したインデックスのポイントラベルを削除する。
    /// </summary>
    /// <param name="index">削除するポイントラベルのインデックス（0または1）</param>
    private void RemovePointerLabel(int index)
    {
        PointerLabel pointerLabel = _pointerLabelInstances[index];
        if (pointerLabel != null && GodotObject.IsInstanceValid(pointerLabel))
        {
            pointerLabel.QueueFree();
        }

        _pointerLabelInstances[index] = null;
    }

    /// <summary>
    /// すべてのポイントラベルを削除する。
    /// </summary>
    private void ClearPointerLabels()
    {
        for (int i = 0; i < _pointerLabelInstances.Length; i++)
        {
            RemovePointerLabel(i);
        }
    }

    /// <summary>
    /// 測定用のビジュアルノードをシーンに追加する。
    /// </summary>
    private void EnsureMeasurementVisuals()
    {
        if (_visualRoot != null && GodotObject.IsInstanceValid(_visualRoot))
        {
            return;
        }

        _visualRoot = new Node3D
        {
            Name = "MeasurementVisualRoot"
        };
        AddChild(_visualRoot);

        _line = new MeshInstance3D
        {
            Name = "MeasurementLine",
            Mesh = _lineMesh,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
        };

        _line.MaterialOverride = _lineMaterial = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            AlbedoColor = Color.FromHtml(Application.Setting.Current.Color.MeasurementLineColor)
        };

        _visualRoot.AddChild(_line);
        _line.Visible = false;
    }

    /// <summary>
    /// 測定用のラインを更新する。
    /// </summary>
    private void UpdateMeasurementLine()
    {
        if (_line == null || !GodotObject.IsInstanceValid(_line))
        {
            return;
        }

        _lineMesh.ClearSurfaces();

        if (!(_points[0].HasHit && _points[1].HasHit))
        {
            _line.Visible = false;
            return;
        }

        _lineMesh.SurfaceBegin(Mesh.PrimitiveType.Lines);
        _lineMesh.SurfaceAddVertex(_points[0].Position);
        _lineMesh.SurfaceAddVertex(_points[1].Position);
        _lineMesh.SurfaceEnd();

        _line.Visible = true;
    }

    #endregion
}
