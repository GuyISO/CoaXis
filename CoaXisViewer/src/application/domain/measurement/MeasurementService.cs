using Godot;
using System;

/// <summary>
/// 測定機能の状態管理・計算・ビジュアル更新を担当するサービス
/// </summary>
public partial class MeasurementService : Node
{
    #region Fields

    // 現在測定対象とするポイントのインデックス（1または2）。0の場合は未選択状態を示す
    private int _pointIndex = 0; // 0: 未選択、1: ポイント1、2: ポイント2

    private readonly PackedScene _pointerLabel = ResourceLoader.Load<PackedScene>("res://scenes/Part/PointerLabel.tscn")!;
    private readonly PointerLabel[] _pointerLabelInstances = new PointerLabel[2];
    private readonly ImmediateMesh _lineMesh = new ImmediateMesh();

    private readonly PickResult[] _points = new PickResult[2] { new PickResult(), new PickResult() };

    private Node3D _visualRoot = null!;
    private MeshInstance3D _line = null!;

    #endregion

    #region Lifecycle

    public override void _Ready()
    {
        SubscribeEvents();
        EnsureMeasurementVisuals();
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
    /// Applicationイベントの購読を開始する
    /// </summary>
    private void SubscribeEvents()
    {
        Application.Pick.Event.HandlingModeNotified += OnPickHandlingModeNotified;
        Application.Pick.Event.ResultNotified += OnPickResultNotified;
        Application.Measurement.Event.AskResultRequested += OnAskResultRequested;
        Application.Measurement.Event.SetPointRequested += OnSetPointRequested;
    }

    /// <summary>
    /// Applicationイベントの購読を解除する
    /// </summary>
    private void UnsubscribeEvents()
    {
        Application.Pick.Event.HandlingModeNotified -= OnPickHandlingModeNotified;
        Application.Pick.Event.ResultNotified -= OnPickResultNotified;
        Application.Measurement.Event.AskResultRequested -= OnAskResultRequested;
        Application.Measurement.Event.SetPointRequested -= OnSetPointRequested;
    }

    /// <summary>
    /// 測定結果の通知がリクエストされたときに呼び出されるイベントハンドラ
    /// </summary>
    private void OnAskResultRequested()
    {
        Application.Measurement.Event.NotifyResult(GetCurrentResult());
    }

    /// <summary>
    /// 測定対象ポイントの設定がリクエストされたときに呼び出されるイベントハンドラ
    /// </summary>
    /// <param name="pointIndex">設定するポイントのインデックス（1または2）。0の場合は未選択状態を示す</param>
    private void OnSetPointRequested(int pointIndex)
    {
        if (pointIndex is < 1 or > 2)
        {
            Application.Log.Service.Warn($"MeasurementService: invalid point index {pointIndex}.");
            return;
        }

        _pointIndex = pointIndex;
        
        Application.Pick.Event.SetHandlingMode(PickHandlingMode.Measurement);
        Application.Measurement.Event.NotifyPoint(pointIndex);
    }

    /// <summary>
    /// ピック操作モードの通知がリクエストされたときに呼び出されるイベントハンドラ
    /// </summary>
    /// <param name="mode">通知されたピック操作モード</param>
    private void OnPickHandlingModeNotified(PickHandlingMode mode)
    {
        if (mode != PickHandlingMode.Measurement)
        {
            _pointIndex = 0;
            Application.Measurement.Event.NotifyPoint(0);
        }
    }

    /// <summary>
    /// ピック結果の通知がリクエストされたときに呼び出されるイベントハンドラ
    /// </summary>
    /// <param name="pickResult">通知されたピック結果</param>
    private void OnPickResultNotified(PickResult pickResult)
    {
        if (Application.Pick.Service.HandlingMode != PickHandlingMode.Measurement)
        {
            return;
        }

        if (_pointIndex == 0)
        {
            return;
        }

        if (pickResult == null || pickResult.Model == null)
        {
            return;
        }

        int index = _pointIndex - 1;
        _points[index] = pickResult;
        Application.Log.Service.Debug($"MeasurementService: point {_pointIndex} picked. Position: {_points[index].Position}, Normal: {_points[index].Normal}, Distance: {_points[index].Distance}");

        EnsureMeasurementVisuals();
        UpdatePointerLabel(index, pickResult);
        UpdateMeasurementLine();
        Application.Measurement.Event.NotifyResult(GetCurrentResult());
    }

    #endregion

    #region Public API

    /// <summary>
    /// 現在の測定結果を取得する
    /// </summary>
    /// <returns>現在の測定結果</returns>
    internal MeasurementResult GetCurrentResult()
    {
        return ComputeMeasurementResult();
    }

    #endregion

    #region Internal Helpers

    /// <summary>
    /// 現在の測定結果を計算する
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
    /// 2つの法線ベクトルのなす角を度単位で計算する
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
    /// 指定したインデックスのポイントラベルを更新する
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

        if (_pointerLabel == null)
        {
            Application.Log.Service.Warn("MeasurementService: pointer label scene is not loaded.");
            return;
        }

        if (pickResult.Collider == null || !GodotObject.IsInstanceValid(pickResult.Collider))
        {
            Application.Log.Service.Warn("MeasurementService: collider is invalid, skip pointer label placement.");
            return;
        }

        PointerLabel pointerLabel = _pointerLabel.Instantiate<PointerLabel>();
        AddChild(pointerLabel);

        pointerLabel.Name = $"MeasurementPoint{index + 1}";
        pointerLabel.GlobalPosition = pickResult.Position;
        pointerLabel.SetOrientationFromNormal(pickResult.Normal);
        pointerLabel.SetText($"Point{index + 1}");

        _pointerLabelInstances[index] = pointerLabel;
    }

    /// <summary>
    /// 指定したインデックスのポイントラベルを削除する
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
    /// すべてのポイントラベルを削除する
    /// </summary>
    private void ClearPointerLabels()
    {
        for (int i = 0; i < _pointerLabelInstances.Length; i++)
        {
            RemovePointerLabel(i);
        }
    }

    /// <summary>
    /// 測定用のビジュアルノードをシーンに追加する
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

        _line.MaterialOverride = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            AlbedoColor = new Color(0.98f, 0.82f, 0.12f, 1.0f)
        };

        _visualRoot.AddChild(_line);
        _line.Visible = false;
    }

    /// <summary>
    /// 測定用のラインを更新する
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
