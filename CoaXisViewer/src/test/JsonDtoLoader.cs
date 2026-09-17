using CoaXis.Protocol.Viewer;
using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

/// <summary>
/// JSON 配列から任意の DTO を読み込む共通ローダー。
/// </summary>
public static class JsonDtoLoader
{
    /// <summary>
    /// JSON ファイルから指定した DTO 型のリストを読み込む。
    /// </summary>
    /// <typeparam name="T">読み込む DTO の型</typeparam>
    /// <param name="path">JSON ファイルのパス</param>
    /// <returns>読み込んだ DTO のリスト。読み込みに失敗した場合は空リスト</returns>
    public static List<T> Load<T>(string path) where T : BaseDto
    {
        var items = new List<T>();

        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PrintErr($"Failed to open JSON: {path}");
            return items;
        }

        string json = file.GetAsText();
        if (string.IsNullOrWhiteSpace(json))
        {
            GD.PrintErr($"JSON file is empty: {path}");
            return items;
        }

        try
        {
            // DTOごとの変換処理を持たせず、JSON構造と型定義の対応をSerializerへ委譲する。
            List<T> loadedItems = JsonSerializer.Deserialize<List<T>>(json);
            return loadedItems ?? items;
        }
        catch (JsonException ex)
        {
            GD.PrintErr($"Failed to parse JSON: {path}. {ex.Message}");
            return items;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Failed to load JSON: {path}. {ex.Message}");
            return items;
        }
    }
}