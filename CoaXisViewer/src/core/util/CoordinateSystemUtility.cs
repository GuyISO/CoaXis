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
    #region Public Methods

    /// <summary>
    /// CATIA のベクトルを Godot のベクトルへ変換する。
    /// </summary>
    /// <param name="catiaVector">CATIA 座標系のベクトル</param>
    /// <returns>Godot 座標系に変換したベクトル</returns>
    public static Vector3 CatiaToGodot(Vector3 catiaVector)
    {
        // CATIA(X aft, Y right, Z up) -> Godot(X left, Y up, Z forward)
        // Xg = Yc, Yg = Zc, Zg = Xc
        return new Vector3(catiaVector.Y, catiaVector.Z, catiaVector.X);
    }

    /// <summary>
    /// Godot のベクトルを CATIA のベクトルへ変換する。
    /// </summary>
    /// <param name="godotVector">Godot 座標系のベクトル</param>
    /// <returns>CATIA 座標系に変換したベクトル</returns>
    public static Vector3 GodotToCatia(Vector3 godotVector)
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
            CatiaToGodot(catiaBasis.X),
            CatiaToGodot(catiaBasis.Y),
            CatiaToGodot(catiaBasis.Z)
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
            GodotToCatia(godotBasis.X),
            GodotToCatia(godotBasis.Y),
            GodotToCatia(godotBasis.Z)
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