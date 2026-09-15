using Godot;
using System.Collections.Generic;
using System.Text.Json;

/// <summary>
/// ラインセット(独自スキーマの JSON)からチューブメッシュを構築するヘルパー
/// </summary>
public static class LineSetJsonParser
{
	private const float MillimetersPerMeter = 1000.0f;
	private static readonly Color DefaultLineColor = Colors.White;

	#region Public Methods

	/// <summary>
	/// ラインセットJSONを読み込み、指定した親ノード配下へチューブメッシュとして構築する
	/// </summary>
	/// <param name="meshRoot">構築したメッシュを追加する親ノード</param>
	/// <param name="path">読み込むラインセットJSONのパス</param>
	/// <returns>読み込みと構築に成功した場合はtrue、失敗した場合はfalseを返す</returns>
	public static bool LoadLines(Node3D meshRoot, string path)
	{
		return LoadLines(meshRoot, path, null);
	}

	/// <summary>
	/// ラインセットJSONを読み込み、チューブメッシュとコライダー用の三角形面を構築する
	/// </summary>
	/// <param name="meshRoot">構築したメッシュを追加する親ノード</param>
	/// <param name="path">読み込むラインセットJSONのパス</param>
	/// <param name="colliderFaces">生成したチューブメッシュのコライダー用面を積み上げる先。不要な場合はnull</param>
	/// <returns>読み込みと構築に成功した場合はtrue、失敗した場合はfalseを返す</returns>
	public static bool LoadLines(Node3D meshRoot, string path, List<Vector3> colliderFaces)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			GD.PushWarning("LineSetJsonParser: empty line set json path.");
			return false;
		}

		string content = AssetPathResolver.ReadText(path);
		if (string.IsNullOrWhiteSpace(content))
		{
			GD.PushError($"LineSetJsonParser: failed to read line set json. path='{path}'");
			return false;
		}

		if (!TryParse(content, out List<LineSetEntry> lineSet))
		{
			GD.PushError($"LineSetJsonParser: failed to parse line set json. path='{path}'");
			return false;
		}

		int segmentCount = AttachLineMesh(meshRoot, lineSet, colliderFaces);
		if (segmentCount <= 0)
		{
			// 意図: 入力データが空配列であるケースは正常な状態のため、警告のみで正常終了とする
			GD.Print($"LineSetJsonParser: no line segment found (empty line set is valid). path='{path}'");
			return true;
		}

		GD.Print($"LineSetJsonParser: loaded {segmentCount} segments from '{path}'.");
		return true;
	}

	#endregion

	#region Parse Helpers

	private static bool TryParse(string content, out List<LineSetEntry> lineSet)
	{
		lineSet = null;

		try
		{
			var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
			lineSet = JsonSerializer.Deserialize<List<LineSetEntry>>(content, options);
		}
		catch (JsonException)
		{
			return false;
		}

		return lineSet != null;
	}

	#endregion

	#region Mesh Helpers

	private static int AttachLineMesh(Node3D meshRoot, List<LineSetEntry> lineSet, List<Vector3> colliderFaces)
	{
		// 意図: 前回のライン生成ノードが残ると、同じ Mesh ノードに別設定が重なって表示が崩れるため必ず除去する。
		ClearLineNodes(meshRoot);

		var lineRoot = new Node3D { Name = "LineSet" };
		meshRoot.AddChild(lineRoot);

		LineSetSettings lineSetSettings = BuilderSettingsLoader.GetInstance().LineSet;
		int segmentCount = 0;
		foreach (LineSetEntry entry in lineSet)
		{
			if (entry?.Points == null || entry.Points.Length < 2)
			{
				continue;
			}

			BaseMaterial3D material = CreateLineMaterial(entry.Color);
			var points = new List<Vector3>(entry.Points.Length);
			foreach (float[] point in entry.Points)
			{
				points.Add(ConvertCatiaPoint(point));
			}

			if (!TryBuildTubeMesh(points, material, lineSetSettings, out ArrayMesh tubeMesh, out int entrySegmentCount))
			{
				continue;
			}

			var line = new MeshInstance3D
			{
				Name = "LinePolyline",
				Mesh = tubeMesh
			};
			lineRoot.AddChild(line);
			// 意図: 描画に使うチューブ面をそのまま収集し、見た目と当たり判定の形状を一致させる。
			// 制約: Meshルートは変換を持たないため、ローカル面座標をSceneBuilderのコライダーへ直接渡せる。
			if (colliderFaces != null)
			{
				colliderFaces.AddRange(tubeMesh.GetFaces());
			}
			segmentCount += entrySegmentCount;
		}

		if (segmentCount <= 0)
		{
			lineRoot.QueueFree();
			return 0;
		}

		return segmentCount;
	}

	// CATIA座標系(mm, Z-Up)をGodot座標系(m, Y-Up)へ変換する。Xg=Yc, Yg=Zc, Zg=Xcの軸対応で1000分の1する
	private static Vector3 ConvertCatiaPoint(float[] point)
	{
		if (point == null || point.Length != 3)
		{
			return Vector3.Zero;
		}

		// CATIA は X=後方, Y=右, Z=上 の座標系を使い、glb変換後にも同じ座標系を維持するため、Godot の X=右, Y=上, Z=後方 へ軸を入れ替える。
		// さらに mm 単位の値を m 単位へ揃えることで、Godot のスケーリングルールと一致させる。
		return new Vector3(point[1], point[2], point[0]) / MillimetersPerMeter;
	}

	private static bool TryBuildTubeMesh(
		List<Vector3> points,
		Material tubeMaterial,
		LineSetSettings settings,
		out ArrayMesh tubeMesh,
		out int segmentCount)
	{
		tubeMesh = null;
		segmentCount = 0;

		// 意図: 短いループの連続点を密集して描かないよう、最小長さ未満の点は早めに捨ててメッシュ量を抑える。
		var validPoints = new List<Vector3>(points.Count);
		foreach (Vector3 point in points)
		{
			if (validPoints.Count == 0
				|| validPoints[^1].DistanceTo(point) >= settings.MinSegmentLengthMeters)
			{
				validPoints.Add(point);
			}
		}

		segmentCount = validPoints.Count - 1;
		if (segmentCount <= 0)
		{
			return false;
		}

		var vertices = new List<Vector3>();
		var indices = new List<int>();
		Vector3 previousSide = Vector3.Zero;

		for (int pointIndex = 0; pointIndex < validPoints.Count; pointIndex++)
		{
			Vector3 tangent = GetPointTangent(validPoints, pointIndex);
			Vector3 side = GetContinuousSide(tangent, previousSide);
			AddRing(vertices, validPoints[pointIndex], tangent, side, settings);
			previousSide = side;
		}

		for (int pointIndex = 1; pointIndex < validPoints.Count; pointIndex++)
		{
			AddRingConnection(indices, pointIndex - 1, pointIndex, settings);
		}
		AddEndCaps(vertices, indices, validPoints.Count, settings);

		var arrays = new Godot.Collections.Array();
		arrays.Resize((int)Mesh.ArrayType.Max);
		arrays[(int)Mesh.ArrayType.Vertex] = vertices.ToArray();
		arrays[(int)Mesh.ArrayType.Index] = indices.ToArray();

		tubeMesh = new ArrayMesh();
		tubeMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
		tubeMesh.SurfaceSetMaterial(0, tubeMaterial);
		return true;
	}

	private static Vector3 GetPointTangent(List<Vector3> points, int pointIndex)
	{
		if (pointIndex == 0)
		{
			return (points[1] - points[0]).Normalized();
		}

		if (pointIndex == points.Count - 1)
		{
			return (points[^1] - points[^2]).Normalized();
		}

		Vector3 incoming = (points[pointIndex] - points[pointIndex - 1]).Normalized();
		Vector3 outgoing = (points[pointIndex + 1] - points[pointIndex]).Normalized();
		Vector3 tangent = incoming + outgoing;
		return tangent.LengthSquared() < Mathf.Epsilon ? outgoing : tangent.Normalized();
	}

	private static Vector3 GetContinuousSide(Vector3 direction, Vector3 previousSide)
	{
		if (previousSide.LengthSquared() > Mathf.Epsilon)
		{
			Vector3 projectedSide = previousSide - direction * previousSide.Dot(direction);
			if (projectedSide.LengthSquared() > Mathf.Epsilon)
			{
				return projectedSide.Normalized();
			}
		}

		Vector3 reference = Mathf.Abs(direction.Dot(Vector3.Up)) < 0.9f
			? Vector3.Up
			: Vector3.Right;
		return direction.Cross(reference).Normalized();
	}

	private static void AddRing(
		List<Vector3> vertices,
		Vector3 center,
		Vector3 direction,
		Vector3 side,
		LineSetSettings settings)
	{
		Vector3 up = direction.Cross(side).Normalized();
		int radialSegments = Mathf.Max(3, settings.TubeRadialSegments);
		float radius = settings.TubeRadiusMeters;
		for (int radialIndex = 0; radialIndex < radialSegments; radialIndex++)
		{
			float angle = Mathf.Tau * radialIndex / radialSegments;
			Vector3 radial = (side * Mathf.Cos(angle) + up * Mathf.Sin(angle)) * radius;
			vertices.Add(center + radial);
		}
	}

	private static void AddRingConnection(List<int> indices, int startRing, int endRing, LineSetSettings settings)
	{
		int radialSegments = Mathf.Max(3, settings.TubeRadialSegments);
		int startIndex = startRing * radialSegments;
		int endIndex = endRing * radialSegments;
		for (int radialIndex = 0; radialIndex < radialSegments; radialIndex++)
		{
			int nextRadialIndex = (radialIndex + 1) % radialSegments;
			int startCurrent = startIndex + radialIndex;
			int startNext = startIndex + nextRadialIndex;
			int endCurrent = endIndex + radialIndex;
			int endNext = endIndex + nextRadialIndex;

			indices.Add(startCurrent);
			indices.Add(endCurrent);
			indices.Add(endNext);
			indices.Add(startCurrent);
			indices.Add(endNext);
			indices.Add(startNext);
		}
	}

	private static void AddEndCaps(List<Vector3> vertices, List<int> indices, int pointCount, LineSetSettings settings)
	{
		int radialSegments = Mathf.Max(3, settings.TubeRadialSegments);
		int startCenter = vertices.Count;
		vertices.Add(vertices[0]);
		int endCenter = vertices.Count;
		vertices.Add(vertices[(pointCount - 1) * radialSegments]);
		int endRing = (pointCount - 1) * radialSegments;

		for (int radialIndex = 0; radialIndex < radialSegments; radialIndex++)
		{
			int nextRadialIndex = (radialIndex + 1) % radialSegments;
			int startCurrent = radialIndex;

			// 始点と終点のキャップは外向きが逆になるため、頂点順も互いに反転させる
			indices.Add(startCenter);
			indices.Add(startCurrent);
			indices.Add(nextRadialIndex);
			indices.Add(endCenter);
			indices.Add(endRing + nextRadialIndex);
			indices.Add(endRing + radialIndex);
		}
	}

	private static BaseMaterial3D CreateLineMaterial(float[] color)
	{
		// color未指定時は固定の既定色にフォールバックする
		Color lineColor = color != null && color.Length == 3
			? new Color(color[0], color[1], color[2])
			: DefaultLineColor;

		return new StandardMaterial3D
		{
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			// キャップは薄い面のため、始点・終点のどちら側から見ても欠けないよう両面描画する
			CullMode = BaseMaterial3D.CullModeEnum.Disabled,
			NoDepthTest = false,
			AlbedoColor = lineColor,
			EmissionEnabled = true,
			Emission = lineColor
		};
	}

	private static void ClearLineNodes(Node meshNode)
	{
		// Meshノードはglbと共有するため、前回生成した"LineSet"ノードのみを除去する
		foreach (Node child in meshNode.GetChildren())
		{
			if (child.Name == "LineSet")
			{
				child.QueueFree();
			}
		}
	}

	#endregion

	/// <summary>
	/// 解析対象のラインセットJSONデータ構造を表す内部クラス
	/// </summary>
	private sealed class LineSetEntry
	{
		public float[] Color { get; set; }
		public float[][] Points { get; set; }
	}
}
