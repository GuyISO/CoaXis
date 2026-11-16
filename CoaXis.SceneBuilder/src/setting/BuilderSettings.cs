using System.Text.Json.Serialization;

/// <summary>
/// SceneBuilder 全体のビルド設定データ構造
/// </summary>
public class BuilderSettings
{
	/// <summary>
	/// GLBモデル変換設定
	/// </summary>
	[JsonPropertyName("GlbTransform")]
	public GlbTransformSettings GlbTransform { get; set; } = new GlbTransformSettings();

	/// <summary>
	/// ラインセットメッシュ設定
	/// </summary>
	[JsonPropertyName("LineSet")]
	public LineSetSettings LineSet { get; set; } = new LineSetSettings();

	/// <summary>
	/// CLI本番実行時に同時起動する子プロセス数の上限（1以下の場合は逐次実行）
	/// </summary>
	[JsonPropertyName("MaxParallelism")]
	public int MaxParallelism { get; set; } = 1;
}
