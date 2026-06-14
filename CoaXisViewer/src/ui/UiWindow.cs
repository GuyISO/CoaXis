using Godot;
using System;

/// <summary>
/// UIを埋め込んで使用するためのウィンドウです。
/// </summary>
public partial class UiWindow : Window
{
	#region Fields

	private Container _rootContainer = null; // コンテンツを配置するためのルートコンテナ

    #endregion

    #region Lifecycle

	public override void _Ready()
	{
		// ウィンドウのクローズがリクエストされたときに呼び出されるイベントハンドラを登録
		CloseRequested += OnCloseRequested;

		// 子ノードが存在しない場合は何もしない
		if (GetChildCount() == 0)
		{
			return;
		}
		// すでにContainerクラスを継承した子ノードが1このみ存在する場合はそれをルートコンテナとして使用する
		else if (GetChildCount() == 1 && GetChild(0) is Container existingContainer)
		{
			SetContainer(existingContainer);
		}
		// それ以外の場合は、エラーを出力する（複数の子ノードが存在する、または子ノードがContainerクラスを継承していない）
		else
		{
			LogHub.I.Error("UiWindow: Invalid child nodes. A UiWindow can only have one child of type Container.");
		}
	}

	public override void _ExitTree()
	{
		// ウィンドウのクローズがリクエストされたときに呼び出されるイベントハンドラを解除
		CloseRequested -= OnCloseRequested;

		ClearContent(); // コンテンツをクリアしてリソースを解放
	}

	#endregion

	#region Events

	/// <summary>
	/// ウィンドウのクローズがリクエストされたときに呼び出されるイベントハンドラです。ウィンドウを閉じるための処理を行います。
	/// </summary>
	private void OnCloseRequested()
	{
		QueueFree();
	}

	/// <summary>
	/// ウィンドウの最小サイズが変更されたときに呼び出されるイベントハンドラです。ウィンドウのサイズを、子コンテナの最小サイズに合わせて調整します。
	/// </summary>
	private void OnMinimumSizeChanged()
	{
        Resize();
	}

	#endregion
	
	#region Public Methods

	/// <summary>
	/// ウィンドウのコンテンツを指定されたコンテナに置き換えます。これにより、ウィンドウ内のUIが新しいコンテナの内容に更新されます。
	/// </summary>
	/// <param name="container">新しいコンテナ</param>
	public void SetContainer(Container container)
	{
		ClearContent();

		// 新しいコンテンツを追加
		if (container != null)
		{
			_rootContainer = container;
			_rootContainer.Owner = this; // シーンツリー上で正しく管理されるようにオーナーを設定
			Resize(); // コンテンツのサイズに合わせてウィンドウのサイズを調整
			// UIの最小サイズが変わったときにウィンドウのサイズも更新するためのイベントハンドラを登録
			_rootContainer.MinimumSizeChanged += OnMinimumSizeChanged;

			// コンテナ名がPanelで始まる場合は除去してタイトルにする（必要に応じて変更可能）
			if (container.Name.ToString().StartsWith("Panel"))
			{
				Title = container.Name.ToString().Substring("Panel".Length);
			}
			else
			{
				Title = container.Name.ToString(); // ウィンドウのタイトルをコンテナの名前に設定（必要に応じて変更可能）
			}

		}
	}

	/// <summary>
	/// ウィンドウのコンテンツをクリアします。これにより、現在のコンテンツが削除され、ウィンドウが空になります。
	/// </summary>
	public void ClearContent()
	{
		if (_rootContainer != null)
		{
			// UIの最小サイズが変わったときにウィンドウのサイズも更新するためのイベントハンドラを解除
			_rootContainer.MinimumSizeChanged -= OnMinimumSizeChanged;
			_rootContainer.QueueFree();
			_rootContainer = null;
		}
	}

	#endregion

	#region Internal Helpers

	/// <summary>
	/// ウィンドウのサイズを、子コンテナの最小サイズに合わせて調整します。これにより、ウィンドウがコンテンツに適切なサイズで表示されるようになります。
	/// </summary>
	private void Resize()
	{
        // 子コンテナの最小サイズを取得
        var min = _rootContainer.GetCombinedMinimumSize();

        // Window のサイズに反映
        Size = new Vector2I((int)min.X, (int)min.Y);
	}

	#endregion
}