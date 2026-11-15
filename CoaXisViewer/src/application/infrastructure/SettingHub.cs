using Godot;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

/// <summary>
/// 外部設定の読み込みとアプリケーションへの提供を管理するハブ。
/// </summary>
/// <remarks>
/// 読み込み優先順位は次の通り。
/// 1) 実行ファイルと同階層の settings/viewer-settings.json
/// 2) 実行ファイルと同階層の viewer-settings.json
/// 3) user://settings/viewer-settings.json（読み書きフォールバック）
///
/// 実行ファイル近傍に設定ファイルを置くことで、ビルド後の配布物でも
/// 再ビルド不要で設定変更できる運用を想定している。
/// </remarks>
public partial class SettingHub : BaseHub
{
    #region Fields

    /// <summary>外部設定ファイル名。配置先ディレクトリは実行環境に応じて決定する。</summary>
    private const string SettingsFileName = "viewer-settings.json";

    /// <summary>現在の設定を書き戻すファイルパス。</summary>
    private string _settingsPath = string.Empty;

    /// <summary>読み込み時に JSON 側の大文字小文字差異を吸収する。</summary>
    private static readonly JsonSerializerOptions ReadOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>既定ファイルを人手編集しやすい形式で出力する。</summary>
    private static readonly JsonSerializerOptions WriteOptions = new JsonSerializerOptions
    {
        WriteIndented = true
    };

    #endregion

    #region Properties

    /// <summary>現在有効な設定値。 起動時に読み込んだ内容を保持し、他サービスはこの値を参照する。</summary>
    public ViewerSettings Current { get; private set; } = ViewerSettings.CreateDefault();

    #endregion

    #region Lifecycle

    /// <summary>
    /// AutoLoad 初期化時に設定を読み込む。
    /// </summary>
    public override void _Ready()
    {
        Reload();
    }

    #endregion

    #region Events

    #endregion

    #region Methods

    /// <summary>
    /// 外部設定を再読込する。
    /// </summary>
    /// <returns>
    /// 外部ファイルから正常に読めた場合は true。
    /// デフォルト値へフォールバックした場合は false。
    /// </returns>
    public bool Reload()
    {
        _settingsPath = string.Empty;
        string[] candidates = BuildSettingsCandidates();

        foreach (string path in candidates)
        {
            if (!File.Exists(path))
            {
                continue;
            }

            if (TryRead(path, out ViewerSettings loaded, out bool migrated))
            {
                Current = loaded;
                _settingsPath = path;
                if (migrated)
                {
                    Save();
                }

                Application.Log.Info($"Settings: loaded from '{path}'.");
                return true;
            }
        }

        // 既存ファイルが読めない場合は安全側で既定値を採用する。
        Current = ViewerSettings.CreateDefault();

        foreach (string path in candidates)
        {
            if (!TryWriteSettings(path, Current))
            {
                continue;
            }

            _settingsPath = path;
            Application.Log.Info($"Settings: default file created at '{path}'.");
            return false;
        }

        Application.Log.Warn("Settings: failed to create default settings file. Using in-memory defaults.");
        return false;
    }

    /// <summary>
    /// 現在の設定を読み込み元のファイルへ保存する。
    /// </summary>
    /// <returns>保存できた場合は true。</returns>
    public bool Save()
    {
        string[] destinations = string.IsNullOrWhiteSpace(_settingsPath)
            ? BuildSettingsCandidates()
            : new[] { _settingsPath };

        foreach (string path in destinations)
        {
            if (!TryWriteSettings(path, Current))
            {
                continue;
            }

            _settingsPath = path;
            Application.Log.Info($"Settings: saved to '{path}'.");
            return true;
        }

        Application.Log.Warn("Settings: failed to save viewer settings.");
        return false;
    }

    /// <summary>
    /// UIテーマの識別子を更新して保存する。
    /// </summary>
    /// <param name="themeId">適用するテーマの識別子。</param>
    /// <returns>設定を保存できた場合は true。</returns>
    public bool SetTheme(string themeId)
    {
        if (string.IsNullOrWhiteSpace(themeId))
        {
            throw new ArgumentException("A theme identifier is required.", nameof(themeId));
        }

        if (string.Equals(Current.Ui.ThemeId, themeId, StringComparison.Ordinal))
        {
            return true;
        }

        Current.Ui.ThemeId = themeId;
        return Save();
    }

    #endregion

    #region Helpers

