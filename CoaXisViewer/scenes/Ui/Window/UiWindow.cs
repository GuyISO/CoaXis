using Godot;
using System;

/// <summary>
/// UIを埋め込んで使用するためのウィンドウ
/// </summary>
public partial class UiWindow : Window
{
    #region Fields

    private Container _container = null; // コンテンツを配置するためのルートコンテナ

    #endregion

    #region Lifecycle

    public override void _Ready()
    {
        SubscribeUiEvents();
    }

    public override void _ExitTree()
    {
        UnsubscribeUiEvents();

        ClearContent(); // コンテンツをクリアしてリソースを解放

        base._ExitTree();
    }

    #endregion

    #region Events

    /// <summary>
    /// ウィンドウのクローズがリクエストされたときに呼び出されるイベントハンドラ
    /// </summary>
    private void OnCloseRequested()
    {
        QueueFree();
    }

    /// <summary>
    /// ウィンドウの最小サイズが変更されたときに呼び出されるイベントハンドラで、ウィンドウサイズを子コンテナの最小サイズに合わせて調整する
    /// </summary>
    private void OnMinimumSizeChanged()
    {
        Resize();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// ウィンドウのコンテンツを指定されたコンテナに置き換え、ウィンドウ内の UI を新しいコンテナ内容へ更新する
    /// </summary>
    /// <param name="container">新しいコンテナ</param>
    public void SetContainer(Container container)
    {
        ClearContent();

        // 新しいコンテンツを追加
        if (container != null)
        {
            _container = container;
            AddChild(_container);
            // 実行時生成ノードは Owner 設定不要。親子関係のみで管理する。
            _container.MinimumSizeChanged += OnMinimumSizeChanged;
            Resize(); // コンテンツのサイズに合わせてウィンドウのサイズを調整

            Title = container.Name.ToString(); // ウィンドウのタイトルをコンテナの名前に設定（必要に応じて変更可能）
        }
    }

    /// <summary>
    /// ウィンドウのコンテンツをクリアし、現在のコンテンツを削除してウィンドウを空にする
    /// </summary>
    public void ClearContent()
    {
        if (_container != null)
        {
            // UIの最小サイズが変わったときにウィンドウのサイズも更新するためのイベントハンドラを解除
            _container.MinimumSizeChanged -= OnMinimumSizeChanged;

            if (_container.GetParent() != null)
            {
                _container.GetParent().RemoveChild(_container);
            }

            _container.QueueFree();
            _container = null;
        }
    }

    #endregion

    #region Internal Helpers

    /// <summary>
    /// UIイベントの購読を開始する
    /// </summary>
    private void SubscribeUiEvents()
    {
        CloseRequested += OnCloseRequested;
    }

    /// <summary>
    /// UIイベントの購読を解除する
    /// </summary>
    private void UnsubscribeUiEvents()
    {
        CloseRequested -= OnCloseRequested;
    }

    /// <summary>
    /// ウィンドウサイズを子コンテナの最小サイズに合わせて調整し、コンテンツに適したサイズで表示できるようにする
    /// </summary>
    private void Resize()
    {
        // 子コンテナの最小サイズを取得
        var min = _container.GetCombinedMinimumSize();

        // Window のサイズに反映
        Size = new Vector2I((int)min.X, (int)min.Y);
    }

    #endregion
}
