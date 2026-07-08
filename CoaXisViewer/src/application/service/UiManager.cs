using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// UI管理用 Autoload ノード
/// </summary>
public partial class UiManager : Node
{
    #region Fields

    // TODO: UiWindowのシーンパスをうまく管理する
    private PackedScene _uiWindow = GD.Load<PackedScene>("res://scenes/Ui/Window/UiWindow.tscn"); // UiWindowのシーンパス
    private readonly Dictionary<string, UiWindow> _windowCache = new(); // UIのキャッシュ

    #endregion

    #region Public Methods

    /// <summary>
    /// 指定されたコンテナを表示する
    /// </summary>
    /// <param name="container"></param>
    /// <remarks>
    /// コンテナは Container クラスを継承したUIである必要がある
    /// </remarks>
    internal void Show(Container container)
    {
        if (container == null)
        {
            Application.System.Log.Warn("UiManager: container is null.");
            return;
        }

        if (_uiWindow == null)
        {
            Application.System.Log.Warn("UiManager: _uiWindow is null. Please ensure the UiWindow scene is loaded correctly.");
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

    #region Internal Helpers

    /// <summary>
    /// 指定されたコンテナを表示する
    /// </summary>
    /// <param name="container">表示するコンテナ</param>
    private void ShowWindow(Container container, string cacheKey)
    {
        if (container == null)
        {
            Application.System.Log.Warn("UiManager: container is null.");
            return;
        }

        if (_uiWindow == null)
        {
            Application.System.Log.Warn("UiManager: _uiWindow is null. Please ensure the UiWindow scene is loaded correctly.");
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