// TODO: リファクタリング確認後に削除
using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

/// <summary>
/// モデルシーンの非同期ロードと描画ノードへの反映を管理するハブ。
/// </summary>
public partial class ModelEntitySceneHub : BaseHub
{
	#region Fields

	/// <summary>1フレームあたりのSceneロードに使える時間（ミリ秒）。</summary>
	private const long SceneLoadFrameBudgetMs = 400;

	/// <summary>Sceneロード待ちのキュー。</summary>
	private readonly Queue<SceneLoadQueueItem> _sceneLoadQueue = new();
	/// <summary>Sceneロードキューの排他制御用ロック。</summary>
	private readonly object _sceneLoadQueueLock = new();
	/// <summary>Sceneロードキューの処理中フラグ。</summary>
	private bool _isSceneLoadQueueRunning;
	/// <summary>Sceneロードの世代番号。取消し判定に使う。</summary>
	private long _sceneLoadGeneration;

	#endregion

	#region Properties

	#endregion

	#region Signals

	#endregion

	#region Lifecycle

	#endregion

	#region Events

	#endregion

	#region Methods

	/// <summary>
	/// 新規に生成したモデルをRegistry登録可能な初期状態へ遷移する。
	/// </summary>
	/// <param name="modelEntity">初期化するモデル実体</param>
	/// <exception cref="ArgumentNullException">modelEntityがnullの場合</exception>
	public void MarkInitialized(ModelEntity modelEntity)
	{
		if (modelEntity == null)
		{
			throw new ArgumentNullException(nameof(modelEntity));
		}

		UpdateModelStatus(modelEntity, ModelStatus.Initialized);
	}

	/// <summary>
	/// モデル集合のシーンロードを準備する。
	/// </summary>
	/// <param name="modelEntities">シーンロード対象のモデル集合</param>
	/// <exception cref="ArgumentNullException">modelEntitiesがnullの場合</exception>
	public void PrepareLoads(IReadOnlyList<ModelEntity> modelEntities)
	{
		if (modelEntities == null)
		{
			throw new ArgumentNullException(nameof(modelEntities));
		}

		foreach (ModelEntity modelEntity in modelEntities)
		{
			if (string.IsNullOrWhiteSpace(modelEntity.ScenePath))
			{
				UpdateModelStatus(modelEntity, ModelStatus.Loaded);
				continue;
			}

			QueueSceneLoad(modelEntity);
		}
	}

	/// <summary>
	/// 準備済みのシーンロードキューを開始する。
	/// </summary>
	public void StartPendingLoads()
	{
		lock (_sceneLoadQueueLock)
		{
			if (_isSceneLoadQueueRunning || _sceneLoadQueue.Count == 0)
			{
				return;
			}

			_isSceneLoadQueueRunning = true;
			_ = ProcessSceneLoadQueueAsync(_sceneLoadGeneration);
		}
	}

	/// <summary>
	/// 保留中のシーンロードを無効化し、完了済みロードのキャッシュをクリアする。
	/// </summary>
	public void CancelPendingLoads()
	{
		lock (_sceneLoadQueueLock)
		{
			_sceneLoadQueue.Clear();
			_isSceneLoadQueueRunning = false;
			_sceneLoadGeneration++;
		}

		SceneAssetLoader.ClearCompletedCache();
	}

	#endregion

	#region Helpers

	/// <summary>
	/// Sceneロードをキューへ追加する。
	/// </summary>
	private void QueueSceneLoad(ModelEntity modelEntity)
	{
		if (modelEntity == null || !Application.Model.Entity.Registry.IsEntityRegistered(modelEntity.Id))
		{
			return;
		}

		// 実ファイルI/Oは直ちに要求し、完了確認だけをフレーム予算内で処理する。
		SceneAssetLoader.RequestLoad(modelEntity.ScenePath);

		lock (_sceneLoadQueueLock)
		{
			_sceneLoadQueue.Enqueue(new SceneLoadQueueItem(modelEntity, _sceneLoadGeneration));
		}
	}

