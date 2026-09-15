using Godot;
using System.Text.Json.Serialization;

/// <summary>
/// GLBモデルに対するトランスフォーム設定（スケール・回転）データ構造
/// </summary>
public class GlbTransformSettings
{
	/// <summary>
	/// 3D座標の各軸要素(X, Y, Z)を保持するDTO
	/// </summary>
	public class Vector3Dto
	{
		/// <summary>
		/// X成分
		/// </summary>
		[JsonPropertyName("X")]
		public float X { get; set; }

		/// <summary>
		/// Y成分
		/// </summary>
		[JsonPropertyName("Y")]
		public float Y { get; set; }

		/// <summary>
		/// Z成分
		/// </summary>
		[JsonPropertyName("Z")]
		public float Z { get; set; }

		/// <summary>
		/// デフォルトコンストラクタ
		/// </summary>
		public Vector3Dto() { }

		/// <summary>
		/// 各軸の値を指定するコンストラクタ
		/// </summary>
		/// <param name="x">X軸の値</param>
		/// <param name="y">Y軸の値</param>
		/// <param name="z">Z軸の値</param>
		public Vector3Dto(float x, float y, float z)
		{
			X = x;
			Y = y;
			Z = z;
		}

		/// <summary>
		/// GodotのVector3へ変換する
		/// </summary>
		/// <returns>変換後のVector3</returns>
		public Vector3 ToVector3()
		{
			return new Vector3(X, Y, Z);
		}
	}

	/// <summary>
	/// スケーリング値
	/// </summary>
	[JsonPropertyName("Scale")]
	public Vector3Dto Scale { get; set; } = new Vector3Dto(0.001f, 0.001f, 0.001f);

	/// <summary>
	/// 回転角度（度数法）
	/// </summary>
	[JsonPropertyName("RotationDegrees")]
	public Vector3Dto RotationDegrees { get; set; } = new Vector3Dto(-90f, -90f, 0f);

	/// <summary>
	/// GodotのVector3形式でスケール値を取得する
	/// </summary>
	/// <returns>スケールを表すVector3</returns>
	public Vector3 GetScaleVector()
	{
		// 意図: JSONデータが不完全でScaleプロパティがnullの場合でも、安全に標準mm->m変換のデフォルト値(0.001)を返す
		// 理由: NullReferenceExceptionを防止して後続のモデル読込処理を止めないため
		return Scale?.ToVector3() ?? new Vector3(0.001f, 0.001f, 0.001f);
	}

	/// <summary>
	/// GodotのVector3形式で回転角度（度数法）を取得する
	/// </summary>
	/// <returns>回転角度を表すVector3</returns>
	public Vector3 GetRotationDegreesVector()
	{
		// 意図: JSONデータが不完全でRotationDegreesプロパティがnullの場合でも、標準Z-up補正のデフォルト角度(-90, -90, 0)を返す
		// 理由: NullReferenceExceptionを防止し、モデル姿勢の崩れを防ぐため
		return RotationDegrees?.ToVector3() ?? new Vector3(-90f, -90f, 0f);
	}
}
