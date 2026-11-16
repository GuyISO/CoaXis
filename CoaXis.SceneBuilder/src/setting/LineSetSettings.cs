using System.Text.Json.Serialization;

/// <summary>
/// ラインセットのメッシュ生成精度およびサイズに関する設定データ構造
/// </summary>
public class LineSetSettings
{
	/// <summary>
	/// チューブメッシュの半径（メートル単位）
	/// </summary>
	[JsonPropertyName("TubeRadiusMeters")]
	public float TubeRadiusMeters { get; set; } = 0.002f;

	/// <summary>
	/// チューブ断面の分割数（円周のポリゴン精度）
	/// </summary>
	[JsonPropertyName("TubeRadialSegments")]
	public int TubeRadialSegments { get; set; } = 8;

	/// <summary>
	/// 最小セグメント長（メートル単位、これ未満の連続点は間引く）
	/// </summary>
	[JsonPropertyName("MinSegmentLengthMeters")]
	public float MinSegmentLengthMeters { get; set; } = 0.0001f;
}
