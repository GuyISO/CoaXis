using Godot;
using System;
using System.IO;
using System.Text.Json;

// TODO: 全然できてないので、後でちゃんと作る








/// <summary>
/// 外部 JSON 設定を読み込んでアプリ全体へ提供する Autoload ノード
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
public partial class SettingService : Node
{

    /// <summary>
    /// 現在有効な設定値。
    /// 起動時に読み込んだ内容を保持し、他サービスはこの値を参照する。
    /// </summary>
    internal ViewerSettings Current { get; private set; } = ViewerSettings.CreateDefault();

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
    /// AutoLoad 初期化時に設定を読み込む。
    /// </summary>
    public override void _Ready()
    {
        Reload();
    }

    /// <summary>
    /// 外部設定を再読込する。
    /// </summary>
    /// <returns>
    /// 外部ファイルから正常に読めた場合は true。
    /// デフォルト値へフォールバックした場合は false。
    /// </returns>
    internal bool Reload()
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
                Application.Log.Info($"Settings: loaded from '{path}'.");
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
            Application.Log.Info($"Settings: default file created at '{writtenPath}'.");
        }
        else
        {
            Application.Log.Warn("Settings: failed to create default settings file. Using in-memory defaults.");
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
            Application.Log.Warn($"Settings: failed to write default file '{path}'. {ex.Message}");
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
    /// カメラ操作に関する設定セクション。
    /// </summary>
    public CameraSettings Camera { get; set; } = new CameraSettings();

    /// <summary>
    /// 入力操作の感度に関する設定セクション。
    /// </summary>
    public InputSettings Input { get; set; } = new InputSettings();

    /// <summary>
    /// UI・描画に関する色設定セクション。
    /// </summary>
    public ColorSettings Color { get; set; } = new ColorSettings();

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
            },
            Camera = new CameraSettings(),
            Input = new InputSettings(),
            Color = new ColorSettings()
        };
    }

    /// <summary>
    /// null や不正値を補正して利用可能な状態にする。
    /// </summary>
    public void Normalize()
    {
        Ipc ??= new IpcSettings();
        Ipc.Normalize();
        Camera ??= new CameraSettings();
        Camera.Normalize();
        Input ??= new InputSettings();
        Input.Normalize();
        Color ??= new ColorSettings();
        Color.Normalize();
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

/// <summary>
/// カメラ操作に適用する設定値。
/// </summary>
public sealed class CameraSettings
{
    /// <summary>
    /// ズーム倍率変更時の底。exponent 1 あたりの拡大倍率。
    /// </summary>
    public float ZoomBase { get; set; } = 1.005f;

    /// <summary>
    /// ズームの最小値。これ以上近づけないようにするための制限値。
    /// </summary>
    public float MinZoomValue { get; set; } = 0.01f;

    /// <summary>
    /// Fit All In 時に対象が画面にぴったり収まるようにするための余白倍率。
    /// </summary>
    public float FitPadding { get; set; } = 1.1f;

    /// <summary>
    /// Tween を使用する場合のアニメーション時間（秒）。
    /// </summary>
    public float TweenDuration { get; set; } = 0.5f;

    /// <summary>
    /// カメラ設定の不正値を補正する。
    /// </summary>
    public void Normalize()
    {
        if (ZoomBase <= 1.0f) ZoomBase = 1.005f;
        if (MinZoomValue <= 0f) MinZoomValue = 0.01f;
        if (FitPadding < 1.0f) FitPadding = 1.1f;
        if (TweenDuration < 0f) TweenDuration = 0.5f;
    }
}

/// <summary>
/// 入力操作の感度に適用する設定値。
/// </summary>
public sealed class InputSettings
{
    /// <summary>
    /// キーボード平行移動速度（m/s）。
    /// </summary>
    public float TranslateSpeed { get; set; } = 8.0f;

    /// <summary>
    /// キーボード回転速度（度/秒）。
    /// </summary>
    public float RotateSpeedDeg { get; set; } = 90.0f;

    /// <summary>
    /// キーボードロール回転速度（度/秒）。
    /// </summary>
    public float RollSpeedDeg { get; set; } = 120.0f;

    /// <summary>
    /// マウスホイールによるズーム倍率係数。
    /// </summary>
    public float ZoomFactor { get; set; } = 1.0f;

    /// <summary>
    /// 画面サイズに対する Orbit/Roll 切り替え用の円領域の半径比率。
    /// </summary>
    public float ArcballRegionRatio { get; set; } = 0.45f;

    /// <summary>
    /// マウス移動の閾値（この値未満の移動は移動なしとみなす）。
    /// </summary>
    public float MoveThreshold { get; set; } = 1.0f;

    /// <summary>
    /// PointerLabel の回転速度（度/秒）。
    /// </summary>
    public float PointerRotationSpeedDeg { get; set; } = 90.0f;

    /// <summary>
    /// 入力感度設定の不正値を補正する。
    /// </summary>
    public void Normalize()
    {
        if (TranslateSpeed <= 0f) TranslateSpeed = 8.0f;
        if (RotateSpeedDeg <= 0f) RotateSpeedDeg = 90.0f;
        if (RollSpeedDeg <= 0f) RollSpeedDeg = 120.0f;
        if (ZoomFactor <= 0f) ZoomFactor = 1.0f;
        if (ArcballRegionRatio <= 0f || ArcballRegionRatio >= 1f) ArcballRegionRatio = 0.45f;
        if (MoveThreshold < 0f) MoveThreshold = 1.0f;
        if (PointerRotationSpeedDeg < 0f) PointerRotationSpeedDeg = 90.0f;
    }
}

/// <summary>
/// UI・描画に適用する色設定値。
/// JSON 上は "#RRGGBBAA" 形式の文字列で管理し、読み込み時に Godot の Color へ変換する。
/// </summary>
public sealed class ColorSettings
{
    /// <summary>
    /// ビューポートオーバーレイ（中心軸・アークボール）の線色。
    /// </summary>
    public string OverlayLineColor { get; set; } = "#E7B1F6FF";

    /// <summary>
    /// 階層ツリーの選択行の背景色。
    /// </summary>
    public string HierarchySelectedColor { get; set; } = "#E7B1F6FF";

    /// <summary>
    /// コマンド履歴の Do（実行済み）行のテキスト色。
    /// </summary>
    public string CommandDoColor { get; set; } = "#FFFFFFFF";

    /// <summary>
    /// コマンド履歴の Undo（取り消し済み）行のテキスト色。
    /// </summary>
    public string CommandUndoColor { get; set; } = "#808080FF";

    /// <summary>
    /// 測定ラインのマテリアル色。
    /// </summary>
    public string MeasurementLineColor { get; set; } = "#FAD11FFF";

    /// <summary>
    /// 色設定の不正値を補正する。
    /// </summary>
    public void Normalize()
    {
        if (string.IsNullOrWhiteSpace(OverlayLineColor)) OverlayLineColor = "#E7B1F6FF";
        if (string.IsNullOrWhiteSpace(HierarchySelectedColor)) HierarchySelectedColor = "#E7B1F6FF";
        if (string.IsNullOrWhiteSpace(CommandDoColor)) CommandDoColor = "#FFFFFFFF";
        if (string.IsNullOrWhiteSpace(CommandUndoColor)) CommandUndoColor = "#808080FF";
        if (string.IsNullOrWhiteSpace(MeasurementLineColor)) MeasurementLineColor = "#FAD11FFF";
    }
}