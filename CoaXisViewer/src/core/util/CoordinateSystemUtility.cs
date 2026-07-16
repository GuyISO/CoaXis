using Godot;
using System;

/// <summary>
/// CATIA における航空機の座標系 (Z+ Up, X+ Aft, Y+ Right) と
/// Godot 標準の座標系 (Y+ Up, Z+ Forward, X+ Left) の相互変換を提供するユーティリティ。
/// </summary>
/// <remarks>
/// 運用ルール:
/// - ユーザー表示値や外部保存値（DB/IPC/ファイル）は CATIA 座標系で扱う。
/// - Godot 内部のノード変換・計算は Godot 座標系で扱う。
/// - 座標系境界を跨ぐ地点で本ユーティリティを必ず使用する。
/// </remarks>
public static class CoordinateSystemUtility
{
    private const float MillimetersPerMeter = 1000.0f;

    #region Public Methods

    /// <summary>
    /// CATIA(mm) の位置ベクトルを Godot(m) の位置ベクトルへ変換する。
    /// </summary>
    /// <param name="catiaVector">CATIA 座標系・mm のベクトル</param>
    /// <returns>Godot 座標系・m に変換した位置ベクトル</returns>
    public static Vector3 CatiaToGodot(Vector3 catiaVector)
    {
        return CatiaDirectionToGodot(catiaVector) / MillimetersPerMeter;
    }

    /// <summary>
    /// CATIA(mm) の距離値を Godot(m) の距離値へ変換する。
    /// </summary>
    /// <param name="catiaDistanceMm">CATIA 単位(mm)の距離</param>
    /// <returns>Godot 単位(m)の距離</returns>
    public static float CatiaDistanceToGodot(float catiaDistanceMm)
    {
        return catiaDistanceMm / MillimetersPerMeter;
    }

    /// <summary>
    /// Godot(m) の距離値を CATIA(mm) の距離値へ変換する。
    /// </summary>
    /// <param name="godotDistanceM">Godot 単位(m)の距離</param>
    /// <returns>CATIA 単位(mm)の距離</returns>
    public static float GodotDistanceToCatia(float godotDistanceM)
    {
        return godotDistanceM * MillimetersPerMeter;
    }

    /// <summary>
    /// CATIA の方向ベクトルを Godot の方向ベクトルへ変換する（単位換算なし）。
    /// </summary>
    /// <param name="catiaVector">CATIA 座標系の方向ベクトル</param>
    /// <returns>Godot 座標系に変換したベクトル</returns>
    public static Vector3 CatiaDirectionToGodot(Vector3 catiaVector)
    {
        // CATIA(X aft, Y right, Z up) -> Godot(X left, Y up, Z forward)
        // Xg = Yc, Yg = Zc, Zg = Xc
        return new Vector3(catiaVector.Y, catiaVector.Z, catiaVector.X);
    }

    /// <summary>
    /// Godot(m) の位置ベクトルを CATIA(mm) の位置ベクトルへ変換する。
    /// </summary>
    /// <param name="godotVector">Godot 座標系・m のベクトル</param>
    /// <returns>CATIA 座標系・mm に変換した位置ベクトル</returns>
    public static Vector3 GodotToCatia(Vector3 godotVector)
    {
        return GodotDirectionToCatia(godotVector) * MillimetersPerMeter;
    }

    /// <summary>
    /// Godot の方向ベクトルを CATIA の方向ベクトルへ変換する（単位換算なし）。
    /// </summary>
    /// <param name="godotVector">Godot 座標系の方向ベクトル</param>
    /// <returns>CATIA 座標系に変換した方向ベクトル</returns>
    public static Vector3 GodotDirectionToCatia(Vector3 godotVector)
    {
        // Godot(X left, Y up, Z forward) -> CATIA(X aft, Y right, Z up)
        // Xc = Zg, Yc = Xg, Zc = Yg
        return new Vector3(godotVector.Z, godotVector.X, godotVector.Y);
    }

    /// <summary>
    /// CATIA の Basis を Godot の Basis へ変換する。
    /// </summary>
    /// <param name="catiaBasis">CATIA 座標系の Basis</param>
    /// <returns>Godot 座標系に変換した Basis</returns>
    public static Basis CatiaToGodot(Basis catiaBasis)
    {
        return new Basis(
            CatiaDirectionToGodot(catiaBasis.X),
            CatiaDirectionToGodot(catiaBasis.Y),
            CatiaDirectionToGodot(catiaBasis.Z)
        ).Orthonormalized();
    }

    /// <summary>
    /// Godot の Basis を CATIA の Basis へ変換する。
    /// </summary>
    /// <param name="godotBasis">Godot 座標系の Basis</param>
    /// <returns>CATIA 座標系に変換した Basis</returns>
    public static Basis GodotToCatia(Basis godotBasis)
    {
        return new Basis(
            GodotDirectionToCatia(godotBasis.X),
            GodotDirectionToCatia(godotBasis.Y),
            GodotDirectionToCatia(godotBasis.Z)
        ).Orthonormalized();
    }

    /// <summary>
    /// CATIA の Quaternion を Godot の Quaternion へ変換する。
    /// </summary>
    /// <param name="catiaQuaternion">CATIA 座標系の Quaternion</param>
    /// <returns>Godot 座標系に変換した Quaternion</returns>
    public static Quaternion CatiaToGodot(Quaternion catiaQuaternion)
    {
        var convertedBasis = CatiaToGodot(new Basis(catiaQuaternion));
        return convertedBasis.GetRotationQuaternion();
    }

    /// <summary>
    /// Godot の Quaternion を CATIA の Quaternion へ変換する。
    /// </summary>
    /// <param name="godotQuaternion">Godot 座標系の Quaternion</param>
    /// <returns>CATIA 座標系に変換した Quaternion</returns>
    public static Quaternion GodotToCatia(Quaternion godotQuaternion)
    {
        var convertedBasis = GodotToCatia(new Basis(godotQuaternion));
        return convertedBasis.GetRotationQuaternion();
    }

    /// <summary>
    /// CATIA の Transform3D を Godot の Transform3D へ変換する。
    /// </summary>
    /// <param name="catiaTransform">CATIA 座標系の Transform3D</param>
    /// <returns>Godot 座標系に変換した Transform3D</returns>
    public static Transform3D CatiaToGodot(Transform3D catiaTransform)
    {
        return new Transform3D(
            CatiaToGodot(catiaTransform.Basis),
            CatiaToGodot(catiaTransform.Origin)
        );
    }

    /// <summary>
    /// Godot の Transform3D を CATIA の Transform3D へ変換する。
    /// </summary>
    /// <param name="godotTransform">Godot 座標系の Transform3D</param>
    /// <returns>CATIA 座標系に変換した Transform3D</returns>
    public static Transform3D GodotToCatia(Transform3D godotTransform)
    {
        return new Transform3D(
            GodotToCatia(godotTransform.Basis),
            GodotToCatia(godotTransform.Origin)
        );
    }

    #endregion
}