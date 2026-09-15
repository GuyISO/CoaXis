using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

/// <summary>
/// ビルド要求を複数チャンクへ分割し、自プロセスを子プロセスとして並列起動して集計するオーケストレータ
/// </summary>
/// <remarks>
/// Godot のノード生成・PackedScene.Pack()・ResourceSaver.Save() は main thread 専用APIのため、
/// 同一プロセス内の Task 並列化では効果が出ない。そのためプロセス単位で並列化し、各子プロセスが
/// 独立した main thread でビルドを行う。
/// </remarks>
public static class SceneBuildProcessOrchestrator
{
	private const string WorkerEnvironmentVariableName = "COAXIS_SCENEBUILDER_WORKER";
	private const string WorkerEnvironmentVariableValue = "1";

	#region Public Methods

	/// <summary>
	/// リクエストを分割し、子プロセスを並列起動してビルドを実行、結果を集計する
	/// </summary>
	/// <param name="requests">ビルド対象の全リクエスト</param>
	/// <param name="maxParallelism">同時起動する子プロセス数の上限</param>
	/// <param name="totalCount">集計された処理件数</param>
	/// <param name="failedCount">集計された失敗件数</param>
	/// <returns>並列実行に成功した場合はtrue。自プロセスパスが取得できない等で実行できなかった場合はfalse</returns>
	public static bool TryRunParallel(List<SceneBuildRequestDto> requests, int maxParallelism, out int totalCount, out int failedCount)
	{
		totalCount = 0;
		failedCount = 0;

		string executablePath = OS.GetExecutablePath();
		if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
		{
			GD.PushError("SceneBuildProcessOrchestrator: failed to resolve own executable path for child process spawning.");
			return false;
		}

		string tempDirectory = Path.Combine(Path.GetTempPath(), "CoaXisSceneBuilder", $"batch_{Guid.NewGuid():N}");
		List<List<SceneBuildRequestDto>> chunks = SceneBuildRequestSplitter.Split(requests, maxParallelism);
		List<string> chunkPaths = SceneBuildRequestSplitter.WriteChunksToTempFiles(chunks, tempDirectory);

		var processes = new List<Process>(chunkPaths.Count);
		try
		{
			for (int i = 0; i < chunkPaths.Count; i++)
			{
				processes.Add(StartChunkProcess(executablePath, chunkPaths[i], i));
			}

			foreach (Process process in processes)
			{
				process.WaitForExit();
			}

			for (int i = 0; i < chunkPaths.Count; i++)
			{
				AggregateChunkResult(chunkPaths[i], chunks[i].Count, ref totalCount, ref failedCount);
			}
		}
		finally
		{
			foreach (Process process in processes)
			{
				process.Dispose();
			}
			CleanUpTempDirectory(tempDirectory);
		}

		return true;
	}

	#endregion

	#region Internal Helpers

	private static Process StartChunkProcess(string executablePath, string chunkPath, int chunkIndex)
	{
		var startInfo = new ProcessStartInfo
		{
			FileName = executablePath,
			UseShellExecute = false,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			CreateNoWindow = true
		};
		startInfo.ArgumentList.Add("--headless");
		startInfo.ArgumentList.Add("--");
		startInfo.ArgumentList.Add(chunkPath);
		// 意図: 子プロセスへ「これは分割済みチャンクなので再分割しない」ことを伝え、無限分割を防ぐ。
		startInfo.EnvironmentVariables[WorkerEnvironmentVariableName] = WorkerEnvironmentVariableValue;

		var process = new Process { StartInfo = startInfo, EnableRaisingEvents = false };
		process.OutputDataReceived += (_, e) =>
		{
			if (e.Data != null)
			{
				GD.Print($"[chunk{chunkIndex}] {e.Data}");
			}
		};
		process.ErrorDataReceived += (_, e) =>
		{
			if (e.Data != null)
			{
				GD.PushError($"[chunk{chunkIndex}] {e.Data}");
			}
		};

		process.Start();
		process.BeginOutputReadLine();
		process.BeginErrorReadLine();
		return process;
	}

	private static void AggregateChunkResult(string chunkPath, int chunkRequestCount, ref int totalCount, ref int failedCount)
	{
		string resultPath = chunkPath + ".result.json";
		if (!File.Exists(resultPath))
		{
			// 意図: 子プロセスがクラッシュ等で結果を書き出せなかった場合、そのチャンク全件を失敗扱いにして集計漏れを防ぐ。
			GD.PushError($"SceneBuildProcessOrchestrator: result file missing, treating chunk as failed. path='{resultPath}'");
			totalCount += chunkRequestCount;
			failedCount += chunkRequestCount;
			return;
		}

		try
		{
			var result = JsonSerializer.Deserialize<SceneBuildResultDto>(File.ReadAllText(resultPath));
			totalCount += result.TotalCount;
			failedCount += result.FailedCount;
		}
		catch (JsonException exception)
		{
			GD.PushError($"SceneBuildProcessOrchestrator: failed to parse result file. path='{resultPath}', error='{exception.Message}'");
			totalCount += chunkRequestCount;
			failedCount += chunkRequestCount;
		}
	}

	private static void CleanUpTempDirectory(string tempDirectory)
	{
		try
		{
			if (Directory.Exists(tempDirectory))
			{
				Directory.Delete(tempDirectory, recursive: true);
			}
		}
		catch (IOException exception)
		{
			GD.PushWarning($"SceneBuildProcessOrchestrator: failed to clean up temp directory. path='{tempDirectory}', error='{exception.Message}'");
		}
	}

	#endregion
}
