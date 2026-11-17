using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// UI ウィンドウの生成と表示状態を管理するハブ。
/// </summary>
public partial class UiWindowHub : BaseHub
{
    #region Fields

    /// <summary>生成済みUiWindowのキャッシュ。</summary>
    private readonly Dictionary<string, UiWindow> _windowCache = new();

    #endregion

    #region Properties

    #endregion

    #region Signals

    /// <summary>表示ウィンドウの状態が変化したことを通知する。</summary>
    [Signal] public delegate void WindowVisibilityNotifiedEventHandler();

    /// <summary>
    /// 表示ウィンドウの状態変更を通知する。
    /// </summary>
    private void NotifyWindowVisibility()
    {
        EmitSignal(SignalName.WindowVisibilityNotified);
    }

    #endregion

    #region Lifecycle

    #endregion

    #region Events

    /// <summary>
    /// ウィンドウがツリーから退出したときにキャッシュから削除するためのイベントハンドラ。
    /// </summary>
    private void OnWindowTreeExited(string cacheKey, UiWindow window)
    {
        if (_windowCache.TryGetValue(cacheKey, out UiWindow cachedWindow) && cachedWindow == window)
        {
            _windowCache.Remove(cacheKey);
            NotifyWindowVisibility();
        }
    }

    #endregion

    #region Methods

    /// <summary>
    /// 指定されたコンテナを表示する。
    /// </summary>
    /// <param name="container">表示するコンテナ。</param>
    /// <remarks>
    /// コンテナは Container クラスを継承したUIである必要がある
    /// </remarks>
    public void Show(Container container)
    {
        if (container == null)
        {
            Application.Log.Warn("UiWindowHub: container is null.");
            return;
        }

        Show(container, GetContainerCacheKey(container));
    }

    /// <summary>
    /// 指定したキャッシュキーでコンテナを表示する。
    /// </summary>
    /// <param name="container">表示するコンテナ</param>
    /// <param name="cacheKey">表示ウィンドウのキャッシュキー</param>
    public void Show(Container container, string cacheKey)
    {
        if (container == null)
        {
            Application.Log.Warn("UiWindowHub: container is null.");
            return;
        }

        if (string.IsNullOrWhiteSpace(cacheKey))
        {
            Application.Log.Warn("UiWindowHub: cache key is empty.");
            return;
        }

        if (_windowCache.TryGetValue(cacheKey, out UiWindow cachedWindow))
        {
            if (GodotObject.IsInstanceValid(cachedWindow))
            {
                cachedWindow.Show();
                cachedWindow.GrabFocus();
                container.QueueFree();
                NotifyWindowVisibility();
                return;
            }

            _windowCache.Remove(cacheKey);
        }

        ShowWindow(container, cacheKey);
    }

    /// <summary>
    /// 指定した ModelEntity を起点とするツリーを独立したウィンドウに表示する。
    /// </summary>
    /// <param name="entityId">ツリーの起点とする ModelEntity の識別子</param>
    public void ShowModelEntityTree(Guid entityId)
    {
        if (entityId == Guid.Empty)
        {
            Application.Log.Warn("UiWindowHub: model entity tree root ID is empty.");
            return;
        }

        ModelEntity modelEntity = Application.Model.Entity.Registry.Get(entityId);
        if (modelEntity == null)
        {
            Application.Log.Warn($"UiWindowHub: model entity tree root not found. entityId='{entityId}'");
            return;
        }

        PackedScene scene = ResourceLoader.Load<PackedScene>("res://scenes/ui/model_entity_tree_container.tscn");
        if (scene == null)
        {
            Application.Log.Error("UiWindowHub: ModelEntityTreeContainer scene could not be loaded.");
            return;
        }

        Container container = scene.Instantiate<Container>();
        ModelEntityTree tree = container.GetNode<ModelEntityTree>("ModelEntityTree");
        tree.SetRoot(modelEntity);
        Show(container, $"ModelEntityTreeContainer:{entityId:N}");
    }

    /// <summary>
    /// 指定されたUIシーンのウィンドウが表示中かどうかを返す。
    /// </summary>
    /// <param name="scene">表示状態を確認するUIシーン。</param>
    /// <returns>ウィンドウが表示中の場合は true。</returns>
    public bool IsContainerVisible(PackedScene scene)
    {
        if (scene == null)
        {
            Application.Log.Warn("UiWindowHub: scene is null.");
            return false;
        }

        string cacheKey = scene.ResourcePath;
        if (string.IsNullOrWhiteSpace(cacheKey))
        {
            Application.Log.Warn("UiWindowHub: scene resource path is empty.");
            return false;
        }

        return _windowCache.TryGetValue(cacheKey, out UiWindow window)
            && GodotObject.IsInstanceValid(window)
            && window.Visible;
    }

    /// <summary>
    /// 指定されたUIシーンのウィンドウを閉じる。
    /// </summary>
    /// <param name="scene">閉じるUIシーン。</param>
    public void Close(PackedScene scene)
    {
        if (scene == null || string.IsNullOrWhiteSpace(scene.ResourcePath))
        {
            Application.Log.Warn("UiWindowHub: scene or its resource path is invalid.");
            return;
        }

        string cacheKey = scene.ResourcePath;
        if (_windowCache.TryGetValue(cacheKey, out UiWindow window))
        {
            _windowCache.Remove(cacheKey);
            if (GodotObject.IsInstanceValid(window))
            {
                window.Hide();
                window.QueueFree();
            }

            NotifyWindowVisibility();
        }
    }

    #endregion

    #region Helpers

    /// <summary>
    /// キャッシュキーを指定してコンテナをウィンドウとして表示する。
    /// </summary>
    /// <param name="container">表示するコンテナ</param>
    private void ShowWindow(Container container, string cacheKey)
    {
        UiWindow window = new()
        {
            OversamplingOverride = 1.0f,
            InitialPosition = Window.WindowInitialPosition.CenterMainWindowScreen,
            AlwaysOnTop = true,
            Unfocusable = true,
            MinimizeDisabled = true,
            MaximizeDisabled = true
        };
        Application.Ui.Theme.ApplyTo(window);
        _windowCache[cacheKey] = window;
        window.TreeExited += () => OnWindowTreeExited(cacheKey, window);

        AddChild(window);
        window.SetContainer(container);
        window.Show();
        window.GrabFocus();
        NotifyWindowVisibility();
    }

    /// <summary>
    /// コンテナのキャッシュキーを取得する。
    /// </summary>
    /// <param name="container">キャッシュキーを取得するコンテナ</param>
    private string GetContainerCacheKey(Container container)
    {
        if (!string.IsNullOrWhiteSpace(container.SceneFilePath))
        {
            return container.SceneFilePath;
        }

        Type type = container.GetType();
        if (type != null && !string.IsNullOrWhiteSpace(type.FullName))
        {
            return type.FullName;
        }

        return container.Name.ToString();
    }

    #endregion
}