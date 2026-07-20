using Godot;

/// <summary>
/// 測定結果を表すオブジェクト
/// </summary>
public partial class MeasurementResult : RefCounted
{
    public bool HasPoint1 { get; }
    public bool HasPoint2 { get; }

    public Vector3 Position1 { get; }
    public Vector3 Position2 { get; }

    public Vector3 Normal1 { get; }
    public Vector3 Normal2 { get; }

    public float Distance { get; }
    public float Angle { get; }

    public Vector3 Delta { get; }

    /// <summary>
    /// 未測定状態を表す MeasurementResult を初期化する
    /// </summary>
    public MeasurementResult()
        : this(false, false, Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero, float.NaN, float.NaN, Vector3.Zero)
    {
    }

    /// <summary>
    /// 完全初期化コンストラクタ
    /// </summary>
    /// <param name="hasPoint1">ポイント1が有効かどうか</param>
    /// <param name="hasPoint2">ポイント2が有効かどうか</param>
    /// <param name="position1">ポイント1の位置</param>
    /// <param name="position2">ポイント2の位置</param>
    /// <param name="normal1">ポイント1の法線</param>
    /// <param name="normal2">ポイント2の法線</param>
    /// <param name="distance">ポイント間距離</param>
    /// <param name="angle">法線角度</param>
    /// <param name="delta">ポイント間差分</param>
    internal MeasurementResult(
        bool hasPoint1,
        bool hasPoint2,
        Vector3 position1,
        Vector3 position2,
        Vector3 normal1,
        Vector3 normal2,
        float distance,
        float angle,
        Vector3 delta)
    {
        HasPoint1 = hasPoint1;
        HasPoint2 = hasPoint2;
        Position1 = position1;
        Position2 = position2;
        Normal1 = normal1;
        Normal2 = normal2;
        Distance = distance;
        Angle = angle;
        Delta = delta;
    }
}