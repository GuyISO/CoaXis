using Godot;
using System;

/// <summary>
/// 測定機能の状態管理・計算・ビジュアル更新を担当するサービス
/// </summary>
public partial class MeasurementService : Node
{
    #region Fields

    private bool _isInitialized = false;
    private PickHandlingMode _pickHandlingMode;
    private int _pickingPointIndex = 0; // 0: 未選択、1: ポイント1、2: ポイント2

    private readonly PackedScene _pointerLabel = ResourceLoader.Load<PackedScene>("res://scenes/Part/PointerLabel.tscn")!;
    private readonly PointerLabel[] _pointerLabelInstances = new PointerLabel[2];
    private readonly ImmediateMesh _measurementLineMesh = new ImmediateMesh();

    private readonly PickResult[] _points = new PickResult[2] { new PickResult(), new PickResult() };

    private Node3D _measurementVisualRoot = null!;
    private MeshInstance3D _measurementLine = null!;

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

        if (_measurementVisualRoot != null && GodotObject.IsInstanceValid(_measurementVisualRoot))
        {
            _measurementVisualRoot.QueueFree();
            _measurementVisualRoot = null;
            _measurementLine = null;
        }

        base._ExitTree();
    }

    public override void _Process(double delta)
    {
        if (!_isInitialized)
        {
            Application.Pick.AskPickHandlingMode();
        }

        EnsureMeasurementVisuals();
    }

    #endregion

    #region Events

    #endregion

    #region Internal Helpers

    private void SubscribeEvents()
    {
        Application.Pick.PickHandlingModeNotified += OnPickHandlingModeNotified;
        Application.Pick.PickResultNotified += OnPickResultNotified;

        Application.Measurement.Event.AskMeasurementResultRequested += OnAskMeasurementResultRequested;
        Application.Measurement.Event.SetPickPointRequested += OnPickPointRequested;
    }

    private void UnsubscribeEvents()
    {
        Application.Pick.PickHandlingModeNotified -= OnPickHandlingModeNotified;
        Application.Pick.PickResultNotified -= OnPickResultNotified;

        Application.Measurement.Event.AskMeasurementResultRequested -= OnAskMeasurementResultRequested;
        Application.Measurement.Event.SetPickPointRequested -= OnPickPointRequested;
    }

    private void OnAskMeasurementResultRequested()
    {
        Application.Measurement.NotifyMeasurementResult(GetCurrentResult());
    }

    private void OnPickPointRequested(int pointIndex)
    {
        if (pointIndex is < 1 or > 2)
        {
            Application.Log.Warn($"MeasurementService: invalid point index {pointIndex}.");
            return;
        }

        Application.Pick.NotifyPickHandlingMode(PickHandlingMode.Measurement);
        _pickingPointIndex = pointIndex;
    }

    private void OnPickHandlingModeNotified(PickHandlingMode mode)
    {
        _isInitialized = true;
        _pickHandlingMode = mode;

        if (mode != PickHandlingMode.Measurement)
        {
            _pickingPointIndex = 0;
        }
    }

    private void OnPickResultNotified(PickResult pickResult)
    {
        if (_pickHandlingMode != PickHandlingMode.Measurement)
        {
            return;
        }

        if (_pickingPointIndex == 0)
        {
            return;
        }

        if (pickResult == null || pickResult.Model == null)
        {
            return;
        }

        int index = _pickingPointIndex - 1;
        _points[index] = pickResult;
        Application.Log.Debug($"MeasurementService: point {_pickingPointIndex} picked. Position: {_points[index].Position}, Normal: {_points[index].Normal}, Distance: {_points[index].Distance}");

        EnsureMeasurementVisuals();
        UpdatePointerLabel(index, pickResult);
        UpdateMeasurementLine();
        Application.Measurement.NotifyMeasurementResult(GetCurrentResult());
    }

    internal MeasurementResult GetCurrentResult()
    {
        return ComputeMeasurementResult();
    }

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
            position1 = CoordinateSystemUtility.GodotToCatia(_points[0].Position) * 1000.0f;
            normal1 = CoordinateSystemUtility.GodotToCatia(_points[0].Normal);
        }

        if (_points[1].HasHit)
        {
            position2 = CoordinateSystemUtility.GodotToCatia(_points[1].Position) * 1000.0f;
            normal2 = CoordinateSystemUtility.GodotToCatia(_points[1].Normal);
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

    private static float ComputeNormalAngleDegrees(Vector3 normal1, Vector3 normal2)
    {
        if (normal1.LengthSquared() <= Mathf.Epsilon || normal2.LengthSquared() <= Mathf.Epsilon)
        {
            return float.NaN;
        }

        float dot = Mathf.Clamp(normal1.Normalized().Dot(normal2.Normalized()), -1.0f, 1.0f);
        return Mathf.RadToDeg(Mathf.Acos(dot));
    }

    private void UpdatePointerLabel(int index, PickResult pickResult)
    {
        RemovePointerLabel(index);

        if (!pickResult.HasHit)
        {
            return;
        }

        if (_pointerLabel == null)
        {
            Application.Log.Warn("MeasurementService: pointer label scene is not loaded.");
            return;
        }

        if (pickResult.Collider == null || !GodotObject.IsInstanceValid(pickResult.Collider))
        {
            Application.Log.Warn("MeasurementService: collider is invalid, skip pointer label placement.");
            return;
        }

        PointerLabel pointerLabel = _pointerLabel.Instantiate<PointerLabel>();
        pointerLabel.Name = $"MeasurementPoint{index + 1}";
        pickResult.Collider.AddChild(pointerLabel);

        pointerLabel.GlobalPosition = pickResult.Position;
        if (pickResult.Normal.LengthSquared() > Mathf.Epsilon)
        {
            pointerLabel.LookAt(pointerLabel.GlobalPosition + pickResult.Normal.Normalized(), Vector3.Up, true);
        }

        pointerLabel.SetText($"Point{index + 1}");
        _pointerLabelInstances[index] = pointerLabel;
    }

    private void RemovePointerLabel(int index)
    {
        PointerLabel pointerLabel = _pointerLabelInstances[index];
        if (pointerLabel != null && GodotObject.IsInstanceValid(pointerLabel))
        {
            pointerLabel.QueueFree();
        }

        _pointerLabelInstances[index] = null;
    }

    private void ClearPointerLabels()
    {
        for (int i = 0; i < _pointerLabelInstances.Length; i++)
        {
            RemovePointerLabel(i);
        }
    }

    private void EnsureMeasurementVisuals()
    {
        if (_measurementVisualRoot != null && GodotObject.IsInstanceValid(_measurementVisualRoot))
        {
            return;
        }

        Node sceneRoot = GetTree()?.CurrentScene;
        if (sceneRoot == null)
        {
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
