using Godot;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

public partial class ModelLoader : Node3D
{

	[Export] public TextEdit TextEditModelPath = null;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

    public override async void _UnhandledKeyInput(InputEvent @event)
    {
		if (@event.IsActionPressed("load"))
		{
			if (TextEditModelPath == null)
			{
				LogHub.Error("TextEditModelPath is not assigned.");
				return;
			}
			string path = TextEditModelPath.Text;
			await LoadModelAsync(path);
		}
	}

	public async Task LoadModelAsync(string path)
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
			// 読み込んだモデルのシーン作成はメインスレッドで行う、まだメインシーンに追加せずキャッシュしておく
			var scene = (Node3D)doc.GenerateScene(state);
			LogHub.Info($"Successfully loaded model: {path}");

			// 新規Node3Dを作成して、その直下に読み込んだモデルの配置
			Node3D node = new Node3D();
			node.AddChild(scene);

			StaticBody3D staticBody = new StaticBody3D();
			node.AddChild(staticBody);

			// glTFモデルの単位系、剤行刑をGodotに合わせる
			// scene.Scale = new Vector3(0.001f, 0.001f, 0.001f);
			// scene.RotationDegrees = new Vector3(-90, -90, 0);

			// StaticBodyにCollisionShape3Dを追加して、モデルの衝突形状を設定
			CollisionShape3D collisionShape = new CollisionShape3D();
			staticBody.AddChild(collisionShape);

			// ここでようやくメインスレッドでモデルをシーンに追加、これ以降はメインスレッドででの処理必須
			AddChild(node);
			LogHub.Info($"Model mesh added to scene: {path}");

			ModelEventHub.RequestAddModel(node, this);

			// モデルの衝突形状を設定するために、1フレーム待つ
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

			var meshes = new List<MeshInstance3D>();
			CollectMeshes(scene, meshes);

			// ArrayMeshを使用するとモデルが大きい場合サイズ制限に引っかかってエラーが出るためConcavePolygonShape3Dを使用して衝突形状を設定
			var allFaces = new Vector3[meshes.Sum(mi => mi.Mesh.GetFaces().Length)];
			int index = 0;

			// StaticBody空間へ変換するための逆行列
			var inverseTransform = node.GlobalTransform.AffineInverse();

			foreach (var mi in meshes)
			{
				var mesh = mi.Mesh;
				if (mesh == null) continue;

				// Meshの三角形頂点列(長さは3の倍数)
				var faces = mesh.GetFaces();

				//mi空間→ワールド→body空間の変換
				var toBody = inverseTransform * mi.GlobalTransform;

				// facesを変換して追記
				for (int i = 0; i < faces.Length; i++)
				{
					allFaces[index++] = toBody * faces[i];
				}
			}
			var shape = new ConcavePolygonShape3D();
			shape.SetFaces(allFaces);

			collisionShape.Shape = shape;

		}
		else
		{
			LogHub.Error($"Failed to load model: {path}, error code: {error}");

		}

		// 所要時間計測終了
		sw.Stop();
		LogHub.Info($"Finished to add collision shape for model: {path} in {sw.ElapsedMilliseconds} ms");
	}

	private void CollectMeshes(Node node, List<MeshInstance3D> list)
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
