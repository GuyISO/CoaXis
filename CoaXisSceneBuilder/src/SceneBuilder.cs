using Godot;
using System;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// glb(視覚/コライダー)とラインセットJSONを事前ロードし、単一のPackedSceneへ焼き込むビルダー
/// </summary>
/// <remarks>
/// 実行時のモデル読み込み・動的生成負荷を完全に排除し、事前に最適化された単一の PackedScene (.scn) を構築するためのビルダー。
/// このクラスはビルド対象のノード構成とライフサイクルの管理に専念する。
/// </remarks>
public partial class SceneBuilder : Node
{
	#region Fields

	private Node3D _scene = null;
	private Node3D _meshRoot = null;
	private StaticBody3D _collider = null;
	private readonly List<Vector3> _colliderFaces = new();

	#endregion

	#region Public Methods

	/// <summary>
	/// 入力 DTO に指定されたアセットを統合し、PackedScene を .scn ファイルへ保存する
	/// </summary>
	/// <param name="request">視覚・コライダー・ラインセットおよび出力先を示すビルド要求</param>
	/// <returns>すべての指定アセットのロードとシーン保存に成功した場合はtrue、失敗した場合はfalse</returns>
	public bool TryBuild(SceneBuildRequestDto request)
	{
		// ビルダーを再利用しても前回のノードやコライダー面が混入しないよう、要求ごとに状態を初期化する
		ResetBuildState();

		try
		{
			if (!TryResolveOutputPath(request, out string outputPath)
				|| !TryPopulateSceneFromRequest(request))
			{
				return false;
			}

			// 意図: 複数の入力から面を集め終えてから、コライダー形状を一度だけ生成する。
			// 理由: Collider GLB と LineSet の追加ごとに再構築すると、同じ面配列を複数回 Godot の形状へ変換するため。
			if (_colliderFaces.Count > 0)
			{
				RebuildColliderShape();
			}

			return TryPackCurrentScene(out PackedScene packedScene)
				&& TrySaveScene(packedScene, outputPath);
		}
		finally
		{
			// 失敗経路を含めて要求ごとの一時ノードを残さず、同じインスタンスを安全に再利用できるようにする
			ResetBuildState();
		}
	}

	#endregion

	#region Internal Helpers

	/// <summary>
	/// DTO に指定された任意アセットを読み込み、ビルド中のシーンへ追加する
	/// </summary>
	/// <param name="request">追加する視覚・衝突・ラインセットアセットを示す要求</param>
	/// <returns>指定済みのすべてのアセットを追加できた場合はtrue、それ以外はfalse</returns>
	private bool TryPopulateSceneFromRequest(SceneBuildRequestDto request)
	{
		// 各アセットは任意だが、指定されたアセットの失敗を無視すると不完全な .scn が生成されるため中断する
		if (!string.IsNullOrWhiteSpace(request.VisualGlb)
			&& !VisualGlbLoader.Load(EnsureMesh(), request.VisualGlb, request.VisualUnshaded))
		{
			return false;
		}

		if (!string.IsNullOrWhiteSpace(request.ColliderGlb)
			&& !TryAppendColliderFromGlb(request.ColliderGlb))
		{
			return false;
		}

		return string.IsNullOrWhiteSpace(request.LineSetJson)
			|| TryAppendLineSet(request.LineSetJson);
	}

	/// <summary>
	/// ビルド対象のシーンルートを取得する。未生成の場合はここで生成する
	/// </summary>
	/// <returns>ビルド対象のシーンルート</returns>
	private Node3D EnsureScene()
	{
		_scene = _scene ?? new Node3D { Name = "Model" };
		return _scene;
	}

	/// <summary>
 	/// コライダー用 GLB の面を収集し、ビルド中のコライダー面へ追加する
	/// </summary>
	/// <remarks>
	/// 複数回呼び出した場合は面を積み上げる。衝突形状の生成はビルド完了直前に行う。
	/// </remarks>
	/// <param name="path">読み込むコライダー用glbファイルのパス</param>
	/// <returns>読み込みに成功した場合はtrue、失敗した場合はfalseを返す</returns>
	private bool TryAppendColliderFromGlb(string path)
	{
		if (!ColliderGlbLoader.CollectFaces(path, _colliderFaces))
		{
			return false;
		}

		return true;
	}

	/// <summary>
 	/// ラインセットをチューブメッシュとして追加し、そのチューブ面をビルド中のコライダー面へ追加する
	/// </summary>
	/// <param name="path">読み込むラインセットJSONファイルのパス</param>
	/// <returns>ラインセットの読み込みとコライダーの追加に成功した場合はtrue、失敗した場合はfalse</returns>
	private bool TryAppendLineSet(string path)
	{
		if (!LineSetJsonParser.LoadLines(EnsureMesh(), path, _colliderFaces))
		{
			return false;
		}

		return true;
	}

	/// <summary>
	/// ビルド中のシーンを PackedScene にパックする
	/// </summary>
	/// <param name="packedScene">パックに成功したシーン。失敗時はnull</param>
	/// <returns>シーンが存在し、パックに成功した場合はtrue、それ以外はfalse</returns>
	private bool TryPackCurrentScene(out PackedScene packedScene)
	{
		packedScene = null;
		if (_scene == null)
		{
			GD.PushWarning("SceneBuilder: no scene to build.");
			return false;
		}

		// PackedScene.Pack()はOwnerが設定されていない子孫ノードを保存対象から除外するため、
		// ここでシーンルートを全子孫のOwnerとして明示する
		SetOwnerRecursive(_scene, _scene);

		var packedSceneToSave = new PackedScene();
		Error error = packedSceneToSave.Pack(_scene);
		if (error != Error.Ok)
		{
			GD.PushError($"SceneBuilder: failed to pack scene. error={error}");
			return false;
		}

		packedScene = packedSceneToSave;
		return true;
	}

