// TODO: ちゃんと内容理解する

using Godot;
using CoaXis.Protocol.Viewer;
using System;

/// <summary>
/// ModelParticle は ModelProperty を 3D Viewer 上で視覚表現するパーティクル。
/// ModelEntity に対する ModelNode に相当し、point / box のシーンへアタッチして使う
/// </summary>
public partial class ModelParticle : GpuParticles3D
{
	#region Fields

	/// <summary>指定色に乗じる発光倍率</summary>
	private const float HdrIntensity = 3.5f;

	/// <summary>AABB対角線長に対する、各方向への余白の割合</summary>
	private const float PaddingRatio = 0.01f;

	/// <summary>AABB対角線長に対する、スプライト1枚の一辺の割合。モデルの大きさに関わらず密度感を保つ</summary>
	private const float SpriteSizeRatio = 0.03f;

	/// <summary>色未指定時の既定色（発光倍率適用前）</summary>
	private static readonly Color DefaultColor = Colors.White;

	/// <summary>テクスチャ未指定時の既定テクスチャID</summary>
	private const string DefaultTextureId = ModelParticleTextureId.Star;

	/// <summary>パーティクル1個あたりの描画数</summary>
	private const int ParticleAmount = 4;

	/// <summary>パーティクルの寿命（秒）</summary>
	private const double ParticleLifetime = 0.5;

	/// <summary>放出範囲が未確定の間に使うスプライトの一辺</summary>
	private const float DefaultSpriteSize = 0.05f;

	/// <summary>テクスチャIDごとの描画メッシュ。メッシュとマテリアルは同じ画像のパーティクル間で共有する</summary>
	private static readonly System.Collections.Generic.Dictionary<string, QuadMesh> MeshCache = new(System.StringComparer.OrdinalIgnoreCase);

	/// <summary>種類ごとのフェード用スケール曲線。全パーティクルで共有する</summary>
	private static readonly System.Collections.Generic.Dictionary<ModelParticleType, CurveTexture> ScaleCurveCache = new();

	/// <summary>現在の描画メッシュに適用済みのテクスチャID</summary>
	private string _appliedTextureId;

	#endregion
	
	#region Properties

	/// <summary>
	/// 表現対象の ModelProperty の識別子、未バインド時は Guid.Empty
	/// </summary>
	public Guid PropertyId { get; private set; } = Guid.Empty;

	/// <summary>
	/// 表現対象の ModelProperty を取得する、未バインドまたは未登録の場合は null
	/// </summary>
	public ModelProperty Property => PropertyId == Guid.Empty ? null : Application.Model.Property.Registry.Get(PropertyId);

	#endregion

	#region Lifecycle

	public override void _Ready()
	{
		Refresh();
	}

	#endregion

	#region Methods

	/// <summary>
	/// 表現対象の ModelProperty を関連付け、現在の値を反映する
	/// </summary>
	/// <param name="propertyId">表現対象の ModelProperty の識別子</param>
	public void Bind(Guid propertyId)
	{
		PropertyId = propertyId;
		Refresh();
	}

	/// <summary>
	/// ModelProperty の現在状態を表示へ反映する
	/// </summary>
	public void Refresh()
	{
		ModelProperty property = Property;
		if (property == null || !IsNodeReady())
		{
			return;
		}

		ApplyTexture(property.ParticleTextureId);

		if (ProcessMaterial is ParticleProcessMaterial material)
		{
			Color baseColor = property.ParticleColor ?? DefaultColor;
			material.Color = new Color(baseColor.R * HdrIntensity, baseColor.G * HdrIntensity, baseColor.B * HdrIntensity, baseColor.A);
		}

		FitToModel(property);
	}

	/// <summary>
	/// 親 ModelNode の AABB 中心へ配置し、AABB を対角線長の一定割合だけ全方向へ広げた範囲に収める
	/// </summary>
	/// <remarks>
	/// 薄いモデルでもパーティクルが見えるよう余白を足す。
	/// シーン未ロードでメッシュが無い場合は現状のまま変更しない。
	/// </remarks>
	/// <param name="property">表現対象の ModelProperty</param>
	public void FitToModel(ModelProperty property)
	{
		if (GetParent() is not ModelNode modelNode ||
			!WorldAabbUtility.TryGetWorldAabb(new[] { modelNode }, out Aabb worldAabb) ||
			worldAabb.Size == Vector3.Zero)
		{
			// AABBが無い間は既定サイズで目立たないよう非表示にする
			Visible = false;
			return;
		}

		Visible = true;

		// ModelNodeローカル空間の外接AABBへ変換し、ModelNodeの回転・スケールに追従させる
		Transform3D worldToLocal = modelNode.GlobalTransform.AffineInverse();
		Aabb localAabb = worldToLocal * worldAabb;
		Vector3 center = localAabb.GetCenter();
		float margin = localAabb.Size.Length() * PaddingRatio;
		Vector3 halfSize = localAabb.Size * 0.5f + Vector3.One * margin;

		Position = center;

		// スプライトの大きさを放出範囲の大きさへ比例させ、小さいモデルがエフェクトで埋もれないようにする。
		// 描画メッシュは共有するため、サイズはメッシュではなくProcessMaterialのスケールで表現する。
		if (ProcessMaterial is not ParticleProcessMaterial material)
		{
			return;
		}

		float spriteSize = localAabb.Size.Length() * SpriteSizeRatio;
		material.ScaleMin = spriteSize;
		material.ScaleMax = spriteSize;

		if (property.ParticleType == ModelParticleType.Box)
		{
			material.EmissionBoxExtents = halfSize;
			VisibilityAabb = new Aabb(-halfSize, halfSize * 2f);
		}
		else
		{
			// 点状の噴出は寿命内に拡張後の範囲の外接球半径へ届く初速にする
			float radius = halfSize.Length();
			material.InitialVelocityMax = radius / (float)Lifetime;
			material.InitialVelocityMin = material.InitialVelocityMax / 3f;
			VisibilityAabb = new Aabb(Vector3.One * -radius, Vector3.One * radius * 2f);
		}
	}

