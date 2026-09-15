using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

/// <summary>
/// ビルド要求リストを並列プロセス数分のチャンクへ分割し、一時JSONファイルへ書き出すユーティリティ
/// </summary>
public static class SceneBuildRequestSplitter
{
	#region Public Methods

	/// <summary>
	/// リクエストリストを指定チャンク数へ均等分割する（余りは先頭チャンクから1件ずつ配分）
	/// </summary>
	/// <param name="requests">分割対象のリクエストリスト</param>
	/// <param name="chunkCount">分割数（リクエスト件数を超える場合は件数に丸める）</param>
	/// <returns>各チャンクのリクエストリストのリスト</returns>
	public static List<List<SceneBuildRequestDto>> Split(List<SceneBuildRequestDto> requests, int chunkCount)
	{
		int clampedChunkCount = Math.Max(1, Math.Min(chunkCount, requests.Count));
		var chunks = new List<List<SceneBuildRequestDto>>(clampedChunkCount);

		int baseSize = requests.Count / clampedChunkCount;
		int remainder = requests.Count % clampedChunkCount;
		int index = 0;
		for (int i = 0; i < clampedChunkCount; i++)
		{
			int size = baseSize + (i < remainder ? 1 : 0);
			var chunk = new List<SceneBuildRequestDto>(size);
			for (int j = 0; j < size; j++)
			{
				chunk.Add(requests[index]);
				index++;
			}
			chunks.Add(chunk);
		}

		return chunks;
	}

	/// <summary>
	/// 分割済みチャンクを一時ディレクトリ配下へ個別のJSONファイルとして書き出す
	/// </summary>
	/// <param name="chunks">書き出し対象のチャンクリスト</param>
	/// <param name="tempDirectory">書き出し先の一時ディレクトリ（存在しない場合は作成する）</param>
	/// <returns>書き出したチャンクJSONファイルの絶対パス一覧（chunksと同じ順序）</returns>
	public static List<string> WriteChunksToTempFiles(List<List<SceneBuildRequestDto>> chunks, string tempDirectory)
	{
		Directory.CreateDirectory(tempDirectory);

		var chunkPaths = new List<string>(chunks.Count);
		for (int i = 0; i < chunks.Count; i++)
		{
			string chunkPath = Path.Combine(tempDirectory, $"chunk_{i}.json");
			File.WriteAllText(chunkPath, JsonSerializer.Serialize(chunks[i]));
			chunkPaths.Add(chunkPath);
		}

		return chunkPaths;
	}

	#endregion
}
