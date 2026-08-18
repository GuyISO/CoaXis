using Godot;
using System;
using System.Collections.Generic;

public static class ModelCsvLoader
{
    /// <summary>
    /// CSV ファイルからモデル情報を読み込み、ModelDto のリストとして返す
    /// </summary>
    /// <param name="path">CSV ファイルのパス</param>
    /// <returns>ModelDto のリスト</returns>
    public static List<ModelDto> Load(string path)
    {
        var models = new List<ModelDto>();

        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PrintErr($"Failed to open CSV: {path}");
            return models;
        }

        bool isHeader = true;

        while (!file.EofReached())
        {
            var line = file.GetLine();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            // ヘッダー行はスキップ
            if (isHeader)
            {
                isHeader = false;
                continue;
            }

            var cols = line.Split(',');

            // CSV の列順に合わせて DTO を作成
            var dto = new ModelDto
            {
                Id = Guid.Parse(cols[0]),
                ParentId = string.IsNullOrWhiteSpace(cols[1]) ? null : Guid.Parse(cols[1]),
                Type = cols[2],
                Name = cols[3],
                Position = new float[]
                {
                    float.Parse(cols[4]),
                    float.Parse(cols[5]),
                    float.Parse(cols[6])
                },
                Rotation = new float[]
                {
                    float.Parse(cols[7]),
                    float.Parse(cols[8]),
                    float.Parse(cols[9]),
                    float.Parse(cols[10])
                },
                Visibility = cols[11],
                IconFilePath = cols[12],
                GlbFilePath = cols[13],
                WrlFilePath = cols[14]
            };

            models.Add(dto);
        }

        return models;
    }
}
