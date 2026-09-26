using CoaXis.Protocol.Viewer;
using Godot;
using System;

/// <summary>
/// モデル実体DTOを内部座標系のModelEntityへ変換する
/// </summary>
internal static class ModelEntityMapper
{
	/// <summary>
	/// DTOをModelEntityへ変換する
	/// </summary>
	/// <param name="dto">変換元のモデル実体DTO</param>
	/// <returns>Godot座標系へ変換済みのモデル実体</returns>
	/// <exception cref="ArgumentNullException">dtoがnullの場合</exception>
	internal static ModelEntity Map(ModelEntityDto dto)
	{
		if (dto == null)
		{
			throw new ArgumentNullException(nameof(dto));
		}

		Guid resolvedParentId = dto.ParentId ?? Guid.Empty;
		Vector3 convertedPosition = ConvertPosition(dto.Position);
		Quaternion convertedRotation = ConvertRotation(dto.Rotation);

		return new ModelEntity(
			dto.Id,
			resolvedParentId,
			dto.Type,
			dto.Name,
			convertedPosition,
			convertedRotation,
			ModelVisibilityResolver.Parse(dto.Visibility),
			dto.IsCollapsed,
			dto.IconPath,
			dto.ScenePath,
			dto.AlignToAabbCenter);
	}

	/// <summary>
	/// 外部DTOのCATIA座標を内部で使用するGodot座標へ変換する
	/// </summary>
	/// <param name="position">CATIA座標の位置配列</param>
	/// <returns>Godot座標の位置。入力不正時は原点</returns>
	private static Vector3 ConvertPosition(float[] position)
	{
		if (position == null || position.Length != 3)
		{
			return Vector3.Zero;
		}

		Vector3 catiaVector = new Vector3(position[0], position[1], position[2]);
		return CoordinateSystemUtility.CatiaToGodot(catiaVector);
	}

	/// <summary>
	/// 外部DTOのCATIA姿勢を内部で使用するGodot姿勢へ変換する
	/// </summary>
	/// <param name="rotation">CATIA座標のクォータニオン配列</param>
	/// <returns>Godot座標の姿勢。入力不正時は単位姿勢</returns>
	private static Quaternion ConvertRotation(float[] rotation)
	{
		if (rotation == null || rotation.Length != 4)
		{
			return Quaternion.Identity;
		}

		Quaternion catiaQuaternion = new Quaternion(rotation[0], rotation[1], rotation[2], rotation[3]);
		Basis catiaBasis = new Basis(catiaQuaternion);
		Basis godotBasis = CoordinateSystemUtility.CatiaToGodot(catiaBasis);
		return godotBasis.GetRotationQuaternion();
	}
}