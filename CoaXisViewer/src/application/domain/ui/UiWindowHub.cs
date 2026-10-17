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

        string cacheKey = GetContainerCacheKey(container);
        if (_windowCache.TryGetValue(cacheKey, out UiWindow cachedWindow))
        {
            if (GodotObject.IsInstanceValid(cachedWindow))
            {
                cachedWindow.Show();
                cachedWindow.GrabFocus();
                container.QueueFree();
                return;
            }

            _windowCache.Remove(cacheKey);
        }

        ShowWindow(container, cacheKey);
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
        _windowCache[cacheKey] = window;
        window.TreeExited += () => OnWindowTreeExited(cacheKey, window);

        AddChild(window);
        window.SetContainer(container);
        window.Show();
        window.GrabFocus();

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