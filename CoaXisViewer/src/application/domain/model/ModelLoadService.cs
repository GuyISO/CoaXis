using CoaXis.Protocol.Viewer;
using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// モデルDTOの置換ロードを統括するサービス
/// </summary>
public partial class ModelLoadService : Node
{
	#region Public API

	/// <summary>
	/// 現在のモデル集合をクリアし、指定したDTOからモデル集合を再構築する
	/// </summary>
	/// <param name="entityDtos">読み込むモデル実体DTOの集合</param>
	/// <returns>登録およびシーン反映を開始したModelEntityの一覧</returns>
	/// <exception cref="ArgumentNullException">entityDtosがnullの場合</exception>
	public IReadOnlyList<ModelEntity> ReplaceEntities(IReadOnlyList<ModelEntityDto> entityDtos)
	{
		if (entityDtos == null)
		{
			throw new ArgumentNullException(nameof(entityDtos));
		}

		ClearModels();
		return Application.Model.EntityFactory.CreateEntities(entityDtos);
	}

	/// <summary>
	/// 現在のモデル集合と保留中のシーンロードをクリアする
	/// </summary>
	public void ClearModels()
	{
		// 旧世代を先に無効化してからRegistryをクリアし、遅延完了したロードが古いノードを更新しないようにする。
		Application.Model.SceneLoader.CancelPendingLoads();
		Application.Model.Registry.Clear();
	}

	#endregion
}