	/// <summary>
	/// ビルド中の一時ノードと要求に紐づく内部状態を初期化する
	/// </summary>
	private void ResetBuildState()
	{
		_scene?.QueueFree();
		_scene = null;
		_meshRoot = null;
		_collider = null;
		_colliderFaces.Clear();
	}

	/// <summary>
	/// パック済みシーンを指定パスへ保存する
	/// </summary>
	/// <param name="packedScene">保存するパック済みシーン</param>
	/// <param name="outputPath">拡張子 .scn を持つ res://・user://・プロジェクト相対・絶対の出力パス</param>
	/// <returns>保存に成功した場合はtrue、それ以外はfalse</returns>
	private static bool TrySaveScene(PackedScene packedScene, string outputPath)
	{
		// 保存先ディレクトリは入力 JSON 側で事前作成を強制せず、ビルダーが出力まで完結させる
		Directory.CreateDirectory(Path.GetDirectoryName(ProjectSettings.GlobalizePath(outputPath))!);
		Error error = ResourceSaver.Save(packedScene, outputPath);
		if (error != Error.Ok)
		{
			GD.PushError($"SceneBuilder: failed to save scene. path='{outputPath}', error={error}");
			return false;
		}

		GD.Print($"SceneBuilder: saved '{outputPath}'.");
		return true;
	}

	/// <summary>
	/// DTO の出力先を検証し、ResourceSaver が受け取れるパスへ正規化する
	/// </summary>
	/// <param name="request">出力先を含むビルド要求</param>
	/// <param name="outputPath">検証済みの出力パス。失敗時は空文字列</param>
	/// <returns>出力先が .scn ファイルとして有効な場合はtrue、それ以外はfalse</returns>
	private static bool TryResolveOutputPath(SceneBuildRequestDto request, out string outputPath)
	{
		outputPath = string.Empty;
		if (request == null || string.IsNullOrWhiteSpace(request.OutputScn))
		{
			GD.PushWarning("SceneBuilder: output scn path is required.");
			return false;
		}

		// 意図: ResourceSaver には res:// や user:// と絶対パスをそのまま渡せる。
		// しかし相対パスをそのまま保存するとプロジェクトルートが曖昧になるため、ビルド側で res:// に寄せて扱う。

		if (!string.Equals(Path.GetExtension(request.OutputScn), ".scn", StringComparison.OrdinalIgnoreCase))
		{
			GD.PushWarning($"SceneBuilder: output path must use the .scn extension. path='{request.OutputScn}'");
			return false;
		}

		// res://・user:// と絶対パスはそのまま ResourceSaver へ渡し、相対パスだけを従来どおりプロジェクト基準にする。
		outputPath = request.OutputScn.StartsWith("res://", StringComparison.OrdinalIgnoreCase)
			|| request.OutputScn.StartsWith("user://", StringComparison.OrdinalIgnoreCase)
			|| Path.IsPathRooted(request.OutputScn)
			? request.OutputScn
			: $"res://{request.OutputScn.TrimStart('/', '\\')}";
		return true;
	}

	/// <summary>
	/// メッシュ/線分表示用ノードを取得する。未生成の場合はここで生成する
	/// </summary>
	private Node3D EnsureMesh()
	{
		if (_meshRoot != null)
		{
			return _meshRoot;
		}

		_meshRoot = new Node3D { Name = "Mesh" };
		EnsureScene().AddChild(_meshRoot);
		return _meshRoot;
	}

	/// <summary>
	/// 衝突形状用ノードを取得する。未生成の場合はここで生成する
	/// </summary>
	private StaticBody3D EnsureCollider()
	{
		if (_collider != null)
		{
			return _collider;
		}

		_collider = new StaticBody3D { Name = "Collider" };
		EnsureScene().AddChild(_collider);
		return _collider;
	}

	/// <summary>
	/// 収集済みの三角形面を凹形状コライダーとしてシーンへ反映する
	/// </summary>
	private void RebuildColliderShape()
	{
		// 意図: 1 回のビルドで同じ StaticBody3D を再利用し、面の蓄積が重なり続けないようにする。
		// 理由: 複数 GLB を積み上げている場合でも、最後に収集した面群だけをコライダーとして再生成するため。
		StaticBody3D collider = EnsureCollider();
		CollisionShape3D collisionShape = collider.GetChildCount() > 0
			? collider.GetChild(0) as CollisionShape3D
			: null;

		if (collisionShape == null)
		{
			collisionShape = new CollisionShape3D();
			collider.AddChild(collisionShape);
		}

		var shape = new ConcavePolygonShape3D();
		shape.SetFaces(_colliderFaces.ToArray());
		// Shape queryにHitBackFacesの指定がないため、凹形状コライダーは両面判定を有効にする
		shape.BackfaceCollision = true;
		collisionShape.Shape = shape;
	}

	/// <summary>
	/// 指定ノード以下の全子孫を、パック対象となるシーンルートの所有物として設定する
	/// </summary>
	/// <param name="root">PackedScene に保存するシーンルート</param>
	/// <param name="node">所有者を設定する子孫探索の起点</param>
	private static void SetOwnerRecursive(Node root, Node node)
	{
		// 意図: PackedScene.Pack() では Owner が明示されたノードだけが保存対象となる。
		// 制約: 一時生成ノードをシーンルート配下へ再帰的に所属させないと、保存時に子孫が落ちるため必須である。
		foreach (Node child in node.GetChildren())
		{
			child.Owner = root;
			SetOwnerRecursive(root, child);
		}
	}

	#endregion
}