using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

/// <summary>
/// モデルシーンの非同期ロードと描画ノードへの反映を管理する
/// </summary>
public partial class ModelSceneLoader : Node
{
	#region Fields

	private const long SceneLoadFrameBudgetMs = 4;

	private readonly Queue<SceneLoadQueueItem> _sceneLoadQueue = new();
	private readonly object _sceneLoadQueueLock = new();
	private bool _isSceneLoadQueueRunning;
	private long _sceneLoadGeneration;

	#endregion

	#region Public API

	/// <summary>
	/// 新規に生成したモデルをRegistry登録可能な初期状態へ遷移する
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
	/// モデル集合のシーンロードを準備する
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
	/// 準備済みのシーンロードキューを開始する
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
	/// 保留中のシーンロードを無効化し、完了済みロードのキャッシュをクリアする
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

	#region Internal Helpers

	private void QueueSceneLoad(ModelEntity modelEntity)
	{
		if (modelEntity == null || !Application.Model.Registry.IsEntityRegistered(modelEntity.Id))
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

				if (IsActiveEntity(nextItem.ModelEntity) && !TryFinishSceneLoad(nextItem.ModelEntity))
				{
					lock (_sceneLoadQueueLock)
					{
						if (generation == _sceneLoadGeneration)
						{
							_sceneLoadQueue.Enqueue(nextItem);
						}
					}
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
			Application.Log.Error($"ModelSceneLoader: scene load queue processing failed. {exception}");
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
			Application.Log.Error($"ModelSceneLoader: failed to load model assets for entityId='{modelEntity.Id}', scene='{modelEntity.ScenePath}'. {exception}");
			return true;
		}
	}

	private static bool IsActiveEntity(ModelEntity modelEntity)
	{
		return modelEntity != null
			&& modelEntity.Status != ModelStatus.Disposed
			&& Application.Model.Registry.IsEntityRegistered(modelEntity.Id)
			&& modelEntity.Node != null
			&& IsInstanceValid(modelEntity.Node);
	}

	private static void UpdateModelStatus(ModelEntity modelEntity, ModelStatus nextStatus)
	{
		if (modelEntity == null || (modelEntity.Status == ModelStatus.Disposed && nextStatus != ModelStatus.Disposed))
		{
			return;
		}

		modelEntity.Status = nextStatus;
		Application.Model.Event.NotifyStatus(modelEntity.Id, nextStatus);
	}

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