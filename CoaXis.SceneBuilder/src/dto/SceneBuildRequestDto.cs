/// <summary>
/// SceneBuilder が 1 つの PackedScene を生成するための入力データを表す DTO
/// </summary>
/// <remarks>
/// JSON の camelCase フィールド名とプロパティ名は、
/// System.Text.Json の大文字・小文字を区別しない既定設定で対応する。
/// </remarks>
public sealed class SceneBuildRequestDto
{
	/// <summary>
	/// 視覚表示用 GLB ファイルのパスを取得または設定する
	/// </summary>
	public string VisualGlb { get; set; } = string.Empty;

	/// <summary>
	/// 衝突判定用 GLB ファイルのパスを取得または設定する
	/// </summary>
	public string ColliderGlb { get; set; } = string.Empty;

	/// <summary>
	/// チューブメッシュ化するラインセット JSON ファイルのパスを取得または設定する
	/// </summary>
	public string LineSetJson { get; set; } = string.Empty;

	/// <summary>
	/// 生成した PackedScene の出力先 .scn ファイルパスを取得または設定する
	/// </summary>
	public string OutputScn { get; set; } = string.Empty;

	/// <summary>
	/// 視覚用 GLB のマテリアルを Unshaded にするかどうかを取得または設定する
	/// </summary>
	public bool VisualUnshaded { get; set; }
}