using Godot;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// glTFモデルの非同期ロードと、ロード後のコライダー追加を行うシングルトンクラスです。
/// </summary>
public partial class ModelLoader : Node
{
	/// <summary>
	/// シングルトン参照です。
	/// </summary>
	public static ModelLoader I { get; private set; }

	/// <summary>
	/// シーンツリー参加時にシングルトン参照を確立します。
	/// </summary>
	public override void _EnterTree()
	{
		I = this;
	}

	/// <summary>
	/// シーンツリー離脱時に、シングルトン参照を破棄します。
	/// </summary>
	public override void _ExitTree()
	{
		I = null;
	}

	/// <summary>
	/// 指定したパスのglTFモデルを非同期でロードし、指定したモデルに追加します。
	/// </summary>
	/// <param name="model">メッシュを追加する親モデル</param>
	/// <param name="path">ロードするglTFモデルのパス</param>
	/// <returns>非同期操作のタスク</returns>
	public static async Task LoadModelAsync(AnyModel model, string path)
	{
		// 所要時間計測開始
		LogHub.Info($"Start loading model: {path}");
		var sw = System.Diagnostics.Stopwatch.StartNew();

		// 非同期でglTFモデルを読み込む
		var doc = new GltfDocument();
		var state = new GltfState();
		var error = await Task.Run(() => doc.AppendFromFile(path, state));

		if (error == Error.Ok)
		{
			var scene = (Node3D)doc.GenerateScene(state);
			model.Mesh.AddChild(scene);
			
			sw.Stop();
			LogHub.Info($"Finished loading model: {path} in {sw.ElapsedMilliseconds} ms");
		}
		else
		{
			LogHub.Error($"Failed to load model: {path}, Error: {error}");
			return;
		}
	}

	public static void AddCollider(AnyModel model)
	{
		
		// 所要時間計測開始
		LogHub.Info($"Start adding collider for model: {model.Name}");
		var sw = System.Diagnostics.Stopwatch.StartNew();

		CollisionShape3D collisionShape = new CollisionShape3D();
		model.Collider.AddChild(collisionShape);

		var meshes = new List<MeshInstance3D>();
		CollectMeshes(model.Mesh, meshes);

		var allFaces = new Vector3[meshes.Sum(mi => mi.Mesh.GetFaces().Length)];
		int index = 0;

		var inverseTransform = model.Collider.GlobalTransform.AffineInverse();

		foreach (var mi in meshes)
		{
			var mesh = mi.Mesh;
			if (mesh == null) continue;

			var faces = mesh.GetFaces();
			var toBody = inverseTransform * mi.GlobalTransform;

			for (int i = 0; i < faces.Length; i++)
			{
				allFaces[index++] = toBody * faces[i];
			}
		}

		var shape = new ConcavePolygonShape3D();
		shape.SetFaces(allFaces);

		collisionShape.Shape = shape;

		sw.Stop();
		LogHub.Info($"Finished adding collider for model: {model.Name} in {sw.ElapsedMilliseconds} ms");
	}

	/// <summary>
	/// 指定したノードの子孫に含まれるすべてのMeshInstance3Dを再帰的に収集します。
	/// </summary>
	/// <param name="node">収集対象のノード</param>
	/// <param name="list">収集したMeshInstance3Dを格納するリスト</param>
	private static void CollectMeshes(Node node, List<MeshInstance3D> list)
	{
		if (node is MeshInstance3D mi)
		{
			list.Add(mi);
		}

		foreach (Node child in node.GetChildren())
		{
			CollectMeshes(child, list);
		}
	}
}
