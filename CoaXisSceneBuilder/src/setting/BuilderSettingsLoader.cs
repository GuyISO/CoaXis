using Godot;
using System;
using System.IO;
using System.Text.Json;

/// <summary>
/// 外部JSONファイルからの SceneBuilder 設定（モデル変換・ラインセット等）の読み込みと保持を担当するクラス
/// </summary>
public static class BuilderSettingsLoader
{
	#region Fields

	private const string SettingsFileName = "builder-settings.json";
	private static BuilderSettings _cachedSettings;

	#endregion

	#region Public Methods

	/// <summary>
	/// 外部JSONファイルからビルド設定をロードする。存在しない・失敗した場合はデフォルト値を返す
	/// </summary>
	/// <param name="customPath">指定のJSONファイルパス（未指定の場合はデフォルト検索パスを使用）</param>
	/// <returns>ビルド設定オブジェクト</returns>
	public static BuilderSettings Load(string customPath = null)
	{
		// 意図: 指定パスがあれば優先し、無ければ実行環境に応じた標準設定パスを探索する
		// 理由: export_presets.cfg で settings/* を PCK から除外しているため、
		//       配布版では res:// 内に設定が存在せず、exe と同じディレクトリを見に行く必要がある
		string path = !string.IsNullOrWhiteSpace(customPath) ? customPath : ResolveDefaultSettingsPath();

		string jsonText = AssetPathResolver.ReadText(path);
		if (string.IsNullOrWhiteSpace(jsonText))
		{
			// 意図: ファイルが存在しない場合でも例外で落とさず既定値で動作を継続する
			// 制約: 設定ファイルなしで動作する場合も既存のデフォルト値を維持する
			GD.Print($"BuilderSettingsLoader: Settings file not found at '{path}'. Using default settings.");
			return new BuilderSettings();
		}

		try
		{
			var options = new JsonSerializerOptions
			{
				PropertyNameCaseInsensitive = true,
				ReadCommentHandling = JsonCommentHandling.Skip,
				AllowTrailingCommas = true
			};

			BuilderSettings settings = JsonSerializer.Deserialize<BuilderSettings>(jsonText, options);
			if (settings != null)
			{
				settings.GlbTransform ??= new GlbTransformSettings();
				settings.LineSet ??= new LineSetSettings();
				GD.Print($"BuilderSettingsLoader: Successfully loaded settings from '{path}'.");
				return settings;
			}
		}
		catch (Exception ex)
		{
			// 意図: パース失敗時などのログ出力を行い、デフォルト設定に安全にフォールバックする
			GD.PushError($"BuilderSettingsLoader: Failed to load settings from '{path}'. Exception: {ex.Message}");
		}

		return new BuilderSettings();
	}

	/// <summary>
	/// キャッシュされた設定インスタンスを取得または読み込みする
	/// </summary>
	/// <returns>ビルド設定オブジェクト</returns>
	public static BuilderSettings GetInstance()
	{
		// 意図: 高頻度の呼び出し時に毎回ファイルI/Oが発生しないようキャッシュを保持する
		// 理由: 複数モデル・アセット構築時のファイルアクセスコスト削減のため
		_cachedSettings ??= Load();
		return _cachedSettings;
	}

	/// <summary>
	/// キャッシュを破棄し、次回取得時に再読み込みされるようにする
	/// </summary>
	public static void Reload()
	{
		_cachedSettings = Load();
	}

	#endregion

	#region Private Methods

	/// <summary>
	/// 実行環境（エディタ／配布実行ファイル）に応じた設定ファイルの既定パスを解決する
	/// </summary>
	/// <returns>エディタ実行時は res:// パス、配布実行時は exe と同じディレクトリの settings フォルダのパス</returns>
	private static string ResolveDefaultSettingsPath()
	{
		// 意図: エディタ実行時はプロジェクト内の settings フォルダをそのまま参照する
		// 理由: PCK が存在せずソースツリーがそのまま res:// にマップされるため
		if (OS.HasFeature("editor"))
		{
			return $"res://settings/{SettingsFileName}";
		}

		// 意図: 配布実行時は exe と同じディレクトリの settings フォルダを参照する
		// 制約: export_presets.cfg の exclude_filter="settings/*" により PCK 内に設定は含まれないため、
		//       ビルド後に settings フォルダを exe と同じディレクトリへ手動または配布スクリプトで配置する必要がある
		string executablePath = OS.GetExecutablePath();
		string executableDir = string.IsNullOrWhiteSpace(executablePath)
			? null
			: Path.GetDirectoryName(executablePath);

		if (string.IsNullOrWhiteSpace(executableDir))
		{
			return $"res://settings/{SettingsFileName}";
		}

		return Path.Combine(executableDir, "settings", SettingsFileName);
	}

	#endregion
}
