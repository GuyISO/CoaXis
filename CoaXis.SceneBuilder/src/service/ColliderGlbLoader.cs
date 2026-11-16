using Godot;
using System.Collections.Generic;

/// <summary>
/// コライダー用glbからメッシュ面を抽出するヘルパー
/// </summary>
public static class ColliderGlbLoader
{
	#region Public Methods

	/// <summary>
	/// 指定パスのglbを読み込み、メッシュ面を指定リストへ積み上げる
	/// </summary>
	/// <remarks>
	/// 当たり判定専用glbを想定しており、読み込んだノード自体は表示に使わないため、
	/// 面情報を抽出した後にシーンツリー未追加のまま破棄する。
	/// </remarks>
	/// <param name="path">読み込むコライダー用glbファイルのパス</param>
	/// <param name="faces">抽出した面座標(ワールド座標系相当)を積み上げる先のリスト</param>
	/// <returns>1面以上抽出できた場合はtrue、失敗した場合はfalseを返す</returns>
	public static bool CollectFaces(string path, List<Vector3> faces)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			GD.PushWarning("ColliderGlbLoader: empty collider glb path.");
			return false;
		}

		if (!AssetPathResolver.TryResolveExistingFilePath(path, out string resolvedPath))
		{
			GD.PushWarning($"ColliderGlbLoader: file not found. path='{path}'");
			return false;
		}

		var doc = new GltfDocument();
		var state = new GltfState();
		Error error = doc.AppendFromFile(resolvedPath, state);
		if (error != Error.Ok)
		{
			GD.PushError($"ColliderGlbLoader: failed to load glb. path='{path}', error={error}");
			return false;
		}

		var colliderScene = (Node3D)doc.GenerateScene(state);
		// 意図: 視覚メッシュと同じ外部設定ファイルのスケール変換・座標補正を適用し、当たり判定を見た目の座標と一致させる
		// 理由: 外部設定でスケーリングが変更された場合でもコライダー座標とのズレを発生させないため
		BuilderSettings builderSettings = BuilderSettingsLoader.GetInstance();
		colliderScene.Scale = builderSettings.GlbTransform.GetScaleVector();
		colliderScene.RotationDegrees = builderSettings.GlbTransform.GetRotationDegreesVector();

		int beforeCount = faces.Count;
		CollectMeshFaces(colliderScene, Transform3D.Identity, faces);
		// シーンツリーに追加していない一時ノードのため、面抽出後は即時破棄してよい
		colliderScene.Free();

		if (faces.Count == beforeCount)
		{
			// 意図: メッシュを一切保持しないglbも入力データとして正常なケースのため、警告のみで正常終了とする
			GD.Print($"ColliderGlbLoader: no mesh faces found (empty glb is valid). path='{path}'.");
			return true;
		}

		GD.Print($"ColliderGlbLoader: collected faces from '{path}'.");
		return true;
	}

	#endregion

	#region Internal Helpers

	// ノードツリーに追加していない一時ノードでもGlobalTransformに頼らず面座標を求められるよう、
	// 親からの変換を手動で積み上げながら再帰的にMeshInstance3Dの面を収集する
	private static void CollectMeshFaces(Node node, Transform3D parentTransform, List<Vector3> faces)
	{
		// 意図: 生成した一時シーンに対して、親ノードの変換を自前で積み重ねることで GlobalTransform を使わずに面座標を精確に求める。
		// 理由: 変換前の MeshInstance3D の local face を親の座標変換と組み合わせて最終的なワールド面を決定するため。
		if (node is not Node3D node3D)
		{
			foreach (Node child in node.GetChildren())
			{
				CollectMeshFaces(child, parentTransform, faces);
			}
			return;
		}

		Transform3D worldTransform = parentTransform * node3D.Transform;

		if (node3D is MeshInstance3D meshInstance && meshInstance.Mesh != null)
		{
			Vector3[] localFaces = meshInstance.Mesh.GetFaces();
			foreach (Vector3 localFace in localFaces)
			{
				faces.Add(worldTransform * localFace);
			}
		}

		foreach (Node child in node3D.GetChildren())
		{
			CollectMeshFaces(child, worldTransform, faces);
		}
	}

	#endregion
}
