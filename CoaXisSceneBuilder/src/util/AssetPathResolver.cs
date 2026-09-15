using Godot;
using System;
using System.IO;

/// <summary>
/// res://・user://・相対/絶対パスを実ファイルパスへ解決する共通ユーティリティ
/// </summary>
public static class AssetPathResolver
{
	#region Public Methods

	/// <summary>
	/// 指定パスが実在するファイルかどうかを判定し、実ファイルパスへ解決する
	/// </summary>
	/// <param name="path">res://・user://・相対/絶対のいずれかのパス</param>
	/// <param name="resolvedPath">解決に成功した場合の実ファイルパス</param>
	/// <returns>ファイルが存在する場合はtrue</returns>
	public static bool TryResolveExistingFilePath(string path, out string resolvedPath)
	{
		resolvedPath = path;

		// 意図: CLI や JSON から指定されるパスは res://, user://, 相対パス, 絶対パスのどれかを混在して受け取るため、
		// ここで実ファイルへ正規化してから以降の I/O は同一の前提で扱えるようにする。
		if (string.IsNullOrWhiteSpace(path))
		{
			return false;
		}

		if (path.StartsWith("res://", StringComparison.OrdinalIgnoreCase)
			|| path.StartsWith("user://", StringComparison.OrdinalIgnoreCase))
		{
			string globalizedPath = ProjectSettings.GlobalizePath(path);
			if (File.Exists(globalizedPath))
			{
				resolvedPath = globalizedPath;
				return true;
			}

			return false;
		}

		if (Path.IsPathRooted(path))
		{
			return File.Exists(path);
		}

		string relativePath = ProjectSettings.GlobalizePath(path);
		if (File.Exists(relativePath))
		{
			resolvedPath = relativePath;
			return true;
		}

		return false;
	}

	/// <summary>
	/// 指定パスのテキストファイルを読み込む
	/// </summary>
	/// <param name="path">res://・user://・相対/絶対のいずれかのパス</param>
	/// <returns>読み込んだテキスト、失敗した場合は空文字列</returns>
	public static string ReadText(string path)
	{
		// 意図: Godot の res:// と通常の OS パスを同じ API で扱えるよう、読み込み経路を分岐する。
		// 理由: PCK 内のリソースとローカルの JSON は異なるアクセス方法を取るため、ここで統一した読み込みルールを守る。
		if (path.StartsWith("res://", StringComparison.OrdinalIgnoreCase)
			|| path.StartsWith("user://", StringComparison.OrdinalIgnoreCase))
		{
			using Godot.FileAccess file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
			return file == null ? string.Empty : file.GetAsText();
		}

		string absolutePath = Path.IsPathRooted(path)
			? path
			: ProjectSettings.GlobalizePath(path);

		if (!File.Exists(absolutePath))
		{
			return string.Empty;
		}

		return File.ReadAllText(absolutePath);
	}

	#endregion
}
