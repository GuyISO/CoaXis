using CoaXis.Protocol.Viewer;
using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

public static class ModelJsonLoader
{
    /// <summary>
    /// JSON ファイルからモデル情報を読み込み、ModelDto のリストとして返す。
    /// </summary>
    /// <param name="path">JSON ファイルのパス</param>
    /// <returns>読み込んだ ModelDto のリスト。読み込みに失敗した場合は空リスト</returns>
    public static List<ModelDto> Load(string path)
    {
        var models = new List<ModelDto>();

        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PrintErr($"Failed to open JSON: {path}");
            return models;
        }

        string json = file.GetAsText();
        if (string.IsNullOrWhiteSpace(json))
        {
            GD.PrintErr($"JSON file is empty: {path}");
            return models;
        }

        try
        {
            List<ModelDto> loadedModels = JsonSerializer.Deserialize<List<ModelDto>>(json);
            return loadedModels ?? models;
        }
        catch (JsonException ex)
        {
            GD.PrintErr($"Failed to parse JSON: {path}. {ex.Message}");
            return models;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Failed to load JSON: {path}. {ex.Message}");
            return models;
        }
    }
}