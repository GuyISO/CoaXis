using Godot;
using CoaXis.Protocol.Viewer;
using System;

/// <summary>
/// ModelParticle は ModelProperty を 3D Viewer 上で視覚表現するパーティクル。
/// ModelEntity に対する ModelNode に相当し、所属Entityの描画Nodeへアタッチして使う
/// </summary>
public partial class ModelParticle : GpuParticles3D
{
	#region Fields

	// TODO: SettingHubの在り方検討後、定数を外部設定可能にする

	/// <summary>指定色に乗じる発光倍率</summary>
	private const float HdrIntensity = 3.5f;

	/// <summary>色未指定時の既定色（発光倍率適用前）</summary>
	private static readonly Color DefaultColor = Colors.White;

	/// <summary>パーティクル1個あたりの描画数</summary>
	private const int ParticleAmount = 12;

	/// <summary>パーティクルの寿命（秒）</summary>
	private const double ParticleLifetime = 0.5;

	/// <summary>放出範囲が未確定の間に使うスプライトの一辺</summary>
	private const float DefaultSpriteSize = 0.05f;

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

		ApplyColor(property.ParticleColor);
		FitToModel(property);
	}

	/// <summary>
	/// 色だけを反映する。放出範囲の再計算は行わない
	/// </summary>
	/// <param name="color">反映する色、null の場合は既定色</param>
	public void ApplyColor(Color? color)
	{
		if (ProcessMaterial is not ParticleProcessMaterial material)
		{
			return;
		}

		Color baseColor = color ?? DefaultColor;
		material.Color = new Color(baseColor.R * HdrIntensity, baseColor.G * HdrIntensity, baseColor.B * HdrIntensity, baseColor.A);
	}

	/// <summary>
	/// パーティクルをモデルに合わせて配置し、描画範囲をモデルの AABB に合わせる
	/// </summary>
	/// <remarks>
	/// シーン未ロードでメッシュが無い場合や放出点を取得できない場合は非表示にする。
	/// </remarks>
	/// <param name="property">表現対象の ModelProperty</param>
	public void FitToModel(ModelProperty property)
	{
		if (ProcessMaterial is not ParticleProcessMaterial material)
		{
			return;
		}

		if (GetParent() is not ModelNode modelNode ||
			!ParticleEmitterFitter.TryFit(
				modelNode,
				material,
				property.ParticleEmitterType,
				property.ParticleBehaviorType,
				Lifetime,
				out Vector3 position,
				out Aabb visibilityAabb))
		{
			Visible = false;
			return;
		}

		Visible = true;
		Position = position;
		VisibilityAabb = visibilityAabb;
	}

	/// <summary>
	/// 発生源と挙動に対応するパーティクルを生成する
	/// </summary>
	/// <param name="emitterType">生成するパーティクルの発生源</param>
	/// <param name="behaviorType">生成するパーティクルの挙動</param>
	/// <returns>生成したパーティクル、None の場合は null</returns>
	public static ModelParticle Create(
		ModelParticleEmitterType emitterType,
		ModelParticleBehaviorType behaviorType)
	{
		if (!Enum.IsDefined(emitterType) || !Enum.IsDefined(behaviorType))
		{
			throw new ArgumentOutOfRangeException(
				!Enum.IsDefined(emitterType) ? nameof(emitterType) : nameof(behaviorType));
		}

		if ((emitterType == ModelParticleEmitterType.None) != (behaviorType == ModelParticleBehaviorType.None))
		{
			throw new ArgumentException("Emitter and behavior must both be None or both be specified.");
		}

		if (emitterType == ModelParticleEmitterType.None)
		{
			return null;
		}

		var material = new ParticleProcessMaterial
		{
			EmissionShape = emitterType switch
			{
				ModelParticleEmitterType.Box => ParticleProcessMaterial.EmissionShapeEnum.Box,
				ModelParticleEmitterType.Sphere => ParticleProcessMaterial.EmissionShapeEnum.Sphere,
				ModelParticleEmitterType.Surface => ParticleProcessMaterial.EmissionShapeEnum.Points,
				_ => ParticleProcessMaterial.EmissionShapeEnum.Point,
			},
			EmissionBoxExtents = Vector3.One,
			AngleMin = -180f,
			AngleMax = 180f,
			Direction = Vector3.Zero,
			Spread = 180f,
			Gravity = Vector3.Zero,
			ScaleMin = DefaultSpriteSize / ParticleVisualResourceFactory.MeshUnitSize,
			ScaleMax = DefaultSpriteSize / ParticleVisualResourceFactory.MeshUnitSize,
			ScaleCurve = ParticleVisualResourceFactory.GetScaleCurve(behaviorType),
		};
		if (behaviorType == ModelParticleBehaviorType.Disperse)
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
		string key = ParticleVisualResourceFactory.NormalizeTextureId(requestedId);
		if (key == _appliedTextureId)
		{
			return;
		}

		_appliedTextureId = key;
		DrawPass1 = ParticleVisualResourceFactory.GetOrCreateMesh(key);
	}

	#endregion
}