	/// <summary>
	/// 種類に対応するパーティクルを生成する
	/// </summary>
	/// <param name="particleType">生成するパーティクルの種類</param>
	/// <returns>生成したパーティクル、None の場合は null</returns>
	public static ModelParticle Create(ModelParticleType particleType)
	{
		if (particleType != ModelParticleType.Point && particleType != ModelParticleType.Box)
		{
			return null;
		}

		bool isBox = particleType == ModelParticleType.Box;
		var material = new ParticleProcessMaterial
		{
			EmissionShape = isBox ? ParticleProcessMaterial.EmissionShapeEnum.Box : ParticleProcessMaterial.EmissionShapeEnum.Point,
			EmissionBoxExtents = Vector3.One,
			AngleMin = -180f,
			AngleMax = 180f,
			Direction = Vector3.Zero,
			Spread = 180f,
			Gravity = Vector3.Zero,
			ScaleMin = DefaultSpriteSize,
			ScaleMax = DefaultSpriteSize,
			ScaleCurve = GetScaleCurve(particleType),
		};
		if (!isBox)
		{
			material.InitialVelocityMin = 1f;
			material.InitialVelocityMax = 3f;
		}

		return new ModelParticle
		{
			Amount = ParticleAmount,
			Lifetime = ParticleLifetime,
			ProcessMaterial = material,
		};
	}

	#endregion

	#region Helpers

	/// <summary>
	/// 要求されたテクスチャIDの共有メッシュを描画パスへ適用する。IDが変わらない場合は何もしない
	/// </summary>
	private void ApplyTexture(string requestedId)
	{
		string key = string.IsNullOrWhiteSpace(requestedId) ? DefaultTextureId : requestedId;
		if (key == _appliedTextureId)
		{
			return;
		}

		_appliedTextureId = key;
		DrawPass1 = GetOrCreateMesh(key);
	}

	/// <summary>
	/// テクスチャIDに対応する共有メッシュを取得する。画像を読み込めない場合はテクスチャなしで描画する
	/// </summary>
	private static QuadMesh GetOrCreateMesh(string textureId)
	{
		if (MeshCache.TryGetValue(textureId, out QuadMesh cached))
		{
			return cached;
		}

		QuadMesh mesh = CreateMesh(Application.Asset.Texture.GetParticle(textureId));
		MeshCache[textureId] = mesh;
		return mesh;
	}

	/// <summary>
	/// テクスチャを貼った 1x1 の Billboard 用メッシュを生成する。大きさはProcessMaterialのスケールで決める
	/// </summary>
	private static QuadMesh CreateMesh(Texture2D texture)
	{
		var material = new StandardMaterial3D
		{
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			VertexColorUseAsAlbedo = true,
			AlbedoTexture = texture,
			BillboardMode = BaseMaterial3D.BillboardModeEnum.Enabled,
			BillboardKeepScale = true,
		};
		return new QuadMesh { Size = Vector2.One, Material = material };
	}

	/// <summary>
	/// 種類に対応する共有のスケール曲線（0 → 1 → 0 でフェードイン・アウト）を取得する
	/// </summary>
	private static CurveTexture GetScaleCurve(ModelParticleType particleType)
	{
		if (ScaleCurveCache.TryGetValue(particleType, out CurveTexture cached))
		{
			return cached;
		}

		var curve = new Curve();
		if (particleType == ModelParticleType.Point)
		{
			// 発生時にサイズ1、消滅時に0へ線形に遷移させる
			curve.AddPoint(new Vector2(0f, 1f), 0f, 0f, Curve.TangentMode.Linear, Curve.TangentMode.Linear);
			curve.AddPoint(new Vector2(1f, 0f), 0f, 0f, Curve.TangentMode.Linear, Curve.TangentMode.Linear);
		}
		else
		{
			// 発生時にサイズ0、中央で最大1、消滅時に0へ線形に遷移させる
			curve.AddPoint(new Vector2(0f, 0f));
			curve.AddPoint(new Vector2(0.5f, 1f));
			curve.AddPoint(new Vector2(1f, 0f));
		}

		var texture = new CurveTexture { Curve = curve };
		ScaleCurveCache[particleType] = texture;
		return texture;
	}

	#endregion
}