    /// <summary>
    /// 指定パスの JSON 設定を読み込み、正規化して返す。
    /// </summary>
    /// <param name="path">読み込み対象ファイルパス</param>
    /// <param name="settings">読み込み結果（失敗時は既定値）</param>
    /// <returns>読み込み成功時は true</returns>
    private static bool TryRead(string path, out ViewerSettings settings, out bool migrated)
    {
        settings = ViewerSettings.CreateDefault();
        migrated = false;

        try
        {
            string json = File.ReadAllText(path);
            json = MigrateLegacySettings(json, out migrated);
            ViewerSettings loaded = JsonSerializer.Deserialize<ViewerSettings>(json, ReadOptions);

            if (loaded == null)
            {
                Application.Log.Warn($"Settings: '{path}' is empty. Falling back to default values.");
                return false;
            }

            loaded.Normalize();
            settings = loaded;
            return true;
        }
        catch (Exception ex)
        {
            Application.Log.Warn($"Settings: failed to read '{path}'. {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 旧テーマ設定を識別子へ変換し、テーマへ移した色項目を設定から除去する。
    /// </summary>
    /// <param name="json">読み込んだ設定JSON。</param>
    /// <param name="migrated">旧項目を変換または除去した場合は true。</param>
    /// <returns>変換後のJSON。</returns>
    private static string MigrateLegacySettings(string json, out bool migrated)
    {
        migrated = false;
        JsonNode rootNode = JsonNode.Parse(json);
        if (rootNode is not JsonObject root)
        {
            return json;
        }

        string colorPropertyName = FindPropertyName(root, "Color");
        if (colorPropertyName != null)
        {
            root.Remove(colorPropertyName);
            migrated = true;
        }

        string uiPropertyName = FindPropertyName(root, "Ui");
        if (uiPropertyName != null && root[uiPropertyName] is JsonObject ui)
        {
            string legacyPropertyName = FindPropertyName(ui, "UseLightTheme");
            if (legacyPropertyName != null)
            {
                if (ui[legacyPropertyName] is JsonValue legacyThemeValue
                    && legacyThemeValue.TryGetValue(out bool useLightTheme)
                    && FindPropertyName(ui, nameof(UiSettings.ThemeId)) == null)
                {
                    ui[nameof(UiSettings.ThemeId)] = useLightTheme
                        ? UiSettings.LightThemeId
                        : UiSettings.DefaultThemeId;
                }
                else if (ui[legacyPropertyName] is not JsonValue legacyValue
                    || !legacyValue.TryGetValue(out bool _))
                {
                    Application.Log.Warn("Settings: ignoring invalid legacy 'UseLightTheme' value.");
                }

                ui.Remove(legacyPropertyName);
                migrated = true;
            }
        }

        return migrated ? root.ToJsonString(WriteOptions) : json;
    }

    /// <summary>
    /// 指定したJSONオブジェクトから大文字小文字を無視してプロパティ名を検索する。
    /// </summary>
    /// <param name="jsonObject">検索対象のJSONオブジェクト。</param>
    /// <param name="propertyName">検索するプロパティ名。</param>
    /// <returns>見つかった実際のプロパティ名。存在しない場合は null。</returns>
    private static string FindPropertyName(JsonObject jsonObject, string propertyName)
    {
        foreach (var property in jsonObject)
        {
            if (string.Equals(property.Key, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                return property.Key;
            }
        }

        return null;
    }

    /// <summary>
    /// 設定ファイルを書き込む。
    /// </summary>
    /// <param name="path">出力先ファイルパス</param>
    /// <param name="settings">出力する設定値</param>
    /// <returns>書き込み成功時は true</returns>
    private static bool TryWriteSettings(string path, ViewerSettings settings)
    {
        try
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = JsonSerializer.Serialize(settings, WriteOptions);
            File.WriteAllText(path, json);
            return true;
        }
        catch (Exception ex)
        {
            Application.Log.Warn($"Settings: failed to write '{path}'. {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 設定ファイル探索候補を優先順で返す。
    /// </summary>
    /// <returns>先頭ほど優先度が高い候補パス配列</returns>
    private static string[] BuildSettingsCandidates()
    {
        string baseDir = ResolveBaseDirectory();
        return new[]
        {
            Path.Combine(baseDir, "settings", SettingsFileName),
            Path.Combine(baseDir, SettingsFileName),
            GetUserSettingsPath()
        };
    }

    /// <summary>
    /// user:// 配下の設定パスを返す（最終フォールバック用途）。
    /// </summary>
    private static string GetUserSettingsPath()
    {
        string userDir = ProjectSettings.GlobalizePath("user://settings");
        return Path.Combine(userDir, SettingsFileName);
    }

    /// <summary>
    /// 設定探索の基準ディレクトリを解決する。
    /// </summary>
    /// <returns>
    /// エディタ実行時: プロジェクトルート（res:// の実体パス）
    /// 配布実行時: 実行ファイルディレクトリ
    /// 上記取得不可時: AppContext.BaseDirectory
    /// </returns>
    private static string ResolveBaseDirectory()
    {
        if (OS.HasFeature("editor"))
        {
            // エディタ実行時はプロジェクト内の設定ファイルを直接編集できるようにする。
            return ProjectSettings.GlobalizePath("res://");
        }

        string executablePath = OS.GetExecutablePath();
        if (!string.IsNullOrWhiteSpace(executablePath))
        {
            string executableDir = Path.GetDirectoryName(executablePath);
            if (!string.IsNullOrWhiteSpace(executableDir))
            {
                return executableDir;
            }
        }

        return AppContext.BaseDirectory;
    }

    #endregion
}
