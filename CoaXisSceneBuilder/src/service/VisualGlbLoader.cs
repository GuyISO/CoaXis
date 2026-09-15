using Godot;

/// <summary>
/// 視覚用glbの非同期でない同期ロードを担当するヘルパー
/// </summary>
public static class VisualGlbLoader
{
	#region Public Methods

	/// <summary>
	/// 指定パスのglbを読み込み、指定した親ノード配下へ追加する
	/// </summary>
	/// <param name="meshRoot">読み込んだシーンを追加する親ノード</param>
	/// <param name="path">読み込むglbファイルのパス</param>
	/// <param name="isUnshaded">trueの場合、読み込んだメッシュのマテリアルをUnshadedへ複製・上書きする</param>
	/// <returns>読み込みに成功した場合はtrue、失敗した場合はfalseを返す</returns>
	public static bool Load(Node3D meshRoot, string path, bool isUnshaded)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			GD.PushWarning("VisualGlbLoader: empty visual glb path.");
			return false;
		}

		if (!AssetPathResolver.TryResolveExistingFilePath(path, out string resolvedPath))
		{
			GD.PushWarning($"VisualGlbLoader: file not found. path='{path}'");
			return false;
		}

		var doc = new GltfDocument();
		var state = new GltfState();
		Error error = doc.AppendFromFile(resolvedPath, state);
		if (error != Error.Ok)
		{
			GD.PushError($"VisualGlbLoader: failed to load glb. path='{path}', error={error}");
			return false;
		}

		var visualScene = (Node3D)doc.GenerateScene(state);
		// 意図: 外部設定ファイルからスケール変換および座標補正を取得・適用する
		// 理由: CATIA/GLB出力条件の変更時にコードの再コンパイルを不要にするため
		BuilderSettings builderSettings = BuilderSettingsLoader.GetInstance();
		visualScene.Scale = builderSettings.GlbTransform.GetScaleVector();
		visualScene.RotationDegrees = builderSettings.GlbTransform.GetRotationDegreesVector();

		if (isUnshaded)
		{
			ApplyUnshadedRecursive(visualScene);
		}

		meshRoot.AddChild(visualScene);
		GD.Print($"VisualGlbLoader: loaded '{path}'.");
		return true;
	}

	#endregion

	#region Internal Helpers

	// インポート元のマテリアルを直接書き換えると他のロードと共有した際に影響するため、複製してから変更する
	private static void ApplyUnshadedRecursive(Node node)
	{
		if (node is MeshInstance3D meshInstance && meshInstance.Mesh != null)
		{
			int surfaceCount = meshInstance.Mesh.GetSurfaceCount();
			for (int surfaceIndex = 0; surfaceIndex < surfaceCount; surfaceIndex++)
			{
				Material material = meshInstance.GetSurfaceOverrideMaterial(surfaceIndex)
					?? meshInstance.Mesh.SurfaceGetMaterial(surfaceIndex);

				if (material is BaseMaterial3D baseMaterial)
				{
					var unshadedMaterial = (BaseMaterial3D)baseMaterial.Duplicate();
					unshadedMaterial.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
					meshInstance.SetSurfaceOverrideMaterial(surfaceIndex, unshadedMaterial);
				}
			}
		}

		foreach (Node child in node.GetChildren())
		{
			ApplyUnshadedRecursive(child);
		}
	}

	#endregion
}
