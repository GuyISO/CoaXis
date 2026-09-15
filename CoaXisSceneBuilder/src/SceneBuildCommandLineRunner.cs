using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

/// <summary>
/// コマンドラインで指定されたビルド要求 JSON を読み込み、SceneBuilder による .scn 生成を実行する本番用エントリポイント
/// </summary>
public partial class SceneBuildCommandLineRunner : Node
{
	private const string WorkerEnvironmentVariableName = "COAXIS_SCENEBUILDER_WORKER";

	#region Lifecycle

	/// <summary>
	/// コマンドライン引数で指定された入力 JSON を使ってシーン生成を実行し、結果を終了コードへ反映する
	/// </summary>
	public override void _Ready()
	{
		// 意図: 実行時の入口がCLI経由かD&D経由かに関わらず、まず入力ファイルの解決を一箇所で固定する。
		// 理由: どちらの起動形式でも、JSON 配列を 1 つのビルド要求群として扱う前提を統一するため。
		if (!TryResolveInputPath(out string inputPath, out string errorMessage))
		{
			PrintUsage(errorMessage);
			GetTree().Quit(1);
			return;
		}

		// 意図: JSON の配列を読み込んで複数リクエストをまとめて処理する。
		// 制約: 1 ファイルに複数モデルの出力先が入る場合でも、バッチ単位で失敗件数を集計できるようにする。
		if (!TryReadRequests(inputPath, out List<SceneBuildRequestDto> requests))
		{
			GetTree().Quit(1);
			return;
		}

		// 意図: 子プロセスとして起動された場合（分割済みチャンク）は再分割せず、常に逐次処理へ回す。
		// 理由: 無限にプロセスを分割・起動し続ける事態を避けるため。
		bool isWorkerProcess = System.Environment.GetEnvironmentVariable(WorkerEnvironmentVariableName) == "1";
		int maxParallelism = BuilderSettingsLoader.GetInstance().MaxParallelism;

		if (!isWorkerProcess && maxParallelism > 1 && requests.Count > 1)
		{
			if (SceneBuildProcessOrchestrator.TryRunParallel(requests, maxParallelism, out int parallelTotalCount, out int parallelFailedCount))
			{
				GD.Print($"SceneBuildCommandLineRunner: completed {parallelTotalCount} request(s) via parallel processes, failed={parallelFailedCount}.");
				GetTree().Quit(parallelFailedCount == 0 ? 0 : 1);
				return;
			}

			GD.PushWarning("SceneBuildCommandLineRunner: parallel orchestration failed, falling back to sequential execution.");
		}

		SceneBuilder sceneBuilder = GetNodeOrNull<SceneBuilder>("SceneBuilder");
		if (sceneBuilder == null)
		{
			GD.PushError("SceneBuildCommandLineRunner: SceneBuilder node is required.");
			GetTree().Quit(1);
			return;
		}

		int failedCount = 0;
		foreach (SceneBuildRequestDto request in requests)
		{
			if (!sceneBuilder.TryBuild(request))
			{
				failedCount++;
			}
		}

		if (isWorkerProcess)
		{
			// 意図: 親プロセス（オーケストレータ）が終了コードだけでは失敗件数を判別できないため、集計用の結果ファイルを残す。
			WriteWorkerResult(inputPath, requests.Count, failedCount);
		}

		// CLI 呼び出し元がバッチ全体の成否を判定できるよう、失敗件数の有無を終了コードへ反映する。
		GD.Print($"SceneBuildCommandLineRunner: completed {requests.Count} request(s), failed={failedCount}.");
		GetTree().Quit(failedCount == 0 ? 0 : 1);
	}

	#endregion

	#region Internal Helpers

	private static bool TryResolveInputPath(out string inputPath, out string errorMessage)
	{
		inputPath = null;
		errorMessage = null;
		// 意図: 実行形式がCLI引数かドラッグ&ドロップかで引数配列の取り出し方が違うため、最終的に1件だけを正規化して扱う。
		// 理由: ユーザーが誤って複数 JSON を渡した場合でも、どっちの形式でも1入力として汚く解釈しないため。
		string[] shellArguments = OS.GetCmdlineUserArgs();
		string[] inputCandidates = shellArguments.Length > 0
			? shellArguments
			: ExtractDraggedJsonPaths(OS.GetCmdlineArgs());

		// 意図: shell とD&Dのいずれでも、誤った複数ファイル指定を1件の入力へ曖昧に解釈しない。
		// 制約: ビルド要求JSONは配列を持てるため、複数モデルはJSON内へまとめて指定する。
		if (inputCandidates.Length != 1)
		{
			errorMessage = "Exactly one input JSON file is required.";
			return false;
		}

		string candidatePath = inputCandidates[0];
		if (!string.Equals(Path.GetExtension(candidatePath), ".json", StringComparison.OrdinalIgnoreCase))
		{
			errorMessage = $"Input file must have the .json extension. path='{candidatePath}'";
			return false;
		}

		if (!AssetPathResolver.TryResolveExistingFilePath(candidatePath, out inputPath))
		{
			errorMessage = $"Input JSON file was not found. path='{candidatePath}'";
			return false;
		}

		return true;
	}

	private static string[] ExtractDraggedJsonPaths(string[] commandLineArguments)
	{
		// 意図: D&D ではコマンドラインに .json だけが混ざる想定になりやすいため、JSON 形式の候補だけを抽出して正規化する。
		// 理由: そのまま全引数を入力ファイルとして扱うと、exe パスや追加オプションまで混ざって誤判定するため。
		var jsonPaths = new List<string>();
		foreach (string argument in commandLineArguments)
		{
			if (string.Equals(Path.GetExtension(argument), ".json", StringComparison.OrdinalIgnoreCase))
			{
				jsonPaths.Add(argument);
			}
		}

		return jsonPaths.ToArray();
	}

	private static void PrintUsage(string errorMessage)
	{
		GD.PushError($"SceneBuildCommandLineRunner: {errorMessage}");
		GD.Print("Usage (shell): CoaXisSceneBuilder.console.exe --headless -- \"C:\\path\\to\\input.json\"");
		GD.Print("Usage (drag and drop): Drop exactly one .json file onto CoaXisSceneBuilder.console.exe.");
	}

	private static bool TryReadRequests(string inputPath, out List<SceneBuildRequestDto> requests)
	{
		requests = null;
		string content = AssetPathResolver.ReadText(inputPath);
		if (string.IsNullOrWhiteSpace(content))
		{
			GD.PushError($"SceneBuildCommandLineRunner: failed to read input json. path='{inputPath}'");
			return false;
		}

		try
		{
			var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
			requests = JsonSerializer.Deserialize<List<SceneBuildRequestDto>>(content, options);
		}
		catch (JsonException exception)
		{
			GD.PushError($"SceneBuildCommandLineRunner: failed to parse input json. path='{inputPath}', error='{exception.Message}'");
			return false;
		}

		if (requests == null || requests.Count == 0 || requests.Exists(request => request == null))
		{
			GD.PushError($"SceneBuildCommandLineRunner: input json must contain at least one request. path='{inputPath}'");
			return false;
		}

		return true;
	}

	private static void WriteWorkerResult(string inputPath, int totalCount, int failedCount)
	{
		var result = new SceneBuildResultDto { TotalCount = totalCount, FailedCount = failedCount };
		try
		{
			File.WriteAllText(inputPath + ".result.json", JsonSerializer.Serialize(result));
		}
		catch (IOException exception)
		{
			GD.PushError($"SceneBuildCommandLineRunner: failed to write worker result file. path='{inputPath}.result.json', error='{exception.Message}'");
		}
	}

	#endregion
}