using CoaXis.Protocol.Viewer;
using Godot;
using System;

/// <summary>
/// モデル実体DTOを内部座標系のModelEntityへ変換する
/// </summary>
public static class ModelEntityMapper
{
	#region Methods

	/// <summary>
	/// DTOをModelEntityへ変換する
	/// </summary>
	/// <param name="dto">変換元のモデル実体DTO</param>
	/// <returns>Godot座標系へ変換済みのモデル実体</returns>
	/// <exception cref="ArgumentNullException">dtoがnullの場合</exception>
	/// <exception cref="ArgumentException">アイコンID、位置、姿勢が不正な場合</exception>
	public static ModelEntity Map(ModelEntityDto dto)
	{
		if (dto == null)
		{
			throw new ArgumentNullException(nameof(dto));
		}

		if (dto.IconId == null || (!string.IsNullOrEmpty(dto.IconId) && !ModelIconId.IsValid(dto.IconId)))
		{
			throw new ArgumentException(
				$"ModelEntity '{dto.Id}' has an invalid IconId '{dto.IconId}'.",
				nameof(dto));
		}

		Guid resolvedParentId = dto.ParentId ?? Guid.Empty;
		Vector3 convertedPosition = ConvertPosition(dto.Id, dto.Position);
		Quaternion convertedRotation = ConvertRotation(dto.Id, dto.Rotation);

		return new ModelEntity(
			dto.Id,
			resolvedParentId,
			dto.Type,
			dto.Name,
			convertedPosition,
			convertedRotation,
			ModelVisibilityResolver.Parse(dto.Visibility),
			dto.IsCollapsed,
			dto.IconId,
			dto.ScenePath,
			dto.IsPropertyTreeVisible);
	}

	/// <summary>
	/// 部分更新DTOを検証し、Godot座標系の更新内容へ変換する
	/// </summary>
	/// <param name="dto">変換元の部分更新DTO</param>
	/// <returns>検証済みの更新内容</returns>
	/// <exception cref="ArgumentNullException">dtoがnullの場合</exception>
	/// <exception cref="ArgumentException">IDが空、または位置、姿勢、表示設定が不正な場合</exception>
	public static ModelEntityModification MapModification(ModelEntityModificationDto dto)
	{
		if (dto == null)
		{
			throw new ArgumentNullException(nameof(dto));
		}

		if (dto.Id == Guid.Empty)
		{
			throw new ArgumentException("ModelEntityModificationDto.Id must not be empty.", nameof(dto));
		}

		return new ModelEntityModification
		{
			Id = dto.Id,
			Position = dto.Position == null ? null : ConvertPosition(dto.Id, dto.Position),
			Rotation = dto.Rotation == null ? null : ConvertRotation(dto.Id, dto.Rotation),
			Visibility = dto.Visibility == null ? null : ModelVisibilityResolver.ParseStrict(dto.Visibility),
			IsCollapsed = dto.IsCollapsed,
			IsPropertyTreeVisible = dto.IsPropertyTreeVisible
		};
	}

	/// <summary>
	/// ModelEntityを外部向けDTO（CATIA座標系）へ変換する
	/// </summary>
	/// <param name="entity">変換元のモデル実体</param>
	/// <returns>モデル実体DTO</returns>
	/// <exception cref="ArgumentNullException">entityがnullの場合</exception>
	public static ModelEntityDto ToDto(ModelEntity entity)
	{
		if (entity == null)
		{
			throw new ArgumentNullException(nameof(entity));
		}

		Vector3 position = CoordinateSystemUtility.GodotToCatia(entity.Position);
		Quaternion rotation = CoordinateSystemUtility.GodotToCatia(entity.Rotation);
		return new ModelEntityDto
		{
			Id = entity.Id,
			ParentId = entity.ParentId == Guid.Empty ? null : entity.ParentId,
			Type = entity.Type,
			Name = entity.Name,
			Position = new[] { position.X, position.Y, position.Z },
			Rotation = new[] { rotation.X, rotation.Y, rotation.Z, rotation.W },
			Visibility = entity.Visibility.ToString(),
			IsCollapsed = entity.IsCollapsed,
			IconId = entity.IconId,
			ScenePath = entity.ScenePath,
			IsPropertyTreeVisible = entity.IsPropertyTreeVisible
		};
	}

	/// <summary>
	/// 外部DTOのCATIA座標を内部で使用するGodot座標へ変換する
	/// </summary>
	/// <param name="entityId">変換対象のEntity識別子</param>
	/// <param name="position">CATIA座標の位置配列</param>
	/// <returns>Godot座標へ変換した位置</returns>
	/// <exception cref="ArgumentException">位置配列がnull、要素数不正、または有限値でない場合</exception>
	private static Vector3 ConvertPosition(Guid entityId, float[] position)
	{
		if (position == null || position.Length != 3)
		{
			throw new ArgumentException(
				$"ModelEntity '{entityId}' has an invalid Position; expected three finite CATIA coordinates.",
				nameof(position));
		}

		foreach (float component in position)
		{
			if (!float.IsFinite(component))
			{
				throw new ArgumentException(
					$"ModelEntity '{entityId}' has an invalid Position; expected three finite CATIA coordinates.",
					nameof(position));
			}
		}

		Vector3 catiaVector = new Vector3(position[0], position[1], position[2]);
		return CoordinateSystemUtility.CatiaToGodot(catiaVector);
	}

	/// <summary>
	/// 外部DTOのCATIA姿勢を内部で使用するGodot姿勢へ変換する
	/// </summary>
	/// <param name="entityId">変換対象のEntity識別子</param>
	/// <param name="rotation">CATIA座標のクォータニオン配列</param>
	/// <returns>Godot座標へ変換した正規化済み姿勢</returns>
	/// <exception cref="ArgumentException">姿勢配列がnull、要素数不正、有限値でない、またはゼロ姿勢の場合</exception>
	private static Quaternion ConvertRotation(Guid entityId, float[] rotation)
	{
		if (rotation == null || rotation.Length != 4)
		{
			throw new ArgumentException(
				$"ModelEntity '{entityId}' has an invalid Rotation; expected four finite quaternion components with non-zero magnitude.",
				nameof(rotation));
		}

		double x = rotation[0];
		double y = rotation[1];
		double z = rotation[2];
		double w = rotation[3];
		double magnitude = Math.Sqrt(x * x + y * y + z * z + w * w);
		if (!float.IsFinite(rotation[0]) ||
			!float.IsFinite(rotation[1]) ||
			!float.IsFinite(rotation[2]) ||
			!float.IsFinite(rotation[3]) ||
			magnitude == 0d)
		{
			throw new ArgumentException(
				$"ModelEntity '{entityId}' has an invalid Rotation; expected four finite quaternion components with non-zero magnitude.",
				nameof(rotation));
		}

		Quaternion catiaQuaternion = new Quaternion(
			(float)(x / magnitude),
			(float)(y / magnitude),
			(float)(z / magnitude),
			(float)(w / magnitude));
		Basis catiaBasis = new Basis(catiaQuaternion);
		Basis godotBasis = CoordinateSystemUtility.CatiaToGodot(catiaBasis);
		return godotBasis.GetRotationQuaternion();
	}
	
	#endregion
}