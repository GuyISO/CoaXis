/// <summary>
/// 並列ビルド時に子プロセスが親プロセスへ結果を伝えるための集計 DTO
/// </summary>
public sealed class SceneBuildResultDto
{
	/// <summary>
	/// このチャンクで処理したリクエスト件数を取得または設定する
	/// </summary>
	public int TotalCount { get; set; }

	/// <summary>
	/// このチャンクで失敗したリクエスト件数を取得または設定する
	/// </summary>
	public int FailedCount { get; set; }
}
