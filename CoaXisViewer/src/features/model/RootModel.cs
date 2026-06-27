using Godot;
using System;

/// <summary>
/// RootModel は、シーンのルートに配置されるモデルであり、モデルロード要求イベントを受け取り、非同期でモデルをロードしてシーンに追加する役割を持ちます。
/// </summary>
public partial class RootModel : AnyModel
{
	#region Lifecycle

	public override void _Ready()
	{
		// イベントハンドラの登録
		ModelEventHub.I.LoadModelRequested += OnLoadModelRequested;
	}

	public override void _ExitTree()
	{
		// イベントハンドラの登録解除
		ModelEventHub.I.LoadModelRequested -= OnLoadModelRequested;
	}

	#endregion

	#region Event

	/// <summary>
	/// モデルロード要求イベントのハンドラです。ModelLoaderを使用して非同期でモデルをロードし、ロード完了後にシーンに追加します。
	/// </summary>
	/// <param name="path">ロードするモデルのパス</param>
	private async void OnLoadModelRequested(string path)
	{
		AnyModel model = new AnyModel();
		model.Name = System.IO.Path.GetFileNameWithoutExtension(path);
		AddChild(model);

		await ModelLoader.LoadModelAsync(model, path);
		
		// モデルの衝突形状を設定するために、1フレーム待つ
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

		ModelLoader.AddCollider(model);

		ModelEventHub.RequestAddModel(model, this);

	}

	#endregion
}
