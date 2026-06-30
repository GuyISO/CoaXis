using Godot;
using System;
using System.IO;
using System.Text.Json;

// TODO: 全然できてないので、後でちゃんと作る








/// <summary>
/// 外部 JSON 設定を読み込んでアプリ全体へ提供する AutoLoad サービス。
/// </summary>
/// <remarks>
/// 読み込み優先順位は次の通り。
/// 1) 実行ファイルと同階層の settings/viewer-settings.json
/// 2) 実行ファイルと同階層の viewer-settings.json
/// 3) user://settings/viewer-settings.json（書き込みフォールバック）
///
/// 実行ファイル近傍に設定ファイルを置くことで、ビルド後の配布物でも
/// 再ビルド不要で設定変更できる運用を想定している。
/// </summary>
public partial class SettingsService : Node
{
    #region Properties

    public static SettingsService Instance { get; private set; }

    #endregion

    /// <summary>
    /// 現在有効な設定値。
    /// 起動時に読み込んだ内容を保持し、他サービスはこの値を参照する。
    /// </summary>
    public static ViewerSettings Current { get; private set; } = ViewerSettings.CreateDefault();

    /// <summary>
    /// 外部設定ファイル名。配置先ディレクトリは実行環境に応じて決定する。
    /// </summary>
    private const string SettingsFileName = "viewer-settings.json";

    /// <summary>
    /// 読み込み時オプション。JSON 側の大文字小文字差異を吸収する。
    /// </summary>
    private static readonly JsonSerializerOptions ReadOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// 既定ファイル生成時オプション。人手編集しやすい整形出力にする。
    /// </summary>
    private static readonly JsonSerializerOptions WriteOptions = new JsonSerializerOptions
    {
        WriteIndented = true
    };

    /// <summary>
    /// シーンツリー参加時にシングルトン参照を確立する。
    /// </summary>
    public override void _EnterTree()
    {
        Instance = this;
    }

    /// <summary>
    /// AutoLoad 初期化時に設定を読み込む。
    /// </summary>
    public override void _Ready()
    {
        Reload();
    }

    /// <summary>
    /// シーンツリー離脱時にシングルトン参照を解放する。
    /// </summary>
    public override void _ExitTree()
    {
        Instance = null;
    }

    /// <summary>
    /// 外部設定を再読込する。
    /// </summary>
    /// <returns>
    /// 外部ファイルから正常に読めた場合は true。
    /// デフォルト値へフォールバックした場合は false。
    /// </returns>
    public static bool Reload()
    {
        // 候補を優先順位順に列挙する。
        string[] candidates = BuildExternalCandidates();

        foreach (string path in candidates)
        {
            if (!File.Exists(path))
            {
                continue;
            }

            if (TryRead(path, out ViewerSettings loaded))
            {
                Current = loaded;
                LogHub.Info($"Settings: loaded from '{path}'.");
                return true;
            }
        }

        // 既存ファイルが読めない場合は安全側で既定値を採用する。
        Current = ViewerSettings.CreateDefault();

        // 次回以降の編集基点を残すため、書ける場所に既定ファイルを生成する。
        if (TryWriteDefault(candidates[0], Current, out string writtenPath) ||
            TryWriteDefault(candidates[1], Current, out writtenPath) ||
            TryWriteDefault(GetUserSettingsPath(), Current, out writtenPath))
        {
            LogHub.Info($"Settings: default file created at '{writtenPath}'.");
        }
        else
        {
            LogHub.Warn("Settings: failed to create default settings file. Using in-memory defaults.");
        }

        return false;
    }

    /// <summary>
    /// 指定パスの JSON 設定を読み込み、正規化して返す。
    /// </summary>
    /// <param name="path">読み込み対象ファイルパス</param>
    /// <param name="settings">読み込み結果（失敗時は既定値）</param>
    /// <returns>読み込み成功時は true</returns>
    private static bool TryRead(string path, out ViewerSettings settings)
    {
        settings = ViewerSettings.CreateDefault();

        try
        {
            string json = File.ReadAllText(path);
            ViewerSettings loaded = JsonSerializer.Deserialize<ViewerSettings>(json, ReadOptions);

            if (loaded == null)
            {
                LogHub.Warn($"Settings: '{path}' is empty. Falling back to default values.");
                return false;
            }

            loaded.Normalize();
            settings = loaded;
            return true;
        }
        catch (Exception ex)
        {
            LogHub.Warn($"Settings: failed to read '{path}'. {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 既定設定ファイルを書き込む。
    /// </summary>
    /// <param name="path">出力先ファイルパス</param>
    /// <param name="settings">出力する設定値</param>
    /// <param name="writtenPath">書き込み成功時の実パス</param>
    /// <returns>書き込み成功時は true</returns>
    private static bool TryWriteDefault(string path, ViewerSettings settings, out string writtenPath)
    {
        writtenPath = string.Empty;

        try
        {
            // settings ディレクトリが未作成でも起動時に自動生成できるようにする。
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = JsonSerializer.Serialize(settings, WriteOptions);
            File.WriteAllText(path, json);
            writtenPath = path;
            return true;
        }
        catch (Exception ex)
        {
            LogHub.Warn($"Settings: failed to write default file '{path}'. {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 外部設定ファイル探索候補を優先順で返す。
    /// </summary>
    /// <returns>先頭ほど優先度が高い候補パス配列</returns>
    private static string[] BuildExternalCandidates()
    {
        string baseDir = ResolveBaseDirectory();
        return new[]
        {
            Path.Combine(baseDir, "settings", SettingsFileName),
            Path.Combine(baseDir, SettingsFileName)
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
}

/// <summary>
/// ビューア全体の設定ルート。
/// セクション単位でプロパティを増やして拡張する。
/// </summary>
public sealed class ViewerSettings
{
    /// <summary>
    /// IPC 関連の設定セクション。
    /// </summary>
    public IpcSettings Ipc { get; set; } = new IpcSettings();

    /// <summary>
    /// 既定設定を生成する。
    /// </summary>
    /// <returns>安全に起動可能な最小設定</returns>
    public static ViewerSettings CreateDefault()
    {
        return new ViewerSettings
        {
            Ipc = new IpcSettings
            {
                PipeName = "CoaXisViewerPipe",
                StartPipeServerOnReady = true
            }
        };
    }

    /// <summary>
    /// null や不正値を補正して利用可能な状態にする。
    /// </summary>
    public void Normalize()
    {
        Ipc ??= new IpcSettings();
        Ipc.Normalize();
    }
}

/// <summary>
/// IPC サービスに適用する設定値。
/// </summary>
public sealed class IpcSettings
{
    /// <summary>
    /// NamedPipe 名。Editor 側と一致させる必要がある。
    /// </summary>
    public string PipeName { get; set; } = "CoaXisViewerPipe";

    /// <summary>
    /// 起動時に NamedPipe サーバーを立ち上げるかどうか。
    /// </summary>
    public bool StartPipeServerOnReady { get; set; } = true;

    /// <summary>
    /// IPC 設定の不正値を補正する。
    /// </summary>
    public void Normalize()
    {
        if (string.IsNullOrWhiteSpace(PipeName))
        {
            PipeName = "CoaXisViewerPipe";
        }
    }
}