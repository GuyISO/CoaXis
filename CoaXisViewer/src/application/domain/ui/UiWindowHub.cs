// TODO: リファクタリング確認後に削除
using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// UI ウィンドウの生成と表示状態を管理するハブ。
/// </summary>
public partial class UiWindowHub : BaseHub
{
    #region Fields

    // TODO: UiWindowのシーンパスをうまく管理する？むしろWindowを継承したUiWindowを動的に生成する
    private PackedScene _uiWindow = GD.Load<PackedScene>("res://scenes/ui/ui_window.tscn"); // UiWindowのシーンパス
    private readonly Dictionary<string, UiWindow> _windowCache = new(); // UIのキャッシュ

    #endregion

    #region Properties

    #endregion

    #region Lifecycle

    #endregion

    #region Actions

    #endregion

    #region Notifications

    #endregion

    #region Events

    #endregion

    #region Methods

    /// <summary>
    /// 指定されたコンテナを表示する
    /// </summary>
    /// <param name="container">表示するコンテナ。</param>
    /// <remarks>
    /// コンテナは Container クラスを継承したUIである必要がある
    /// </remarks>
    internal void Show(Container container)
    {
        if (container == null)
        {
            Application.Log.Warn("UiWindowHub: container is null.");
            return;
        }

        if (_uiWindow == null)
        {
            Application.Log.Warn("UiWindowHub: _uiWindow is null. Please ensure the UiWindow scene is loaded correctly.");
            container.QueueFree();
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
    /// 指定されたコンテナを表示する
    /// </summary>
    /// <param name="container">表示するコンテナ</param>
    private void ShowWindow(Container container, string cacheKey)
    {
        if (container == null)
        {
            Application.Log.Warn("UiWindowHub: container is null.");
            return;
        }

        if (_uiWindow == null)
        {
            Application.Log.Warn("UiWindowHub: _uiWindow is null. Please ensure the UiWindow scene is loaded correctly.");
            container.QueueFree();
            return;
        }

        UiWindow window = _uiWindow.Instantiate<UiWindow>();
        _windowCache[cacheKey] = window;
        window.TreeExited += () => OnWindowTreeExited(cacheKey, window);

        AddChild(window);
        window.SetContainer(container);
        window.Show();
        window.GrabFocus();

    }

    /// <summary>
    /// ウィンドウがツリーから退出したときにキャッシュから削除するためのイベントハンドラ
    /// </summary>
    private void OnWindowTreeExited(string cacheKey, UiWindow window)
    {
        if (_windowCache.TryGetValue(cacheKey, out UiWindow cachedWindow) && cachedWindow == window)
        {
            _windowCache.Remove(cacheKey);
        }
    }

    /// <summary>
    /// コンテナのキャッシュキーを取得する
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