	/// <summary>
	/// Sceneロードキューを順次処理する。
	/// </summary>
	private async Task ProcessSceneLoadQueueAsync(long generation)
	{
		try
		{
			var stopwatch = Stopwatch.StartNew();
			while (true)
			{
				SceneLoadQueueItem nextItem;
				lock (_sceneLoadQueueLock)
				{
					if (generation != _sceneLoadGeneration)
					{
						return;
					}

					if (_sceneLoadQueue.Count == 0)
					{
						_isSceneLoadQueueRunning = false;
						return;
					}

					nextItem = _sceneLoadQueue.Dequeue();
				}

				if (nextItem.Generation != generation)
				{
					continue;
				}

				if (IsActiveEntity(nextItem.ModelEntity))
				{
					if (!TryFinishSceneLoad(nextItem.ModelEntity))
					{
						lock (_sceneLoadQueueLock)
						{
							if (generation == _sceneLoadGeneration)
							{
								_sceneLoadQueue.Enqueue(nextItem);
							}
						}
					}
				}
				else
				{
					SceneAssetLoader.ReleaseLoad(nextItem.ModelEntity.ScenePath);
				}

				if (stopwatch.ElapsedMilliseconds >= SceneLoadFrameBudgetMs)
				{
					await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
					stopwatch.Restart();
				}
			}
		}
		catch (Exception exception)
		{
			Application.Log.Error($"ModelEntitySceneHub: scene load queue processing failed. {exception}");
		}
		finally
		{
			lock (_sceneLoadQueueLock)
			{
				if (generation == _sceneLoadGeneration)
				{
					_isSceneLoadQueueRunning = false;
				}
			}
		}
	}

	/// <summary>
	/// Sceneロード完了を判定し、完了していれば状態を更新する。
	/// </summary>
	private bool TryFinishSceneLoad(ModelEntity modelEntity)
	{
		try
		{
			if (!IsActiveEntity(modelEntity))
			{
				return true;
			}

			ModelNode modelNode = modelEntity.Node;
			if (modelNode == null || !IsInstanceValid(modelNode))
			{
				return true;
			}

			if (modelEntity.Status != ModelStatus.Loading)
			{
				UpdateModelStatus(modelEntity, ModelStatus.Loading);
			}

			SceneLoadResult result = SceneAssetLoader.TryFinishLoad(modelNode, modelEntity.ScenePath);
			if (result == SceneLoadResult.InProgress)
			{
				return false;
			}

			if (!IsActiveEntity(modelEntity))
			{
				return true;
			}

			bool sceneLoaded = result == SceneLoadResult.Loaded;
			if (sceneLoaded && modelEntity.AlignToAabbCenter)
			{
				ModelMeshCenterAligner.AlignPivotToMeshCenter(modelNode);
			}

			modelNode.ApplyVisibilityLayer(ModelVisibilityResolver.IsVisible(modelEntity));
			UpdateModelStatus(modelEntity, sceneLoaded ? ModelStatus.Loaded : ModelStatus.LoadFailed);
			return true;
		}
		catch (Exception exception)
		{
			UpdateModelStatus(modelEntity, ModelStatus.LoadFailed);
			Application.Log.Error($"ModelEntitySceneHub: failed to load model assets for entityId='{modelEntity.Id}', scene='{modelEntity.ScenePath}'. {exception}");
			return true;
		}
	}

	/// <summary>
	/// Entityが現在も有効（登録済み）か判定する。
	/// </summary>
	private static bool IsActiveEntity(ModelEntity modelEntity)
	{
		return modelEntity != null
			&& modelEntity.Status != ModelStatus.Disposed
			&& Application.Model.Entity.Registry.IsEntityRegistered(modelEntity.Id)
			&& modelEntity.Node != null
			&& IsInstanceValid(modelEntity.Node);
	}

	/// <summary>
	/// Entityのロード状態を更新して通知する。
	/// </summary>
	private static void UpdateModelStatus(ModelEntity modelEntity, ModelStatus nextStatus)
	{
		if (modelEntity == null || (modelEntity.Status == ModelStatus.Disposed && nextStatus != ModelStatus.Disposed))
		{
			return;
		}

		modelEntity.Status = nextStatus;
		Application.Model.Entity.State.NotifyStatus(modelEntity.Id);
	}

	/// <summary>
	/// Sceneロードキューの要素。
	/// </summary>
	private sealed class SceneLoadQueueItem
	{
		public SceneLoadQueueItem(ModelEntity modelEntity, long generation)
		{
			ModelEntity = modelEntity;
			Generation = generation;
		}

		public ModelEntity ModelEntity { get; }

		public long Generation { get; }
	}

	#endregion
}