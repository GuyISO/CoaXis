#nullable enable

// TODO: 全然できてないので、後でちゃんと作る

using Godot;
using System;

/// <summary>
/// UI管理用のシングルトンクラス、AutoLoadノードとしてシーンツリーに配置する
/// </summary>
public partial class UiManager : Node
{
    #region Properties

    public static UiManager? Instance { get; private set; }

    #endregion

    #region Lifecycle

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _ExitTree()
    {
        Instance = null;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 指定されたコマンド名に対応する処理を実行する
    /// </summary>
    /// <param name="commandName">実行するコマンドの名前</param>
    /// <param name="source">コマンドを発行したノード</param>
    public void ExecuteCommand(string commandName, Node? source = null)
    {
        _ = source;

        switch (commandName)
        {
            case "ShowViewportInteractionWindow":
                ShowWindow("WindowViewportInteraction");
                break;
            case "ShowMessageWindow":
                ShowWindow("WindowMessage");
                break;
            case "ShowSettingWindow":
                LogHub.Warn("UiManager: settings window is not implemented yet.");
                break;
            case "Load":
                RequestModelLoadFromMainScene();
                break;
            default:
                LogHub.Warn($"UiManager: unknown command '{commandName}'.");
                break;
        }
    }

    #endregion

    #region Internal Helpers

    // Main シーン上の既存 Window ノードを表示し、存在しない scene 参照を排除する
    private void ShowWindow(string windowName)
    {
        Window? window = GetTree().Root.GetNodeOrNull<Window>($"Main/{windowName}");
        if (window == null)
        {
            LogHub.Warn($"UiManager: window '{windowName}' was not found under Main.");
            return;
        }

        window.Show();
        window.GrabFocus();
    }

    // ロード対象パスは Main シーン上の TextEdit から取得し、未設定なら警告して終了する
    private static void RequestModelLoadFromMainScene()
    {
        TextEdit? pathEditor = Engine.GetMainLoop() is SceneTree sceneTree
            ? sceneTree.Root.GetNodeOrNull<TextEdit>("Main/Canvas/VBoxContainer/TopBar/TextGlbPath")
            : null;

        string path = pathEditor?.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(path))
        {
            LogHub.Warn("UiManager: model path is empty.");
            return;
        }

        ModelEventHub.RequestLoadModel(path);
    }

    #endregion

}